// MedMania.Presentation.Input
// IProcedureStation.cs
// Responsibility: Contract describing procedure stations that staff can interact with.

using UnityEngine;
using MedMania.Core.Domain.Procedures;

namespace MedMania.Presentation.Input.Staff
{
    /// <summary>
    /// Represents a stationary interaction point that can host patients and trigger procedures.
    /// </summary>
    public interface IProcedureStation : IProcedureProvider
    {
        /// <summary>True when the station has a patient ready for interaction.</summary>
        bool IsPatientReady { get; }

        /// <summary>Position anchor that staff should use for interaction distance checks.</summary>
        Transform InteractionAnchor { get; }

        /// <summary>Transform used for fallback distance calculations when no anchor is provided.</summary>
        Transform Transform { get; }
    }
}
