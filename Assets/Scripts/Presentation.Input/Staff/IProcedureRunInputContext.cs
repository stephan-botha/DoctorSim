// MedMania.Presentation.Input
// IProcedureRunInputContext.cs
// Responsibility: Bridges input-driven procedure requests to runtime contexts/runners.

using System;
using UnityEngine;
using MedMania.Core.Domain.Procedures;

namespace MedMania.Presentation.Input.Staff
{
    public interface IProcedureRunInputContext : IProcedureContext
    {
        bool TryValidateTarget(IProcedureDef procedure, out Transform interactionAnchor);
        bool TryCancelActiveRun();
        bool HasActiveRun { get; }
        void SetHoldActive(bool isHeld);

        event Action<IProcedureDef> RunStarted;
        event Action<IProcedureDef> RunCompleted;
    }
}
