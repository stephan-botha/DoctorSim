// MedMania.Presentation.Views
// HandAnimator.cs
// Responsibility: Tween-based verbs that drive disembodied hand targets.

using DG.Tweening;
using UnityEngine;
using UnityEngine.Animations;
using MedMania.Core.Data.ScriptableObjects;

namespace MedMania.Presentation.Views.Hands
{
    [DisallowMultipleComponent]
    public sealed class HandAnimator : MonoBehaviour
    {
        private const float k_DefaultDuration = 0.25f;
        private const float k_DefaultConstraintBlend = 0.12f;

        private static bool s_DOTweenInitialized;

        [Header("Targets")]
        [SerializeField] private Transform _handTarget;
        [SerializeField] private Transform _elbowHint;
        [SerializeField] private Behaviour _constraintBehaviour;
        [SerializeField] private bool _autoEnableConstraint = true;
        [SerializeField] private bool _autoDisableConstraintOnRelease = true;
        [SerializeField, Range(0.01f, 1f)] private float _constraintBlendDuration = k_DefaultConstraintBlend;

        [Header("Presets")]
        [SerializeField] private MotionPreset _reachPreset;
        [SerializeField] private MotionPreset _hoverPreset;
        [SerializeField] private MotionPreset _tapPreset;
        [SerializeField] private MotionPreset _releasePreset;

        [Header("Defaults")]
        [SerializeField] private bool _hoverOnEnable = true;
        [SerializeField, Range(0f, 0.25f)] private float _fallbackHoverRadius = 0.01f;
        [SerializeField, Range(0.2f, 5f)] private float _fallbackHoverPeriod = 1.6f;
        [SerializeField, Range(-30f, 30f)] private float _fallbackHoverTilt = 3f;

        private Transform _initialParent;
        private Vector3 _neutralLocalPosition;
        private Quaternion _neutralLocalRotation = Quaternion.identity;
        private Sequence _hoverSequence;
        private Sequence _activeSequence;
        private Tween _constraintBlend;

        public Transform HandTarget => _handTarget;
        public Transform ElbowHint => _elbowHint;
        public MotionPreset ReachPreset => _reachPreset;
        public MotionPreset HoverPreset => _hoverPreset;
        public MotionPreset TapPreset => _tapPreset;
        public float DefaultReachOffsetCm => _reachPreset != null ? _reachPreset.DefaultReachOffsetCm : 0f;

        private void Awake()
        {
            EnsureTweenSystem();
            CaptureNeutralPose();
        }

        private void OnEnable()
        {
            if (_hoverOnEnable)
            {
                HoverLoop();
            }
        }

        private void OnDisable()
        {
            KillActiveSequence();
            KillHover();
            KillConstraintBlend();
            DOTween.Kill(this);
        }

        private void Reset()
        {
            if (_handTarget == null && transform.childCount > 0)
            {
                _handTarget = transform.GetChild(0);
            }
        }

        public void RefreshNeutralPose()
        {
            CaptureNeutralPose();
        }

        public Sequence Reach(Transform anchor, float handOffsetCm = 0f)
        {
            if (_handTarget == null || anchor == null)
            {
                return null;
            }

            KillHover();
            KillActiveSequence();

            var seq = CreateSequence();
            var duration = ResolveDuration(_reachPreset);
            var ease = ResolveEase(_reachPreset, Ease.OutCubic);

            var targetPosition = anchor.position;
            if (!Mathf.Approximately(handOffsetCm, 0f))
            {
                targetPosition += anchor.right * (handOffsetCm * 0.01f);
            }

            seq.Join(_handTarget.DOMove(targetPosition, duration).SetEase(ease));
            seq.Join(_handTarget.DORotateQuaternion(anchor.rotation, duration).SetEase(ease));

            var constraintTween = CreateConstraintTween(1f);
            if (constraintTween != null)
            {
                seq.Join(constraintTween);
            }

            RegisterActiveSequence(seq);
            return seq;
        }

        public Sequence Attach(Transform anchor)
        {
            var seq = Reach(anchor, DefaultReachOffsetCm);
            if (seq == null)
            {
                return null;
            }

            seq.AppendCallback(() =>
            {
                if (_handTarget != null && anchor != null)
                {
                    _handTarget.SetParent(anchor, true);
                }
            });

            seq.OnComplete(StartHoverFromCurrent);
            return seq;
        }

        public void Release(Transform reparent = null)
        {
            KillHover();
            KillActiveSequence();

            if (_handTarget == null)
            {
                return;
            }

            var targetParent = reparent != null ? reparent : _initialParent;
            if (targetParent != null && _handTarget.parent != targetParent)
            {
                _handTarget.SetParent(targetParent, true);
            }

            var seq = CreateSequence();
            var preset = _releasePreset != null ? _releasePreset : _reachPreset;
            var duration = ResolveDuration(preset);
            var ease = ResolveEase(preset, Ease.OutCubic);

            seq.Join(_handTarget.DOLocalMove(_neutralLocalPosition, duration).SetEase(ease));
            seq.Join(_handTarget.DOLocalRotateQuaternion(_neutralLocalRotation, duration).SetEase(ease));
            seq.OnComplete(StartHoverFromCurrent);

            var constraintTween = CreateConstraintTween(0f);
            if (constraintTween != null)
            {
                seq.Join(constraintTween);
            }

            RegisterActiveSequence(seq);
        }

        public void HoverLoop(float? radius = null, float? period = null, float? tilt = null)
        {
            if (_handTarget == null)
            {
                return;
            }

            KillHover();

            var startPos = _handTarget.localPosition;
            var startRot = _handTarget.localRotation;

            float resolvedRadius = radius ?? (_hoverPreset != null ? _hoverPreset.HoverRadius : _fallbackHoverRadius);
            float resolvedPeriod = Mathf.Max(0.01f, period ?? (_hoverPreset != null ? _hoverPreset.HoverPeriod : _fallbackHoverPeriod));
            float resolvedTilt = tilt ?? (_hoverPreset != null ? _hoverPreset.HoverTiltDegrees : _fallbackHoverTilt);

            if (resolvedRadius <= 0f && Mathf.Approximately(resolvedTilt, 0f))
            {
                return;
            }

            var halfPeriod = resolvedPeriod * 0.5f;

            _hoverSequence = CreateSequence();
            _hoverSequence.Append(_handTarget.DOLocalMove(startPos + Vector3.up * resolvedRadius, halfPeriod).SetEase(Ease.InOutSine));
            _hoverSequence.Join(_handTarget.DOLocalRotateQuaternion(Quaternion.Euler(resolvedTilt, 0f, 0f) * startRot, halfPeriod));
            _hoverSequence.Append(_handTarget.DOLocalMove(startPos, halfPeriod).SetEase(Ease.InOutSine));
            _hoverSequence.Join(_handTarget.DOLocalRotateQuaternion(startRot, halfPeriod));
            _hoverSequence.SetLoops(-1, LoopType.Restart);
        }

        public void Tap(Vector3 worldPoint)
        {
            if (_handTarget == null)
            {
                return;
            }

            KillHover();
            KillActiveSequence();

            var seq = CreateSequence();
            var duration = ResolveDuration(_tapPreset);
            duration = Mathf.Max(0.01f, duration);

            var forwardDuration = duration * 0.6f;
            var returnDuration = Mathf.Max(0.01f, duration - forwardDuration);

            var startPos = _handTarget.position;
            seq.Append(_handTarget.DOMove(worldPoint, forwardDuration).SetEase(Ease.InOutCubic));
            seq.Append(_handTarget.DOMove(startPos, returnDuration).SetEase(Ease.OutCubic));

            RegisterActiveSequence(seq);
        }

        public void Recoil(float degrees = 8f, float duration = 0.2f)
        {
            if (_handTarget == null)
            {
                return;
            }

            _handTarget.DOPunchRotation(new Vector3(-degrees, 0f, 0f), duration,
                ResolveVibrato(_tapPreset), ResolveElasticity(_tapPreset))
                .SetUpdate(UpdateType.Late, true)
                .SetId(this)
                .SetLink(gameObject);
        }

        public void Nudge(Vector3 direction, float distance = 0.05f, float duration = 0.15f)
        {
            if (_handTarget == null || direction == Vector3.zero)
            {
                return;
            }

            KillHover();

            _handTarget.DOBlendableMoveBy(direction.normalized * distance, duration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(UpdateType.Late, true)
                .SetId(this)
                .SetLink(gameObject);
        }

        public void PulseGrip(float amount = 0.05f, float duration = 0.25f)
        {
            if (_handTarget == null)
            {
                return;
            }

            _handTarget.DOPunchScale(Vector3.one * amount, duration,
                ResolveVibrato(_tapPreset), ResolveElasticity(_tapPreset))
                .SetUpdate(UpdateType.Late, true)
                .SetId(this)
                .SetLink(gameObject);
        }

        private void CaptureNeutralPose()
        {
            if (_handTarget == null)
            {
                _initialParent = null;
                _neutralLocalPosition = Vector3.zero;
                _neutralLocalRotation = Quaternion.identity;
                return;
            }

            _initialParent = _handTarget.parent;
            _neutralLocalPosition = _handTarget.localPosition;
            _neutralLocalRotation = _handTarget.localRotation;
        }

        private void StartHoverFromCurrent()
        {
            if (_hoverOnEnable)
            {
                HoverLoop();
            }
        }

        private Sequence CreateSequence()
        {
            return DOTween.Sequence()
                .SetUpdate(UpdateType.Late, true)
                .SetId(this)
                .SetLink(gameObject);
        }

        private void RegisterActiveSequence(Sequence sequence)
        {
            _activeSequence = sequence;
            sequence.OnKill(() =>
            {
                if (_activeSequence == sequence)
                {
                    _activeSequence = null;
                }
            });
        }

        private float ResolveDuration(MotionPreset preset)
        {
            return preset != null ? Mathf.Max(0.01f, preset.Duration) : k_DefaultDuration;
        }

        private Ease ResolveEase(MotionPreset preset, Ease fallback)
        {
            return preset != null ? preset.Ease : fallback;
        }

        private int ResolveVibrato(MotionPreset preset)
        {
            return preset != null ? Mathf.Max(1, preset.Vibrato) : 10;
        }

        private float ResolveElasticity(MotionPreset preset)
        {
            return preset != null ? Mathf.Clamp01(preset.Elasticity) : 0.5f;
        }

        private Tween CreateConstraintTween(float targetWeight)
        {
            if (_constraintBehaviour == null)
            {
                return null;
            }

            if (_autoEnableConstraint && !_constraintBehaviour.enabled)
            {
                _constraintBehaviour.enabled = true;
            }

            if (!(_constraintBehaviour is IConstraint constraint))
            {
                return null;
            }

            constraint.constraintActive = true;

            var duration = Mathf.Max(0.01f, _constraintBlendDuration);
            KillConstraintBlend();

            _constraintBlend = DOVirtual.Float(constraint.weight, targetWeight, duration, w => constraint.weight = w)
                .SetUpdate(UpdateType.Late, true)
                .SetId(this)
                .SetLink(gameObject)
                .OnComplete(() =>
                {
                    if (Mathf.Approximately(targetWeight, 0f) && _autoDisableConstraintOnRelease)
                    {
                        constraint.constraintActive = false;
                        if (_constraintBehaviour != null)
                        {
                            _constraintBehaviour.enabled = false;
                        }
                    }
                });

            return _constraintBlend;
        }

        private void KillActiveSequence()
        {
            if (_activeSequence != null && _activeSequence.IsActive())
            {
                _activeSequence.Kill();
                _activeSequence = null;
            }
        }

        private void KillHover()
        {
            if (_hoverSequence != null && _hoverSequence.IsActive())
            {
                _hoverSequence.Kill();
                _hoverSequence = null;
            }
        }

        private void KillConstraintBlend()
        {
            if (_constraintBlend != null && _constraintBlend.IsActive())
            {
                _constraintBlend.Kill();
                _constraintBlend = null;
            }
        }

        private static void EnsureTweenSystem()
        {
            if (s_DOTweenInitialized)
            {
                return;
            }

            DOTween.Init(false, true, LogBehaviour.ErrorsOnly);
            DOTween.useSafeMode = true;
            s_DOTweenInitialized = true;
        }
    }
}
