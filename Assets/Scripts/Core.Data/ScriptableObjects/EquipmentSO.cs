// MedMania.Core.Data
// EquipmentSO.cs
// Responsibility: Defines equipment inventory assets used by loadouts and availability checks across simulation systems.

using UnityEngine;
using MedMania.Core.Domain.Inventory;

namespace MedMania.Core.Data.ScriptableObjects
{
    [CreateAssetMenu(menuName = "MedMania/Equipment")]
    public class EquipmentSO : ScriptableObject, IEquipmentDef { [SerializeField] private string _name = "Equipment"; public string Name => _name; }
}
