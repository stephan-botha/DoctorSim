// MedMania.Presentation.Input
// ITransporter.cs
// Responsibility: Abstraction for carryable transporters that can load patients and apply movement modifiers.

using MedMania.Core.Domain.Inventory;
using MedMania.Core.Domain.Patients;

namespace MedMania.Presentation.Input.Staff
{
    public interface ITransporter : ICarryable
    {
        /// <summary>Gets the transporter definition providing display data and movement modifiers.</summary>
        ITransporterDef Transporter { get; }

        /// <summary>Gets the movement speed modifier applied while the transporter is held.</summary>
        float SpeedModifier { get; }

        /// <summary>Indicates whether the transporter currently holds a patient.</summary>
        bool HasPatient { get; }

        /// <summary>Attempts to load a patient into the transporter.</summary>
        bool TryLoadPatient(IPatientCarryable patient);

        /// <summary>Attempts to unload the held patient, if any.</summary>
        bool TryUnloadPatient(out IPatientCarryable patient);
    }
}
