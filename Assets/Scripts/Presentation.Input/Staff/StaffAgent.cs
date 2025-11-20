// MedMania.Presentation.Input
// StaffAgent.cs
// Responsibility: handle staff interaction inputs (carry/perform) and slot highlighting

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using MedMania.Core.Domain.Inventory;
using MedMania.Core.Domain.Procedures;

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
        [SerializeField] private InputActionReference _carryAction;
        [SerializeField] private InputActionReference _performAction;
        [SerializeField, FormerlySerializedAs("_slotHighlightPrefab")] private GameObject _slotHighlightPrefab;
        [SerializeField] private Camera _viewCamera;
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
            BindInput(_carryAction, OnCarryPerformed);
            BindInput(_performAction, OnPerformRequested);
            EnableAction(_moveAction);
        }

        private void OnDisable()
        {
            _focusedSlot = null;
            _slotHighlight.Clear();
            UnbindInput(_carryAction, OnCarryPerformed);
            UnbindInput(_performAction, OnPerformRequested);
            DisableAction(_moveAction);
            DisableAction(_carryAction);
            DisableAction(_performAction);
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

            var slot = _focusedSlot != null ? _focusedSlot : FindBestSlot();

            if (_hands.IsEmpty)
            {
                if (slot != null && slot.TryTake(out var removed))
                {
                    if (!_hands.TrySwap(removed, out _))
                    {
                        slot.TrySwap(removed, out _);
                    }
                }
            }
            else
            {
                if (slot != null && _hands.TryTake(out var carrying))
                {
                    if (!slot.TrySwap(carrying, out var displaced))
                    {
                        _hands.TrySwap(carrying, out _);
                    }
                    else if (displaced != null)
                    {
                        _hands.TrySwap(displaced, out _);
                    }
                }
            }

            UpdateHeldProcedure();
            RefreshFocusedSlot();
            UpdateAnimator();
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
            IProcedureStation station = null;

            if (_hands != null && !_hands.IsEmpty)
            {
                var current = _hands.Current;
                if (current is IProcedureProvider provider && provider.Procedure != null)
                {
                    procedure = provider.Procedure;
                }
            }

            if (procedure == null)
            {
                station = FindBestStation();
                if (station != null && station.Procedure != null)
                {
                    procedure = station.Procedure;
                }
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

        private void OnPerformRequested(InputAction.CallbackContext _)
        {
            _performRequested?.Invoke(_heldProcedure);
            _performRequestedHandlers?.Invoke(_heldProcedure);
        }

        private void OnCarryPerformed(InputAction.CallbackContext _)
        {
            HandleCarryToggle();
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
            return new Vector3(input.x, 0f, input.y).normalized;
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
