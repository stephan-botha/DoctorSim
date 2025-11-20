// MedMania.Core.Domain
// IPatientCarryable.cs
// Responsibility: Marker for carryable patient representations.

using MedMania.Core.Domain.Inventory;

namespace MedMania.Core.Domain.Patients
{
    /// <summary>
    /// Identifies a carryable scene object that represents a patient.
    /// </summary>
    public interface IPatientCarryable : ICarryable
    {
        /// <summary>The domain patient associated with this carryable object.</summary>
        IPatient Patient { get; }
    }
}
