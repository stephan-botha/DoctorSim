// MedMania.Presentation.Input
// IProcedureAnchorHandler.cs
// Responsibility: Allows input to drive interaction anchor + constraint weight updates decoupled from view implementations.

using UnityEngine;

namespace MedMania.Presentation.Input.Staff
{
    public interface IProcedureAnchorHandler
    {
        void ApplyInteractionAnchor(Transform anchor);
        void SetConstraintWeight(float weight);
    }
}
