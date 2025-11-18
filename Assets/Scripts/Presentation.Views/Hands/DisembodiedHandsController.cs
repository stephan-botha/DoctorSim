// MedMania.Presentation.Views
// DisembodiedHandsController.cs
// Responsibility: Orchestrates DOTween hand verbs in response to staff interactions.

using DG.Tweening;
using UnityEngine;
using MedMania.Core.Domain.Inventory;
using MedMania.Core.Domain.Procedures;
using MedMania.Presentation.Input.Staff;
using MedMania.Presentation.Views.Procedures;
using MedMania.Presentation.Views.Patients;

namespace MedMania.Presentation.Views.Hands
{
    [DisallowMultipleComponent]
    public sealed class DisembodiedHandsController : MonoBehaviour
    {
        [Header("Hands")]
        [SerializeField] private HandAnimator _leftHand;
        [SerializeField] private HandAnimator _rightHand;
        [SerializeField] private Transform _leftNeutralAnchor;
        [SerializeField] private Transform _rightNeutralAnchor;
        [SerializeField] private float _leftOffsetCm = -6f;
        [SerializeField] private float _rightOffsetCm = 6f;

        [Header("Sources")]
        [SerializeField] private StaffAgent _staff;
        [SerializeField] private ProcedureRunner _procedureRunner;
        [SerializeField] private Transform _fallbackAnchor;

        [Header("Behaviour")]
        [SerializeField] private bool _hoverWhenIdle = true;

        private ICarrySlot _handsSlot;
        private bool _isCarrying;
        private bool _subscribed;
        private Transform _currentInteractionAnchor;

        private void Awake()
        {
            ResolveAnimators();
            ResolveDependencies();
        }

        private void OnEnable()
        {
            ResolveAnimators();
            ResolveDependencies();
            Subscribe();
            SyncState();
        }

        private void OnDisable()
        {
            Unsubscribe();
            DetachSlotListener();
            _currentInteractionAnchor = null;
        }

        private void ResolveAnimators()
        {
            if (_leftHand != null && _rightHand != null)
            {
                return;
            }

            var animators = GetComponentsInChildren<HandAnimator>(true);
            if (_leftHand == null && animators.Length > 0)
            {
                _leftHand = animators[0];
            }

            if (_rightHand == null)
            {
                for (int i = animators.Length - 1; i >= 0; i--)
                {
                    var candidate = animators[i];
                    if (candidate != null && candidate != _leftHand)
                    {
                        _rightHand = candidate;
                        break;
                    }
                }
            }
        }

        private void ResolveDependencies()
        {
            if (_staff == null)
            {
                _staff = GetComponentInParent<StaffAgent>();
            }

            if (_procedureRunner == null)
            {
                _procedureRunner = GetComponentInParent<ProcedureRunner>();
            }

            _currentInteractionAnchor = _procedureRunner != null ? _procedureRunner.ActiveInteractionAnchor : null;

            ResolveHandsSlot();
        }

        private void Subscribe()
        {
            if (_subscribed)
            {
                return;
            }

            if (_staff != null)
            {
                _staff.PerformRequested.AddListener(HandlePerformRequested);
            }

            if (_procedureRunner != null)
            {
                _procedureRunner.onStarted.AddListener(HandleRunStarted);
                _procedureRunner.onCompleted.AddListener(HandleRunCompleted);
                _procedureRunner.onPatientReset.AddListener(HandlePatientReset);
                _procedureRunner.onInteractionAnchorResolved.AddListener(HandleInteractionAnchorResolved);
            }

            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            if (_staff != null)
            {
                _staff.PerformRequested.RemoveListener(HandlePerformRequested);
            }

            if (_procedureRunner != null)
            {
                _procedureRunner.onStarted.RemoveListener(HandleRunStarted);
                _procedureRunner.onCompleted.RemoveListener(HandleRunCompleted);
                _procedureRunner.onPatientReset.RemoveListener(HandlePatientReset);
                _procedureRunner.onInteractionAnchorResolved.RemoveListener(HandleInteractionAnchorResolved);
            }

            _subscribed = false;
        }

        private void ResolveHandsSlot()
        {
            DetachSlotListener();

            if (_staff != null && _staff.HandsSlot != null)
            {
                _handsSlot = _staff.HandsSlot;
            }
            else
            {
                _handsSlot = GetComponentInChildren<ICarrySlot>();
            }

            if (_handsSlot != null)
            {
                _handsSlot.OccupantChanged += OnHandsOccupantChanged;
                _isCarrying = !_handsSlot.IsEmpty;
            }
            else
            {
                _isCarrying = false;
            }
        }

        private void DetachSlotListener()
        {
            if (_handsSlot != null)
            {
                _handsSlot.OccupantChanged -= OnHandsOccupantChanged;
                _handsSlot = null;
            }
        }

        private void OnHandsOccupantChanged(ICarryable occupant)
        {
            _isCarrying = occupant != null;
            if (_isCarrying)
            {
                AttachToCarryable(occupant);
            }
            else
            {
                if (_currentInteractionAnchor != null)
                {
                    ReachForAnchor(_currentInteractionAnchor);
                }
                else
                {
                    ReturnHandsToNeutral();
                }
            }
        }

        private void HandlePerformRequested(IProcedureDef procedure)
        {
            if (_isCarrying)
            {
                _rightHand?.PulseGrip(0.03f, 0.2f);
                return;
            }

            var anchor = ResolveActiveAnchor();
            if (anchor != null)
            {
                _rightHand?.Tap(anchor.position);
                _leftHand?.Nudge(Vector3.left * 0.5f, 0.02f, 0.15f);
            }
        }

        private void HandleRunStarted(IProcedureDef procedure)
        {
            if (_isCarrying)
            {
                return;
            }

            var anchor = ResolveActiveAnchor();
            if (anchor == null)
            {
                return;
            }

            var rightSequence = _rightHand?.Attach(anchor);
            rightSequence?.OnComplete(() =>
            {
                if (_hoverWhenIdle)
                {
                    _rightHand?.HoverLoop();
                }
            });

            var leftSequence = _leftHand?.Reach(anchor, ResolveLeftOffset());
            leftSequence?.OnComplete(() =>
            {
                if (_hoverWhenIdle)
                {
                    _leftHand?.HoverLoop();
                }
            });
        }

        private void HandleRunCompleted(IProcedureDef _)
        {
            if (_isCarrying)
            {
                return;
            }

            ReturnHandsToNeutral();
        }

        private void HandlePatientReset(PatientView _)
        {
            if (_isCarrying)
            {
                return;
            }

            ReturnHandsToNeutral();
        }

        private void HandleInteractionAnchorResolved(Transform anchor)
        {
            _currentInteractionAnchor = anchor;
            if (_isCarrying)
            {
                return;
            }

            if (anchor != null)
            {
                ReachForAnchor(anchor);
            }
            else
            {
                ReturnHandsToNeutral();
            }
        }

        private void AttachToCarryable(ICarryable carryable)
        {
            Transform anchor = null;
            if (carryable is Component component)
            {
                anchor = component.transform;
            }

            anchor = anchor != null ? anchor : ResolveActiveAnchor();
            if (anchor == null)
            {
                anchor = _fallbackAnchor != null ? _fallbackAnchor : transform;
            }

            var rightSequence = _rightHand?.Attach(anchor);
            rightSequence?.OnComplete(() =>
            {
                if (_hoverWhenIdle)
                {
                    _rightHand?.HoverLoop();
                }
            });

            var leftSequence = _leftHand?.Reach(anchor, ResolveLeftOffset());
            leftSequence?.OnComplete(() =>
            {
                if (_hoverWhenIdle)
                {
                    _leftHand?.HoverLoop();
                }
            });
        }

        private void ReachForAnchor(Transform anchor)
        {
            if (anchor == null)
            {
                return;
            }

            var rightSequence = _rightHand?.Reach(anchor, ResolveRightOffset());
            rightSequence?.OnComplete(() =>
            {
                if (_hoverWhenIdle && !_isCarrying)
                {
                    _rightHand?.HoverLoop();
                }
            });

            var leftSequence = _leftHand?.Reach(anchor, ResolveLeftOffset());
            leftSequence?.OnComplete(() =>
            {
                if (_hoverWhenIdle && !_isCarrying)
                {
                    _leftHand?.HoverLoop();
                }
            });
        }

        private void ReturnHandsToNeutral()
        {
            _rightHand?.Release(_rightNeutralAnchor);
            _leftHand?.Release(_leftNeutralAnchor);

            if (_hoverWhenIdle)
            {
                _rightHand?.HoverLoop();
                _leftHand?.HoverLoop();
            }
        }

        private Transform ResolveActiveAnchor()
        {
            if (_currentInteractionAnchor != null)
            {
                return _currentInteractionAnchor;
            }

            if (_staff != null)
            {
                var station = _staff.CurrentStation;
                if (station != null)
                {
                    return station.InteractionAnchor != null ? station.InteractionAnchor : station.Transform;
                }
            }

            return _fallbackAnchor;
        }

        private float ResolveLeftOffset()
        {
            if (_leftHand != null)
            {
                var presetOffset = _leftHand.DefaultReachOffsetCm;
                if (!Mathf.Approximately(presetOffset, 0f))
                {
                    return presetOffset;
                }
            }

            return _leftOffsetCm;
        }

        private float ResolveRightOffset()
        {
            if (_rightHand != null)
            {
                var presetOffset = _rightHand.DefaultReachOffsetCm;
                if (!Mathf.Approximately(presetOffset, 0f))
                {
                    return presetOffset;
                }
            }

            return _rightOffsetCm;
        }

        private void SyncState()
        {
            if (_isCarrying && _handsSlot != null)
            {
                AttachToCarryable(_handsSlot.Current);
                return;
            }

            if (_currentInteractionAnchor != null)
            {
                ReachForAnchor(_currentInteractionAnchor);
                return;
            }

            if (_hoverWhenIdle)
            {
                _leftHand?.HoverLoop();
                _rightHand?.HoverLoop();
            }
        }
    }
}
