// MedMania.Presentation.Input
// IProcedureContextSource.cs
// Responsibility: Exposes a procedure context for input systems without tying to concrete view components.

using MedMania.Core.Domain.Procedures;

namespace MedMania.Presentation.Input.Staff
{
    public interface IProcedureContextSource
    {
        IProcedureRunInputContext ProcedureContext { get; }
    }
}
