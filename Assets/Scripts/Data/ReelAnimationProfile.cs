using UnityEngine;

namespace SlotGame.Data
{
    [CreateAssetMenu(fileName = "ReelAnimationProfile", menuName = "Slot Game/Reel Animation Profile", order = 2)]
    public sealed class ReelAnimationProfile : ScriptableObject
    {
        [Header("Start")]
        [Tooltip("How far the reel pulls back (upwards) before spinning, like a real reel's kick.")]
        [Range(0f, 1f)] public float windUpDistance = 0.25f;
        [Min(0.01f)] public float windUpDuration = 0.12f;
        [Min(0.01f)] public float accelerationDuration = 0.25f;

        [Header("Spin")]
        [Tooltip("Top speed in symbols per second.")]
        [Min(1f)] public float maxSpeed = 22f;
        [Tooltip("Delay between reels starting, left to right.")]
        [Min(0f)] public float reelStartStagger = 0.07f;
        [Tooltip("Minimum time all reels spin before the first one stops.")]
        [Min(0f)] public float minimumSpinTime = 0.9f;
        [Tooltip("Delay between consecutive reels stopping.")]
        [Min(0f)] public float reelStopInterval = 0.32f;
        [Tooltip("Extra spin time on the last reel when a big result is still possible.")]
        [Min(0f)] public float anticipationDuration = 1.4f;
        [Tooltip("Spin speed multiplier during anticipation (slower = more tension).")]
        [Range(0.2f, 1f)] public float anticipationSpeedFactor = 0.55f;

        [Header("Stop")]
        [Tooltip("Minimum symbols travelled while decelerating, so the stop never looks abrupt.")]
        [Min(1f)] public float minimumStopDistance = 3f;
        [Tooltip("How far the reel overshoots before settling back (bounce).")]
        [Range(0f, 0.6f)] public float bounceDistance = 0.22f;
        [Min(0.01f)] public float bounceDuration = 0.16f;

        [Header("Motion Blur (fake)")]
        [Tooltip("Vertical stretch applied to symbols at full speed to suggest motion blur.")]
        [Range(0f, 0.5f)] public float maxStretch = 0.18f;
        [Range(0f, 1f)] public float minAlphaAtSpeed = 0.75f;
    }
}
