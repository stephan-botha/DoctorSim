// MedMania.Core.Domain.Procedures
// IProcedurePerformer.cs
// Responsibility: Abstraction for actors that can request procedures to run.

namespace MedMania.Core.Domain.Procedures
{
    public interface IProcedurePerformer
    {
        IProcedureDef HeldProcedure { get; }
        event System.Action<IProcedureDef> PerformRequested;
    }
}
