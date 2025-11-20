// MedMania.Presentation.Views
// PatientCarryView.cs
// Responsibility: Adapter allowing PatientView to be treated as an ICarryable scene object.

using UnityEngine;
using MedMania.Core.Domain.Inventory;
using MedMania.Core.Domain.Patients;

namespace MedMania.Presentation.Views.Patients
{
    [RequireComponent(typeof(PatientView))]
    public sealed class PatientCarryView : MonoBehaviour, IPatientCarryable
    {
        [SerializeField] private PatientView _patientView;

        public PatientView View => _patientView;
        public IPatient Patient => _patientView ? _patientView.Domain : null;
        public string DisplayName => Patient?.DisplayName ?? name;

        private void Awake()
        {
            if (_patientView == null)
            {
                _patientView = GetComponent<PatientView>();
            }
        }
    }
}
