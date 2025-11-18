// MedMania.Core.Data
// TreatmentSO.cs
// Responsibility: Defines treatment procedure assets referenced by disease setups and procedure execution flows.

using UnityEngine;
using MedMania.Core.Domain.Procedures;

namespace MedMania.Core.Data.ScriptableObjects
{
    [CreateAssetMenu(menuName = "MedMania/Procedure/Treatment")]
    public class TreatmentSO : ProcedureSOBase { public override ProcedureKind Kind => ProcedureKind.Treatment; }
}
