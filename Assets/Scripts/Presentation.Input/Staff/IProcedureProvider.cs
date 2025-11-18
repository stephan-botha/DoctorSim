// MedMania.Presentation.Input
// IProcedureProvider.cs
// Responsibility: Contract for objects that expose a procedure definition.

using MedMania.Core.Domain.Procedures;

namespace MedMania.Presentation.Input.Staff
{
    /// <summary>
    /// Represents any object that can provide a procedure definition for input-driven interactions.
    /// </summary>
    public interface IProcedureProvider
    {
        IProcedureDef Procedure { get; }
    }
}
