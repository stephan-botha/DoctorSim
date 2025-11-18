// MedMania.Core.Domain
// IDiseaseDef.cs
// Responsibility: Contract for disease data the domain consumes (tests & treatments).

using MedMania.Core.Domain.Procedures;

namespace MedMania.Core.Domain.Diseases
{
    public interface IDiseaseDef
    {
        string Name { get; }
        IProcedureDef[] Tests { get; }
        IProcedureDef[] Treatments { get; }
        float MaxWaitSeconds { get; }
    }
}
