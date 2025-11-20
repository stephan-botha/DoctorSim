// MedMania.Core.Domain
// IProcedureDef.cs
// Responsibility: Contract describing a test or treatment.

using MedMania.Core.Domain.Inventory;

namespace MedMania.Core.Domain.Procedures
{
    public interface IProcedureDef
    {
        string Name { get; }
        ProcedureKind Kind { get; }
        float DurationSeconds { get; }

        IToolDef RequiredTool { get; }
        IEquipmentDef RequiredEquipment { get; }
    }

    public enum ProcedureKind { Test, Treatment }
}
