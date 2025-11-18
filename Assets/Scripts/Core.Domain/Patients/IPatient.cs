// MedMania.Core.Domain
// IPatient.cs
// Responsibility: Contract for patient state and operations.
// API: Read-only properties for identity and state; methods to begin/end procedures.
// Usage: Implemented by Patient. Consumed by Views/UI/Services. Testable without Unity.

using System;

namespace MedMania.Core.Domain.Patients
{
    public enum PatientState
    {
        Waiting,
        UnderTest,
        Diagnosed,
        UnderTreatment,
        ReadyForDischarge,
        Discharged
    }

    public interface IPatient
    {
        Guid Id { get; }
        string DisplayName { get; }
        PatientState State { get; }
        MedMania.Core.Domain.Diseases.IDiseaseDef Disease { get; }
        bool DiagnosisKnown { get; }
        int CompletedTestCount { get; }
        int TotalTestCount { get; }
        bool AreAllTestsCompleted { get; }

        bool TryBeginProcedure(MedMania.Core.Domain.Procedures.IProcedureDef procedure);
        void CompleteProcedure(MedMania.Core.Domain.Procedures.IProcedureDef procedure);
        void CancelActiveProcedure();
        bool TryDischarge();
    }
}
