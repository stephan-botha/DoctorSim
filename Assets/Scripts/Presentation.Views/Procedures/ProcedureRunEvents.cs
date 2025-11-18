// MedMania.Presentation.Views
// ProcedureRunEvents.cs

using UnityEngine;
using UnityEngine.Events;
using MedMania.Core.Domain.Patients;
using MedMania.Core.Domain.Procedures;

namespace MedMania.Presentation.Views.Procedures
{
    public sealed class ProcedureRunEvents
    {
        private readonly UnityEvent<IProcedureDef> _onStarted;
        private readonly UnityEvent<float> _onProgress;
        private readonly UnityEvent<IProcedureDef> _onCompleted;
        private readonly UnityEvent<Patients.PatientView, IProcedureDef> _onPatientStarted;
        private readonly UnityEvent<Patients.PatientView, float> _onPatientProgress;
        private readonly UnityEvent<Patients.PatientView, IProcedureDef> _onPatientCompleted;
        private readonly UnityEvent<IPatient, IProcedureDef> _onDomainPatientCompleted;
        private readonly UnityEvent<Patients.PatientView> _onPatientReset;
        private readonly UnityEvent<Transform> _onInteractionAnchorResolved;

        public ProcedureRunEvents(
            UnityEvent<IProcedureDef> onStarted,
            UnityEvent<float> onProgress,
            UnityEvent<IProcedureDef> onCompleted,
            UnityEvent<Patients.PatientView, IProcedureDef> onPatientStarted,
            UnityEvent<Patients.PatientView, float> onPatientProgress,
            UnityEvent<Patients.PatientView, IProcedureDef> onPatientCompleted,
            UnityEvent<IPatient, IProcedureDef> onDomainPatientCompleted,
            UnityEvent<Patients.PatientView> onPatientReset,
            UnityEvent<Transform> onInteractionAnchorResolved)
        {
            _onStarted = onStarted;
            _onProgress = onProgress;
            _onCompleted = onCompleted;
            _onPatientStarted = onPatientStarted;
            _onPatientProgress = onPatientProgress;
            _onPatientCompleted = onPatientCompleted;
            _onDomainPatientCompleted = onDomainPatientCompleted;
            _onPatientReset = onPatientReset;
            _onInteractionAnchorResolved = onInteractionAnchorResolved;
        }

        public void NotifyStarted(IProcedureDef procedure, float initialProgress, Patients.PatientView patient, Transform anchor)
        {
            _onProgress?.Invoke(initialProgress);
            if (patient != null)
            {
                _onPatientProgress?.Invoke(patient, initialProgress);
                _onPatientStarted?.Invoke(patient, procedure);
            }

            _onInteractionAnchorResolved?.Invoke(anchor);
            _onStarted?.Invoke(procedure);
        }

        public void NotifyProgress(float progress, Patients.PatientView patient)
        {
            _onProgress?.Invoke(progress);
            if (patient != null)
            {
                _onPatientProgress?.Invoke(patient, progress);
            }
        }

        public void NotifyCompleted(IProcedureDef procedure, Patients.PatientView patient)
        {
            _onProgress?.Invoke(1f);
            if (patient != null)
            {
                _onPatientProgress?.Invoke(patient, 1f);
                _onPatientCompleted?.Invoke(patient, procedure);

                var domainPatient = patient.Domain;
                if (domainPatient != null)
                {
                    _onDomainPatientCompleted?.Invoke(domainPatient, procedure);
                }
            }

            _onCompleted?.Invoke(procedure);
        }

        public void NotifyReset(Patients.PatientView patient)
        {
            if (patient != null)
            {
                _onPatientReset?.Invoke(patient);
            }
        }

        public void ClearInteractionAnchor()
        {
            _onInteractionAnchorResolved?.Invoke(null);
        }
    }
}
