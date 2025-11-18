// MedMania.Core.Domain
// IProcedureContext.cs
// Responsibility: Timer/cancellation interface supplied by runner/services.

using System;
using System.Threading;
using MedMania.Core.Domain.Inventory;

namespace MedMania.Core.Domain.Procedures
{
    public interface IProcedureContext
    {
        IDisposable Schedule(TimeSpan duration, Action onCompleted, CancellationToken token = default);
        bool IsEquipmentAvailable(IEquipmentDef equipment);
    }
}
