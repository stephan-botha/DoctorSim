// MedMania.Presentation.Views
// CarrySlotInteractionDisplay.cs
// Responsibility: Show diegetic interaction text when a carry slot is focused with a performable procedure.

using TMPro;
using UnityEngine;
using MedMania.Core.Domain.Procedures;
using MedMania.Presentation.Input.Staff;

namespace MedMania.Presentation.Views.Carry
{
    [RequireComponent(typeof(CarrySlot))]
    public sealed class CarrySlotInteractionDisplay : MonoBehaviour
    {
        [SerializeField] private Canvas _canvas;
        [SerializeField] private TMP_Text _text;
        [SerializeField] private bool _hideOnAwake = true;
        [SerializeField] private StaffAgent _staffAgent;

        private CarrySlot _carrySlot;

        private void Awake()
        {
            _carrySlot = GetComponent<CarrySlot>();

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

        private void Update()
        {
            EnsureStaffAgent();

            var procedure = _staffAgent != null ? _staffAgent.HeldProcedure : null;
            bool isFocused = _staffAgent != null && ReferenceEquals(_staffAgent.FocusedSlot, _carrySlot);
            bool shouldShow = isFocused && IsPerformable(procedure);

            if (shouldShow && _text != null)
            {
                _text.text = ResolveText(procedure);
            }

            SetVisible(shouldShow);
        }

        private void SetVisible(bool visible)
        {
            if (_canvas == null)
            {
                return;
            }

            if (_canvas.gameObject.activeSelf != visible)
            {
                _canvas.gameObject.SetActive(visible);
            }
        }

        private string ResolveText(IProcedureDef procedure)
        {
            if (procedure == null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrEmpty(procedure.InteractionText))
            {
                return procedure.InteractionText;
            }

            return procedure.Name;
        }

        private bool IsPerformable(IProcedureDef procedure)
        {
            if (procedure == null)
            {
                return false;
            }

            return procedure.Kind == ProcedureKind.Test || procedure.Kind == ProcedureKind.Treatment;
        }

        private void EnsureStaffAgent()
        {
            if (_staffAgent != null)
            {
                return;
            }

            _staffAgent = FindObjectOfType<StaffAgent>();
        }
    }
}
