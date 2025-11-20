// MedMania.Core.Data
// TransporterSO.cs
// Responsibility: Defines transporter assets that adjust movement and expose display data.

using UnityEngine;
using MedMania.Core.Domain.Inventory;

namespace MedMania.Core.Data.ScriptableObjects
{
    [CreateAssetMenu(menuName = "MedMania/Transporter")]
    public class TransporterSO : ScriptableObject, ITransporterDef
    {
        [SerializeField] private string _name = "Transporter";
        [SerializeField, Tooltip("Multiplier applied to staff movement speed while holding this transporter.")]
        private float _speedModifier = 1f;

        public string Name => _name;
        public float SpeedModifier => _speedModifier;
    }
}
