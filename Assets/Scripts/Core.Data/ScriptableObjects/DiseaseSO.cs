// MedMania.Core.Data
// DiseaseSO.cs
// Responsibility: Data definition for diseases, mapped to domain interfaces.

using UnityEngine;
using MedMania.Core.Domain.Diseases;
using MedMania.Core.Domain.Procedures;

namespace MedMania.Core.Data.ScriptableObjects
{
    [CreateAssetMenu(menuName = "MedMania/Disease")]
    public class DiseaseSO : ScriptableObject, IDiseaseDef
    {
        [SerializeField] private string _name = "STEMI";
        [SerializeField] private TestSO[] _tests = default;
        [SerializeField] private TreatmentSO[] _treatments = default;
        [SerializeField] private float _maxWaitSeconds = 120f;

        public string Name => _name;
        public IProcedureDef[] Tests => _tests;
        public IProcedureDef[] Treatments => _treatments;
        public float MaxWaitSeconds => _maxWaitSeconds;
    }
}
