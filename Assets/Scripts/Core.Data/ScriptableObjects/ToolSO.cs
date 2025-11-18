// MedMania.Core.Data
// ToolSO.cs
// Responsibility: Defines tool inventory assets consumed by inventory management and procedure requirement checks.

using UnityEngine;
using MedMania.Core.Domain.Inventory;

namespace MedMania.Core.Data.ScriptableObjects
{
    [CreateAssetMenu(menuName = "MedMania/Tool")]
    public class ToolSO : ScriptableObject, IToolDef { [SerializeField] private string _name = "Tool"; public string Name => _name; }
}
