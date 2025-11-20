// MedMania.Presentation.Views
// CarrySlotProcedureInteractionDisplay.cs
// Responsibility: Shows a diegetic prompt on a carry slot when a procedure can be performed on its occupant.

using TMPro;
using UnityEngine;
using MedMania.Core.Domain.Inventory;
using MedMania.Core.Domain.Procedures;
using MedMania.Presentation.Input.Staff;
using MedMania.Presentation.Views.Inventory;
using MedMania.Presentation.Views.Patients;

namespace MedMania.Presentation.Views.Carry
{
    [RequireComponent(typeof(CarrySlot))]
    public sealed class CarrySlotProcedureInteractionDisplay : MonoBehaviour
    {
        [SerializeField] private Canvas _canvas;
        [SerializeField] private TMP_Text _text;
        [SerializeField] private bool _hideOnAwake = true;
        [SerializeField] private StaffAgent _staffAgent;
        [SerializeField] private EquipmentView _equipmentView;

        private CarrySlot _carrySlot;

        private void Awake()
        {
            _carrySlot = GetComponent<CarrySlot>();

            if (_equipmentView == null)
            {
                _equipmentView = GetComponentInParent<EquipmentView>();
            }

            if (_canvas == null)
            {
                _canvas = GetComponentInChildren<Canvas>(true);
            }

            if (_text == null && _canvas != null)
            {
                _text = _canvas.GetComponentInChildren<TMP_Text>(true);
            }

            if (_hideOnAwake)
            {
                SetVisible(false);
            }
        }

        private void OnEnable()
        {
            if (_carrySlot != null)
            {
                _carrySlot.OccupantChanged += OnOccupantChanged;
            }

            RefreshDisplay();
        }

        private void OnDisable()
        {
            if (_carrySlot != null)
            {
                _carrySlot.OccupantChanged -= OnOccupantChanged;
            }
        }

        private void Update()
        {
            RefreshDisplay();
        }

        private void OnOccupantChanged(ICarryable _)
        {
            RefreshDisplay();
        }

        private void RefreshDisplay()
        {
            var staff = ResolveStaffAgent();
            var procedure = staff != null ? staff.HeldProcedure : null;

            if (staff == null || procedure == null || staff.FocusedSlot != _carrySlot || !IsProcedureValidForSlot(procedure))
            {
                SetVisible(false);
                return;
            }

            if (_text != null)
            {
                _text.text = procedure.InteractionText;
            }

            SetVisible(true);
        }

        private StaffAgent ResolveStaffAgent()
        {
            if (_staffAgent != null)
            {
                return _staffAgent;
            }

            _staffAgent = FindObjectOfType<StaffAgent>();
            return _staffAgent;
        }

        private bool IsProcedureValidForSlot(IProcedureDef procedure)
        {
            if (procedure == null || _carrySlot == null)
            {
                return false;
            }

            if (!TryGetPatient(out _))
            {
                return false;
            }

            var requiredEquipment = procedure.RequiredEquipment;
            if (requiredEquipment == null)
            {
                return true;
            }

            var equipmentView = ResolveEquipmentView();
            if (equipmentView == null)
            {
                return false;
            }

            if (equipmentView.PatientSlot != _carrySlot)
            {
                return false;
            }

            return equipmentView.Equipment == requiredEquipment;
        }

        private bool TryGetPatient(out PatientCarryView patient)
        {
            patient = _carrySlot != null ? _carrySlot.Current as PatientCarryView : null;
            return patient != null;
        }

        private EquipmentView ResolveEquipmentView()
        {
            if (_equipmentView != null)
            {
                return _equipmentView;
            }

            _equipmentView = GetComponentInParent<EquipmentView>();
            return _equipmentView;
        }

        private void SetVisible(bool visible)
        {
            if (_canvas == null)
            {
                return;
            }

            var root = _canvas.gameObject;
            if (root.activeSelf != visible)
            {
                root.SetActive(visible);
            }
        }
    }
}
