// MedMania.Presentation.Views
// EquipmentView.cs
// Responsibility: Scene representation for stationary equipment stations and associated procedure metadata.

using System.Collections.Generic;
using UnityEngine;
using MedMania.Core.Data.ScriptableObjects;
using MedMania.Core.Domain.Inventory;
using MedMania.Core.Domain.Procedures;
using MedMania.Core.Domain.Patients;
using MedMania.Presentation.Input.Staff;
using MedMania.Presentation.Views.Carry;
using MedMania.Presentation.Views.Patients;

namespace MedMania.Presentation.Views.Inventory
{
    /// <summary>
    /// Scene-side representation for a fixed equipment station. Equipment views are stationary fixtures
    /// and must never implement <see cref="ICarryable"/>; instead they expose metadata and optional
    /// interaction anchors for other systems.
    /// </summary>
    public sealed class EquipmentView : MonoBehaviour, IProcedureStation
    {
        private static readonly List<EquipmentView> s_Active = new List<EquipmentView>();

        [SerializeField] private EquipmentSO _equipment;
        [SerializeField] private ProcedureSOBase _procedure;
        [SerializeField] private CarrySlot _patientSlot;
        [SerializeField] private Transform _interactionAnchor;

        /// <summary>Gets the backing equipment asset assigned to this station.</summary>
        public EquipmentSO EquipmentAsset => _equipment;

        /// <summary>Gets the equipment definition exposed to domain systems.</summary>
        public IEquipmentDef Equipment => _equipment;

        /// <summary>Gets the associated procedure asset enabled by this equipment.</summary>
        public ProcedureSOBase ProcedureAsset => _procedure;

        /// <summary>Gets the procedure definition for domain logic.</summary>
        public IProcedureDef Procedure => _procedure;

        /// <summary>Gets the slot used to hold a patient while interacting with this equipment.</summary>
        public CarrySlot PatientSlot => _patientSlot;

        /// <summary>Gets an optional transform that should be used for interaction anchoring.</summary>
        public Transform InteractionAnchor => _interactionAnchor != null ? _interactionAnchor : transform;

        /// <summary>Gets a read-only collection of all active equipment views.</summary>
        public static IReadOnlyList<EquipmentView> Active => s_Active;

        bool IProcedureStation.IsPatientReady => _patientSlot != null && _patientSlot.Current is IPatientCarryable;

        Transform IProcedureStation.InteractionAnchor => InteractionAnchor;

        Transform IProcedureStation.Transform => transform;

        IProcedureDef IProcedureProvider.Procedure => Procedure;

        private void OnEnable()
        {
            if (!s_Active.Contains(this))
            {
                s_Active.Add(this);
            }

            ProcedureStationRegistry.Register(this);
        }

        private void OnDisable()
        {
            s_Active.Remove(this);
            ProcedureStationRegistry.Unregister(this);
        }

        /// <summary>Tries to fetch the patient view currently occupying the carry slot.</summary>
        public bool TryGetPatient(out PatientView view)
        {
            view = null;
            if (_patientSlot == null)
            {
                return false;
            }

            if (_patientSlot.Current is IPatientCarryable patientCarry && patientCarry is Component component)
            {
                if (patientCarry is PatientCarryView patientCarryView && patientCarryView.View != null)
                {
                    view = patientCarryView.View;
                    return true;
                }

                if (component.TryGetComponent(out PatientView patientView))
                {
                    view = patientView;
                    return true;
                }
            }

            return false;
        }
    }
}
