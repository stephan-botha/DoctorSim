// MedMania.Presentation.Input
// ICarrySlot.cs
// Responsibility: Abstraction for carry slot interactions used by input agents.

using System;
using UnityEngine;
using MedMania.Core.Domain.Inventory;

namespace MedMania.Presentation.Input.Staff
{
    /// <summary>
    /// View-agnostic contract for slot components that can hold carryable items.
    /// Allows input agents to manipulate slots without depending on concrete view types.
    /// </summary>
    public interface ICarrySlot
    {
        bool IsEmpty { get; }
        ICarryable Current { get; }
        event Action<ICarryable> OccupantChanged;
        bool TryTake(out ICarryable carryable);
        bool TrySwap(ICarryable incoming, out ICarryable removed);
        Transform Transform { get; }
    }
}
