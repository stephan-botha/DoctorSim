// MedMania.Core.Domain
// ProcedureRun.cs
// Responsibility: Pure coordinator that uses IPatient + IProcedureDef + IProcedureContext.

using System;
namespace MedMania.Core.Domain.Procedures
{
    public static class ProcedureRun
    {
        public static IDisposable TryRun(
            MedMania.Core.Domain.Patients.IPatient patient,
            IProcedureDef procedure,
            IProcedureContext context,
            Action onBegan = null,
            Action onCompleted = null)
        {
            var requiredEquipment = procedure.RequiredEquipment;
            if (requiredEquipment != null && !context.IsEquipmentAvailable(requiredEquipment))
            {
                return null;
            }

            if (!patient.TryBeginProcedure(procedure)) return null;

            onBegan?.Invoke();
            var runHandle = new RunHandle(patient, procedure, onCompleted);
            var scheduled = context.Schedule(TimeSpan.FromSeconds(procedure.DurationSeconds), runHandle.OnCompleted);
            runHandle.BindScheduledHandle(scheduled);

            return runHandle;
        }

        private sealed class RunHandle : IDisposable
        {
            private readonly MedMania.Core.Domain.Patients.IPatient _patient;
            private readonly IProcedureDef _procedure;
            private readonly Action _onCompleted;
            private IDisposable _scheduled;
            private bool _disposed;
            private bool _finished;

            public RunHandle(MedMania.Core.Domain.Patients.IPatient patient, IProcedureDef procedure, Action onCompleted)
            {
                _patient = patient;
                _procedure = procedure;
                _onCompleted = onCompleted;
            }

            public void BindScheduledHandle(IDisposable scheduled)
            {
                if (_disposed || _finished)
                {
                    scheduled?.Dispose();
                    return;
                }

                _scheduled = scheduled;
            }

            public void OnCompleted()
            {
                if (_disposed || _finished) return;

                _scheduled = null;
                _finished = true;
                _patient.CompleteProcedure(_procedure);
                _onCompleted?.Invoke();
            }

            public void Dispose()
            {
                if (_disposed) return;

                _disposed = true;
                _scheduled?.Dispose();
                _scheduled = null;

                if (_finished) return;

                _patient.CancelActiveProcedure();
            }
        }
    }
}
