// MedMania.Core.Domain
// ITransporterDef.cs
// Responsibility: Defines transporter metadata used for movement modifiers and inventory display.

namespace MedMania.Core.Domain.Inventory
{
    public interface ITransporterDef
    {
        string Name { get; }
        float SpeedModifier { get; }
    }
}
