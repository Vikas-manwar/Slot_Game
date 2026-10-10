using System;
using UnityEngine;

namespace SlotGame.Audio
{
    public sealed class AudioManager : MonoBehaviour
    {
        [Serializable]
        private struct SoundEntry
        {
            public SoundEffect effect;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume;
        }

        [SerializeField] private AudioSource oneShotSource;
        [SerializeField] private AudioSource accentSource;
        [SerializeField] private AudioSource loopSource;
        [SerializeField] private AudioClip spinLoopClip;
        [SerializeField] private SoundEntry[] sounds = new SoundEntry[0];

        private const string MutedPrefKey = "SlotGame.Muted";

        public bool IsMuted { get; private set; }

        private void Awake()
        {
            SetMuted(PlayerPrefs.GetInt(MutedPrefKey, 0) == 1);
        }

        public void Play(SoundEffect effect, float pitch = 1f)
        {
            if (IsMuted || oneShotSource == null) return;

            foreach (var entry in sounds)
            {
                if (entry.effect != effect || entry.clip == null) continue;

                var source = oneShotSource;
                if (!Mathf.Approximately(pitch, 1f) && accentSource != null)
                {
                    source = accentSource;
                    source.pitch = pitch;
                }
                source.PlayOneShot(entry.clip, entry.volume <= 0f ? 1f : entry.volume);
                return;
            }
        }

        public void StartSpinLoop()
        {
            if (loopSource == null || spinLoopClip == null) return;
            loopSource.clip = spinLoopClip;
            loopSource.loop = true;
            if (!loopSource.isPlaying) loopSource.Play();
        }

        public void StopSpinLoop()
        {
            if (loopSource != null) loopSource.Stop();
        }

        public void ToggleMute() => SetMuted(!IsMuted);

        public void SetMuted(bool muted)
        {
            IsMuted = muted;
            if (oneShotSource != null) oneShotSource.mute = muted;
            if (accentSource != null) accentSource.mute = muted;
            if (loopSource != null) loopSource.mute = muted;
            PlayerPrefs.SetInt(MutedPrefKey, muted ? 1 : 0);
        }
    }
}
