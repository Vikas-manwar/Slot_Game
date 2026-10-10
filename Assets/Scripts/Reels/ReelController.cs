using System;
using System.Collections.Generic;
using SlotGame.Core;
using SlotGame.Data;
using UnityEngine;

namespace SlotGame.Reels
{
    public sealed class ReelController : MonoBehaviour
    {
        private enum Phase { Idle, WindUp, Accelerate, Spin, Decelerate, Settle }

        [SerializeField] private RectTransform symbolContainer;
        [SerializeField] private SymbolView symbolPrefab;
        [SerializeField] private ReelAnimationProfile profile;
        [Tooltip("Distance between symbol centres, in UI units.")]
        [SerializeField] private float symbolSpacing = 70f;
        [Tooltip("Size of each symbol cell, in UI units.")]
        [SerializeField] private Vector2 symbolSize = new Vector2(68f, 68f);

        public event Action<int> Stopped;

        private readonly List<SymbolView> _views = new List<SymbolView>();
        private SlotConfig _config;
        private ReelStrip _strip;
        private int _reelIndex;
        private int _visibleRows;
        private int _halfPool;

        private Phase _phase = Phase.Idle;
        private double _position;
        private double _phaseStartPosition;
        private double _phaseTargetPosition;
        private float _phaseTime;
        private float _phaseDuration;
        private float _speedFactor = 1f;

        private int? _pendingStop;
        private int _targetStop;
        private double _settleTarget;

        public bool IsSpinning => _phase != Phase.Idle;
        public ReelAnimationProfile Profile => profile;
        public int ReelIndex => _reelIndex;

        public void Initialize(int reelIndex, ReelStrip strip, SlotConfig config, int startStop)
        {
            _reelIndex = reelIndex;
            _strip = strip ?? throw new ArgumentNullException(nameof(strip));
            _config = config;
            _visibleRows = config.VisibleRows;
            _halfPool = _visibleRows / 2 + 1;

            foreach (var view in _views)
                if (view != null) Destroy(view.gameObject);
            _views.Clear();

            for (int i = 0; i < _halfPool * 2 + 1; i++)
            {
                var view = Instantiate(symbolPrefab, symbolContainer);
                view.name = $"Symbol_{i}";
                view.RectTransform.sizeDelta = symbolSize;
                _views.Add(view);
            }

            _position = _strip.Wrap(startStop);
            _phase = Phase.Idle;
            Render(0f);
        }

        public void StartSpin()
        {
            if (_strip == null) throw new InvalidOperationException("Reel not initialised.");

            _position = _strip.Wrap((int)Math.Round(_position));
            _pendingStop = null;
            _speedFactor = 1f;
            SetPhase(Phase.WindUp, profile.windUpDuration, _position, _position - profile.windUpDistance);
        }

        public void RequestStop(int stopIndex)
        {
            if (!IsSpinning) return;
            if (_phase == Phase.Decelerate || _phase == Phase.Settle) return;

            if (_phase == Phase.Spin) BeginDeceleration(stopIndex);
            else _pendingStop = stopIndex;
        }

        public void SetAnticipation(bool active)
        {
            _speedFactor = active ? profile.anticipationSpeedFactor : 1f;
        }

        public SymbolView GetViewAtRow(int row)
        {
            int offset = _visibleRows / 2 - row;
            return _views[offset + _halfPool];
        }

        public void SetAllDimmed(bool dimmed)
        {
            foreach (var view in _views) view.SetDimmed(dimmed);
        }

        public void ClearWinStates()
        {
            foreach (var view in _views)
            {
                view.SetWinning(false);
                view.SetDimmed(false);
            }
        }

        private void Update()
        {
            if (_phase == Phase.Idle) return;

            float dt = Time.deltaTime;
            double previous = _position;
            _phaseTime += dt;
            float t = _phaseDuration > 0f ? Mathf.Clamp01(_phaseTime / _phaseDuration) : 1f;

            switch (_phase)
            {
                case Phase.WindUp:
                    _position = Lerp(_phaseStartPosition, _phaseTargetPosition, Easing.InOutSine(t));
                    if (t >= 1f) SetPhase(Phase.Accelerate, profile.accelerationDuration, _position, _position);
                    break;

                case Phase.Accelerate:
                    _position += profile.maxSpeed * Easing.InQuad(t) * _speedFactor * dt;
                    if (t >= 1f)
                    {
                        SetPhase(Phase.Spin, 0f, _position, _position);
                        if (_pendingStop.HasValue) BeginDeceleration(_pendingStop.Value);
                    }
                    break;

                case Phase.Spin:
                    _position += profile.maxSpeed * _speedFactor * dt;
                    break;

                case Phase.Decelerate:
                    _position = Lerp(_phaseStartPosition, _phaseTargetPosition, Easing.OutCubic(t));
                    if (t >= 1f)
                        SetPhase(Phase.Settle, profile.bounceDuration, _position, _settleTarget);
                    break;

                case Phase.Settle:
                    _position = Lerp(_phaseStartPosition, _phaseTargetPosition, Easing.InOutSine(t));
                    if (t >= 1f) FinishStop();
                    break;
            }

            float speed01 = dt > 0f ? (float)(Math.Abs(_position - previous) / dt / profile.maxSpeed) : 0f;
            Render(speed01);
        }

        private void BeginDeceleration(int stopIndex)
        {
            _pendingStop = null;
            _targetStop = _strip.Wrap(stopIndex);

            double earliest = _position + profile.minimumStopDistance;
            double cycles = Math.Ceiling((earliest - _targetStop) / _strip.Length);
            double target = _targetStop + cycles * _strip.Length;

            double overshootTarget = target + profile.bounceDistance;
            float speed = Mathf.Max(0.01f, profile.maxSpeed * _speedFactor);
            float duration = (float)(3.0 * (overshootTarget - _position) / speed);

            _settleTarget = target;
            SetPhase(Phase.Decelerate, duration, _position, overshootTarget);
        }

        private void FinishStop()
        {
            _position = _targetStop;
            _phase = Phase.Idle;
            _speedFactor = 1f;
            Render(0f);
            Stopped?.Invoke(_reelIndex);
        }

        private void SetPhase(Phase phase, float duration, double from, double to)
        {
            _phase = phase;
            _phaseTime = 0f;
            _phaseDuration = duration;
            _phaseStartPosition = from;
            _phaseTargetPosition = to;
        }

        private void Render(float speed01)
        {
            double floor = Math.Floor(_position);
            float fraction = (float)(_position - floor);
            int baseIndex = (int)floor;

            for (int k = -_halfPool; k <= _halfPool; k++)
            {
                var view = _views[k + _halfPool];
                view.RectTransform.anchoredPosition = new Vector2(0f, (k - fraction) * symbolSpacing);
                view.SetSymbol(_config.GetSymbol(_strip.GetSymbolAt(baseIndex + k)));
                view.SetMotion(speed01, profile.maxStretch, profile.minAlphaAtSpeed);
            }
        }

        private static double Lerp(double a, double b, float t) => a + (b - a) * t;
    }
}
