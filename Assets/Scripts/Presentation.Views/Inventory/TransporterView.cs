// MedMania.Presentation.Views
// TransporterView.cs
// Responsibility: Scene representation for transporters that carry patients and alter movement speed.

using UnityEngine;
using MedMania.Core.Data.ScriptableObjects;
using MedMania.Core.Domain.Inventory;
using MedMania.Core.Domain.Patients;
using MedMania.Presentation.Input.Staff;
using MedMania.Presentation.Views.Carry;

namespace MedMania.Presentation.Views.Inventory
{
    public sealed class TransporterView : MonoBehaviour, ICarryable, ITransporter
    {
        [SerializeField] private TransporterSO _transporter;
        [SerializeField] private CarrySlot _patientSlot;

        public TransporterSO TransporterAsset => _transporter;
        public ITransporterDef Transporter => _transporter;
        public bool HasPatient => _patientSlot != null && !_patientSlot.IsEmpty;
        public float SpeedModifier => _transporter != null ? _transporter.SpeedModifier : 1f;
        public string DisplayName => _transporter != null ? _transporter.Name : name;

        public bool TryLoadPatient(IPatientCarryable patient)
        {
            if (_patientSlot == null || patient == null || !_patientSlot.IsEmpty)
            {
                return false;
            }

            return _patientSlot.TrySwap(patient, out _);
        }

        public bool TryUnloadPatient(out IPatientCarryable patient)
        {
            patient = null;
            if (_patientSlot == null || _patientSlot.IsEmpty)
            {
                return false;
            }

            if (!_patientSlot.TryTake(out var carryable))
            {
                return false;
            }

            patient = carryable as IPatientCarryable;
            if (patient == null)
            {
                _patientSlot.TrySwap(carryable, out _);
                return false;
            }

            return true;
        }
    }
}
