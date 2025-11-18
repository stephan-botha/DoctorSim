// MedMania.Core.Domain
// Inventory interfaces
namespace MedMania.Core.Domain.Inventory
{
    public interface IToolDef { string Name { get; } }
    public interface IEquipmentDef { string Name { get; } }
    public interface ICarryable { string DisplayName { get; } }
}
