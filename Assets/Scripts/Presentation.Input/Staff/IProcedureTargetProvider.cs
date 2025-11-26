// MedMania.Presentation.Input
// IProcedureTargetProvider.cs
// Responsibility: Bridge for input-layer raycasts to resolve procedure-specific interaction targets.

using UnityEngine;
using MedMania.Core.Domain.Procedures;

namespace MedMania.Presentation.Input.Staff
{
    public interface IProcedureTargetProvider
    {
        bool TryGetTarget(IProcedureDef procedure, out Transform target);
        bool TryGetTarget(Collider sourceCollider, IProcedureDef procedure, out Transform target);
    }
}
