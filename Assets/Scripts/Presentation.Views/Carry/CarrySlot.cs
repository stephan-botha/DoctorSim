// MedMania.Presentation.Views
// CarrySlot.cs
// Responsibility: Simple holder that parents ICarryable scene objects to a slot anchor.

using System;
using UnityEngine;
using MedMania.Core.Domain.Inventory;
using MedMania.Presentation.Input.Staff;

namespace MedMania.Presentation.Views.Carry
{
    [DisallowMultipleComponent]
    public sealed class CarrySlot : MonoBehaviour, ICarrySlot
    {
        [SerializeField] private Transform _anchor;
        [SerializeField] private bool _captureExistingChild = true;

        private ICarryable _current;
        private Transform _currentTransform;

        public bool IsEmpty => _current == null;
        public ICarryable Current => _current;
        public Transform CurrentTransform => _currentTransform;
        public Transform Anchor => _anchor ? _anchor : transform;
        public event Action<ICarryable> OccupantChanged;

        Transform ICarrySlot.Transform => transform;

        private void Awake()
        {
            if (_anchor == null)
            {
                _anchor = transform;
            }

            if (_captureExistingChild && _current == null)
            {
                RefreshFromAnchor();
            }
        }

        /// <summary>Places a carryable into this slot if it is currently empty.</summary>
        public bool TryPlace(ICarryable carryable)
        {
            if (!IsEmpty)
            {
                return false;
            }

            if (!Attach(carryable))
            {
                return false;
            }

            RaiseOccupantChanged();
            return true;
        }

        /// <summary>Swaps the current item with the provided carryable (or just places if empty).</summary>
        public bool TrySwap(ICarryable incoming, out ICarryable removed)
        {
            removed = null;
            if (incoming == null) return false;
            if (!(incoming is Component component)) return false;

            var previous = _current;
            var previousTransform = _currentTransform;

            if (!AttachComponent(component, incoming))
            {
                return false;
            }

            var newTransform = _currentTransform;
            if (previousTransform && previousTransform != newTransform)
            {
                previousTransform.SetParent(null, true);
            }

            removed = ReferenceEquals(previous, incoming) ? null : previous;
            RaiseOccupantChanged();
            return true;
        }

        /// <summary>Takes the current item out of the slot.</summary>
        public bool TryTake(out ICarryable carryable)
        {
            if (_current == null)
            {
                carryable = null;
                return false;
            }

            carryable = _current;
            if (_currentTransform)
            {
                _currentTransform.SetParent(null, true);
            }

            _current = null;
            _currentTransform = null;
            RaiseOccupantChanged();
            return true;
        }

        /// <summary>Clears any tracked item without returning it.</summary>
        public void Clear()
        {
            if (_currentTransform)
            {
                _currentTransform.SetParent(null, true);
            }

            _current = null;
            _currentTransform = null;
            RaiseOccupantChanged();
        }

        private bool Attach(ICarryable carryable)
        {
            if (carryable == null) return false;
            if (carryable is Component component)
            {
                return AttachComponent(component, carryable);
            }

            return false;
        }

        private bool AttachComponent(Component component, ICarryable carryable)
        {
            if (component == null)
            {
                return false;
            }

            var anchor = Anchor;
            var componentTransform = component.transform;
            componentTransform.SetParent(anchor, false);
            componentTransform.localPosition = Vector3.zero;
            componentTransform.localRotation = Quaternion.identity;

            _current = carryable;
            _currentTransform = componentTransform;

            return true;
        }

        private void RefreshFromAnchor()
        {
            var anchor = Anchor;
            for (int i = 0; i < anchor.childCount; i++)
            {
                var child = anchor.GetChild(i);
                if (child.TryGetComponent<ICarryable>(out var carryable))
                {
                    _current = carryable;
                    _currentTransform = child;
                    RaiseOccupantChanged();
                    return;
                }
            }
        }

        private void RaiseOccupantChanged()
        {
            OccupantChanged?.Invoke(_current);
        }
    }
}
