// MedMania.Presentation.Input
// StaffAgent.cs
// Responsibility: handle staff interaction inputs (carry/perform) and slot highlighting

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using MedMania.Core.Domain.Inventory;
using MedMania.Core.Domain.Procedures;
using MedMania.Core.Domain.Patients;

namespace MedMania.Presentation.Input.Staff
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class StaffAgent : MonoBehaviour, IProcedurePerformer
    {
        [SerializeField] private float _speed = 5f;
        [SerializeField] private Component _handsComponent;
        [SerializeField] private float _interactionRadius = 1.5f;
        [SerializeField] private LayerMask _interactionMask = ~0;
        [SerializeField] private Animator _animator;
        [SerializeField] private string _speedParam = "Speed";
        [SerializeField] private string _carryingParam = "Carrying";
        [SerializeField] private InputActionReference _moveAction;
        [SerializeField, FormerlySerializedAs("_carryAction")] private InputActionReference _interactAction;
        [SerializeField] private InputActionReference _procedurePressAction;
        [SerializeField] private InputActionReference _procedureHoldAction;
        [SerializeField] private InputActionReference _procedureReleaseAction;
        [SerializeField, FormerlySerializedAs("_slotHighlightPrefab")] private GameObject _slotHighlightPrefab;
        [SerializeField] private Camera _viewCamera;
        [SerializeField] private float _procedureWeightLerpSpeed = 8f;
        [NonSerialized] private IProcedureDef _heldProcedure;
        [SerializeField, Tooltip("Runtime debug view of the currently held procedure.")]
        private UnityEngine.Object _heldProcedureDebug;
        [SerializeField] private UnityEvent<IProcedureDef> _performRequested = new();
        [SerializeField] private SlotHighlightController _slotHighlight = new();

        private readonly Collider[] _overlapBuffer = new Collider[16];
        private readonly List<ICarrySlot> _nearbySlots = new();

        private Rigidbody _rb;
        private Vector3 _move;
        private int _speedParamId;
        private int _carryingParamId;
        private event System.Action<IProcedureDef> _performRequestedHandlers;
        private ICarrySlot _focusedSlot;
        private IProcedureStation _cachedStation;
        private IProcedureRunInputContext _activeProcedureContext;
        private IProcedureRunInputContext _holdProcedureContext;
        private IProcedureAnchorHandler _activeAnchorHandler;
        private Coroutine _procedureWeightRoutine;
        private float _currentConstraintWeight;

        private ICarrySlot _hands;

        public IProcedureDef HeldProcedure => _heldProcedure;
        public IProcedureStation CurrentStation => _cachedStation;
        public ICarrySlot HandsSlot => _hands;
        public ICarrySlot FocusedSlot => _focusedSlot;
        public UnityEvent<IProcedureDef> PerformRequested => _performRequested;
        IProcedureDef IProcedurePerformer.HeldProcedure => HeldProcedure;
        event System.Action<IProcedureDef> IProcedurePerformer.PerformRequested
        {
            add => _performRequestedHandlers += value;
            remove => _performRequestedHandlers -= value;
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();

            _hands = ResolveSlot(_handsComponent);
            if (_hands == null)
            {
                _hands = GetComponentInChildren<ICarrySlot>();
                _handsComponent = (_hands as Component) ?? _handsComponent;
            }

            _speedParamId = string.IsNullOrEmpty(_speedParam) ? -1 : Animator.StringToHash(_speedParam);
            _carryingParamId = string.IsNullOrEmpty(_carryingParam) ? -1 : Animator.StringToHash(_carryingParam);

            _slotHighlight.SetPrefab(_slotHighlightPrefab);

            UpdateHeldProcedure();
            UpdateAnimator();
        }

        private void Update()
        {
            _move = ReadMoveInput();

            RefreshFocusedSlot();
            UpdateHeldProcedure();
            UpdateAnimator();
        }

        private void FixedUpdate()
        {
            //if (_rb) _rb.MovePosition(_rb.position + _move * _speed * Time.fixedDeltaTime);
        }

        private void OnValidate()
        {
            _slotHighlight.SetPrefab(_slotHighlightPrefab);
        }

        private void OnEnable()
        {
            BindInput(_interactAction, OnInteractPerformed);
            BindInput(_procedurePressAction, OnProcedurePressPerformed);
            BindInput(_procedureHoldAction, OnProcedureHoldPerformed);
            BindInput(_procedureReleaseAction, OnProcedureReleasePerformed);
            EnableAction(_moveAction);
        }

        private void OnDisable()
        {
            _focusedSlot = null;
            _slotHighlight.Clear();
            UnbindInput(_interactAction, OnInteractPerformed);
            UnbindInput(_procedurePressAction, OnProcedurePressPerformed);
            UnbindInput(_procedureHoldAction, OnProcedureHoldPerformed);
            UnbindInput(_procedureReleaseAction, OnProcedureReleasePerformed);
            DisableAction(_moveAction);
            DisableAction(_interactAction);
            DisableAction(_procedurePressAction);
            DisableAction(_procedureHoldAction);
            DisableAction(_procedureReleaseAction);
            StopProcedureWeightLerp();
            _holdProcedureContext?.SetHoldActive(false);
            UnregisterProcedureContext();
            _holdProcedureContext = null;
            _activeAnchorHandler = null;
        }

        private ICarrySlot ResolveSlot(Component component)
        {
            if (component == null)
            {
                return null;
            }

            if (component is ICarrySlot slot)
            {
                return slot;
            }

            return component.GetComponent<ICarrySlot>();
        }

        private void HandleCarryToggle()
        {
            if (_hands == null) return;

            RefreshFocusedSlot();
            UpdateHeldProcedure();

            var slot = _focusedSlot != null ? _focusedSlot : FindBestSlot();

            if (slot == null)
            {
                FinalizeCarryInteraction();
                return;
            }

            var heldTransporter = GetTransporter(_hands.Current);
            if (heldTransporter != null)
            {
                HandleTransporterInteraction(slot, heldTransporter);
                FinalizeCarryInteraction();
                return;
            }

            if (IsPatientSlot(slot))
            {
                HandlePatientInteraction();
            }
            else if (slot.IsEmpty)
            {
                HandleEmptySlotInteraction(slot);
            }
            else
            {
                HandleToolInteraction(slot);
            }

            FinalizeCarryInteraction();
        }

        private void FinalizeCarryInteraction()
        {
            UpdateHeldProcedure();
            RefreshFocusedSlot();
            UpdateAnimator();
        }

        private bool IsPatientSlot(ICarrySlot slot)
        {
            return slot?.Current is IPatientCarryable;
        }

        private void HandlePatientInteraction()
        {
            if (_heldProcedure == null)
            {
                return;
            }

            _performRequested?.Invoke(_heldProcedure);
            _performRequestedHandlers?.Invoke(_heldProcedure);
        }

        private void HandleEmptySlotInteraction(ICarrySlot slot)
        {
            if (_hands.IsEmpty)
            {
                return;
            }

            if (_hands.Current is ITransporter transporter && transporter.HasPatient)
            {
                return;
            }

            if (!_hands.TryTake(out var carrying) || carrying == null)
            {
                return;
            }

            if (carrying is IPatientCarryable)
            {
                _hands.TrySwap(carrying, out _);
                return;
            }

            if (!slot.TrySwap(carrying, out _))
            {
                _hands.TrySwap(carrying, out _);
            }
        }

        private void HandleToolInteraction(ICarrySlot slot)
        {
            var slotTransporter = GetTransporter(slot.Current);
            if (slotTransporter != null && slotTransporter.HasPatient)
            {
                return;
            }

            if (_hands.IsEmpty)
            {
                if (slot.TryTake(out var removed))
                {
                    if (!TryPlaceInHands(removed))
                    {
                        slot.TrySwap(removed, out _);
                    }
                }

                return;
            }

            if (!_hands.TryTake(out var carrying) || carrying == null)
            {
                return;
            }

            if (!slot.TrySwap(carrying, out var displaced))
            {
                _hands.TrySwap(carrying, out _);
                return;
            }

            if (!TryPlaceInHands(displaced))
            {
                if (slot.TrySwap(displaced, out var reverted))
                {
                    _hands.TrySwap(reverted, out _);
                }
            }
        }

        private void HandleTransporterInteraction(ICarrySlot slot, ITransporter transporter)
        {
            if (transporter == null || slot == null)
            {
                return;
            }

            if (transporter.HasPatient)
            {
                if (slot.IsEmpty && TryUnloadTransporter(transporter, slot))
                {
                    return;
                }

                return;
            }

            if (slot.Current is IPatientCarryable && TryLoadTransporter(transporter, slot))
            {
                return;
            }

            if (slot.IsEmpty)
            {
                HandleEmptySlotInteraction(slot);
            }
        }

        private bool TryLoadTransporter(ITransporter transporter, ICarrySlot sourceSlot)
        {
            if (transporter == null || sourceSlot == null || transporter.HasPatient)
            {
                return false;
            }

            if (!(sourceSlot.Current is IPatientCarryable))
            {
                return false;
            }

            if (!sourceSlot.TryTake(out var carryable) || carryable is not IPatientCarryable patient)
            {
                return false;
            }

            if (transporter.TryLoadPatient(patient))
            {
                return true;
            }

            sourceSlot.TrySwap(patient, out _);
            return false;
        }

        private bool TryUnloadTransporter(ITransporter transporter, ICarrySlot targetSlot)
        {
            if (transporter == null || targetSlot == null || !targetSlot.IsEmpty)
            {
                return false;
            }

            if (!transporter.TryUnloadPatient(out var patient) || patient == null)
            {
                return false;
            }

            if (targetSlot.TrySwap(patient, out _))
            {
                return true;
            }

            transporter.TryLoadPatient(patient);
            return false;
        }

        private static ITransporter GetTransporter(ICarryable carryable)
        {
            return carryable as ITransporter;
        }

        private bool TryPlaceInHands(ICarryable carryable)
        {
            if (carryable is IPatientCarryable)
            {
                return false;
            }

            if (carryable is ITransporter transporter && transporter.HasPatient)
            {
                return false;
            }

            return _hands.TrySwap(carryable, out _);
        }

        private void RefreshFocusedSlot()
        {
            var best = FindSlotAtPointer();
            if (!ReferenceEquals(_focusedSlot, best))
            {
                _focusedSlot = best;
            }

            _slotHighlight.Apply(_focusedSlot);
        }

        private ICarrySlot FindSlotAtPointer()
        {
            var camera = ResolveCamera();
            if (camera == null)
            {
                return null;
            }

            var mouse = Mouse.current;
            var screenPosition = mouse != null
                ? mouse.position.ReadValue()
                : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            var ray = camera.ScreenPointToRay(screenPosition);

            int mask = _interactionMask == 0 ? Physics.DefaultRaycastLayers : _interactionMask;
            if (Physics.Raycast(ray, out var hit, _interactionRadius, mask, QueryTriggerInteraction.Collide))
            {
                var slot = hit.collider.GetComponentInParent<ICarrySlot>();
                if (slot != null && slot != _hands)
                {
                    return slot;
                }
            }

            return null;
        }

        private Camera ResolveCamera()
        {
            if (_viewCamera != null)
            {
                return _viewCamera;
            }

            if (Camera.main != null)
            {
                _viewCamera = Camera.main;
                return _viewCamera;
            }

            return null;
        }

        private ICarrySlot FindBestSlot()
        {
            CleanupNearbySlots();

            ICarrySlot best = null;
            float bestDist = float.MaxValue;

            foreach (var slot in _nearbySlots)
            {
                float dist = Vector3.Distance(transform.position, slot.Transform.position);
                if (dist < bestDist)
                {
                    best = slot;
                    bestDist = dist;
                }
            }

            if (best != null) return best;

            int count = Physics.OverlapSphereNonAlloc(transform.position, _interactionRadius, _overlapBuffer, _interactionMask, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                var collider = _overlapBuffer[i];
                if (!collider) continue;
                if (!IsLayerValid(collider.gameObject.layer)) continue;

                var slot = collider.GetComponentInParent<ICarrySlot>();
                if (slot == null || slot == _hands) continue;

                float dist = Vector3.Distance(transform.position, slot.Transform.position);
                if (dist < bestDist)
                {
                    best = slot;
                    bestDist = dist;
                }
            }

            return best;
        }

        private void CleanupNearbySlots()
        {
            for (int i = _nearbySlots.Count - 1; i >= 0; i--)
            {
                if (_nearbySlots[i] == null)
                {
                    _nearbySlots.RemoveAt(i);
                }
            }
        }

        private bool IsLayerValid(int layer)
        {
            return _interactionMask == 0 || ((_interactionMask.value & (1 << layer)) != 0);
        }

        private void UpdateHeldProcedure()
        {
            IProcedureDef procedure = null;
            var station = FindBestStation();

            if (_hands != null && !_hands.IsEmpty)
            {
                var current = _hands.Current;
                if (current is IProcedureProvider provider && provider.Procedure != null)
                {
                    procedure = provider.Procedure;
                }
            }

            if (procedure == null && station != null && station.Procedure != null)
            {
                procedure = station.Procedure;
            }

            _cachedStation = station;
            _heldProcedure = procedure;
            _heldProcedureDebug = procedure as UnityEngine.Object;
        }

        private IProcedureStation FindBestStation()
        {
            IProcedureStation best = null;
            float bestDist = float.MaxValue;
            var origin = transform.position;

            var active = ProcedureStationRegistry.Stations;
            for (int i = 0; i < active.Count; i++)
            {
                var equipment = active[i];
                if (equipment == null)
                {
                    continue;
                }

                if (!equipment.IsPatientReady)
                {
                    continue;
                }

                var anchor = equipment.InteractionAnchor;
                var anchorPos = anchor != null ? anchor.position : equipment.Transform.position;
                float dist = Vector3.Distance(origin, anchorPos);
                if (dist > _interactionRadius)
                {
                    continue;
                }

                if (dist < bestDist)
                {
                    best = equipment;
                    bestDist = dist;
                }
            }

            return best;
        }

        private void UpdateAnimator()
        {
            if (_animator == null) return;

            if (_speedParamId >= 0)
            {
                _animator.SetFloat(_speedParamId, _move.magnitude);
            }

            if (_carryingParamId >= 0)
            {
                _animator.SetBool(_carryingParamId, _hands != null && !_hands.IsEmpty);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsLayerValid(other.gameObject.layer)) return;

            var slot = other.GetComponentInParent<ICarrySlot>();
            if (slot != null && slot != _hands && !_nearbySlots.Contains(slot))
            {
                _nearbySlots.Add(slot);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var slot = other.GetComponentInParent<ICarrySlot>();
            if (slot != null)
            {
                _nearbySlots.Remove(slot);
            }
        }

        private void OnDestroy()
        {
            _slotHighlight.Dispose();
        }

        private void OnInteractPerformed(InputAction.CallbackContext _)
        {
            if (TryRequestProcedure())
            {
                return;
            }

            HandleCarryToggle();
        }

        private void OnProcedurePressPerformed(InputAction.CallbackContext _)
        {
            if (!TryPrepareProcedureRun(out var context, out var anchorHandler, out var procedure))
            {
                return;
            }

            if (!context.TryValidateTarget(procedure, out var anchor))
            {
                StartProcedureWeightLerp(anchorHandler, 0f);
                return;
            }

            RegisterProcedureContext(context);
            _activeAnchorHandler = anchorHandler;

            _holdProcedureContext = context;
            context.SetHoldActive(true);

            anchorHandler?.ApplyInteractionAnchor(anchor);
            StartProcedureWeightLerp(anchorHandler, 1f);
            RequestProcedure(procedure);
        }

        private void OnProcedureHoldPerformed(InputAction.CallbackContext _)
        {
            if (_activeAnchorHandler != null)
            {
                _activeAnchorHandler.SetConstraintWeight(1f);
                _currentConstraintWeight = 1f;
            }
        }

        private void OnProcedureReleasePerformed(InputAction.CallbackContext _)
        {
            if (_activeAnchorHandler != null)
            {
                StartProcedureWeightLerp(_activeAnchorHandler, 0f);
            }

            var holdContext = _activeProcedureContext ?? _holdProcedureContext;
            holdContext?.SetHoldActive(false);

            UnregisterProcedureContext();
            _holdProcedureContext = null;
            _activeAnchorHandler = null;
        }

        private bool TryRequestProcedure()
        {
            if (_cachedStation == null || _cachedStation.Procedure == null || _heldProcedure == null)
            {
                return false;
            }

            _performRequested?.Invoke(_heldProcedure);
            _performRequestedHandlers?.Invoke(_heldProcedure);
            return true;
        }

        private bool TryPrepareProcedureRun(out IProcedureRunInputContext context, out IProcedureAnchorHandler anchorHandler, out IProcedureDef procedure)
        {
            context = null;
            anchorHandler = null;
            procedure = _heldProcedure;

            if (procedure == null)
            {
                return false;
            }

            if (_hands?.Current is not Component component)
            {
                return false;
            }

            context = component.GetComponentInParent<IProcedureContextSource>()?.ProcedureContext;
            anchorHandler = component.GetComponentInParent<IProcedureAnchorHandler>();

            if (context == null)
            {
                return false;
            }

            return true;
        }

        private void RequestProcedure(IProcedureDef procedure)
        {
            _performRequested?.Invoke(procedure);
            _performRequestedHandlers?.Invoke(procedure);
        }

        private void RegisterProcedureContext(IProcedureRunInputContext context)
        {
            if (ReferenceEquals(_activeProcedureContext, context))
            {
                return;
            }

            UnregisterProcedureContext();

            _activeProcedureContext = context;
            if (_activeProcedureContext != null)
            {
                _activeProcedureContext.RunCompleted += HandleProcedureRunCompleted;
            }
        }

        private void UnregisterProcedureContext()
        {
            if (_activeProcedureContext != null)
            {
                _activeProcedureContext.RunCompleted -= HandleProcedureRunCompleted;
            }

            _activeProcedureContext = null;
        }

        private void HandleProcedureRunCompleted(IProcedureDef _)
        {
            if (_activeAnchorHandler != null)
            {
                StartProcedureWeightLerp(_activeAnchorHandler, 0f);
                _activeAnchorHandler = null;
            }

            _currentConstraintWeight = 0f;
            UnregisterProcedureContext();
        }

        private void StartProcedureWeightLerp(IProcedureAnchorHandler handler, float targetWeight)
        {
            if (handler == null)
            {
                return;
            }

            StopProcedureWeightLerp();
            _procedureWeightRoutine = StartCoroutine(LerpProcedureWeight(handler, targetWeight));
        }

        private void StopProcedureWeightLerp()
        {
            if (_procedureWeightRoutine == null)
            {
                return;
            }

            StopCoroutine(_procedureWeightRoutine);
            _procedureWeightRoutine = null;
        }

        private IEnumerator LerpProcedureWeight(IProcedureAnchorHandler handler, float targetWeight)
        {
            float speed = _procedureWeightLerpSpeed <= 0f ? 1f : _procedureWeightLerpSpeed;
            float current = _currentConstraintWeight;
            targetWeight = Mathf.Clamp01(targetWeight);

            while (!Mathf.Approximately(current, targetWeight))
            {
                current = Mathf.MoveTowards(current, targetWeight, speed * Time.deltaTime);
                handler.SetConstraintWeight(current);
                _currentConstraintWeight = current;
                yield return null;
            }

            handler.SetConstraintWeight(targetWeight);
            _currentConstraintWeight = targetWeight;
            _procedureWeightRoutine = null;
        }

        private void BindInput(InputActionReference actionRef, System.Action<InputAction.CallbackContext> callback)
        {
            if (actionRef == null || callback == null)
            {
                return;
            }

            var action = actionRef.action;
            if (action == null)
            {
                return;
            }

            action.performed += callback;
            EnableAction(actionRef);
        }

        private void UnbindInput(InputActionReference actionRef, System.Action<InputAction.CallbackContext> callback)
        {
            if (actionRef == null || callback == null)
            {
                return;
            }

            var action = actionRef.action;
            if (action == null)
            {
                return;
            }

            action.performed -= callback;
        }

        private static void EnableAction(InputActionReference actionRef)
        {
            var action = actionRef != null ? actionRef.action : null;
            if (action != null && !action.enabled)
            {
                action.Enable();
            }
        }

        private static void DisableAction(InputActionReference actionRef)
        {
            var action = actionRef != null ? actionRef.action : null;
            if (action != null && action.enabled)
            {
                action.Disable();
            }
        }

        private Vector3 ReadMoveInput()
        {
            var action = _moveAction != null ? _moveAction.action : null;
            if (action == null)
            {
                return Vector3.zero;
            }

            var input = action.ReadValue<Vector2>();
            var move = new Vector3(input.x, 0f, input.y);
            if (move.sqrMagnitude > 1f)
            {
                move.Normalize();
            }

            return move * ResolveSpeedModifier();
        }

        private float ResolveSpeedModifier()
        {
            var transporter = GetTransporter(_hands?.Current);
            if (transporter == null)
            {
                return 1f;
            }

            var modifier = transporter.SpeedModifier;
            return modifier <= 0f ? 1f : modifier;
        }

        [Serializable]
        private sealed class SlotHighlightController
        {
            [SerializeField] private GameObject _slotHighlightPrefab;
            private GameObject _slotHighlightInstance;

            public void Apply(ICarrySlot slot)
            {
                if (slot == null)
                {
                    if (_slotHighlightInstance != null && _slotHighlightInstance.activeSelf)
                    {
                        _slotHighlightInstance.SetActive(false);
                    }

                    return;
                }

                if (_slotHighlightPrefab == null)
                {
                    return;
                }

                if (_slotHighlightInstance == null)
                {
                    _slotHighlightInstance = GameObject.Instantiate(_slotHighlightPrefab);
                }

                if (_slotHighlightInstance == null)
                {
                    return;
                }

                var highlightTransform = _slotHighlightInstance.transform;
                var slotTransform = slot.Transform;

                if (highlightTransform.parent != slotTransform)
                {
                    highlightTransform.SetParent(slotTransform, false);
                }

                highlightTransform.localPosition = Vector3.zero;

                if (!_slotHighlightInstance.activeSelf)
                {
                    _slotHighlightInstance.SetActive(true);
                }
            }

            public void Clear()
            {
                Apply(null);
            }

            public void Dispose()
            {
                if (_slotHighlightInstance == null)
                {
                    return;
                }

                if (Application.isPlaying)
                {
                    GameObject.Destroy(_slotHighlightInstance);
                }
                else
                {
                    GameObject.DestroyImmediate(_slotHighlightInstance);
                }

                _slotHighlightInstance = null;
            }

            public void SetPrefab(GameObject prefab)
            {
                _slotHighlightPrefab = prefab;
            }
        }
    }
}
