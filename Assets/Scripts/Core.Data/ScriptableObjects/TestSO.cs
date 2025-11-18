// MedMania.Core.Data
// TestSO.cs
// Responsibility: Defines diagnostic procedure assets consumed by disease configurations and runtime procedure orchestration.

using UnityEngine;
using MedMania.Core.Domain.Procedures;

namespace MedMania.Core.Data.ScriptableObjects
{
    [CreateAssetMenu(menuName = "MedMania/Procedure/Test")]
    public class TestSO : ProcedureSOBase { public override ProcedureKind Kind => ProcedureKind.Test; }
}
