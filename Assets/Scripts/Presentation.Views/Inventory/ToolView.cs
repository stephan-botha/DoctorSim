// MedMania.Presentation.Views
// ToolView.cs
// Responsibility: Scene representation for tools, exposing carry + procedure metadata.

using UnityEngine;
using MedMania.Core.Data.ScriptableObjects;
using MedMania.Core.Domain.Inventory;
using MedMania.Core.Domain.Procedures;
using MedMania.Presentation.Input.Staff;

namespace MedMania.Presentation.Views.Inventory
{
    public sealed class ToolView : MonoBehaviour, ICarryable, IProcedureProvider
    {
        [SerializeField] private ToolSO _tool;
        [SerializeField] private ProcedureSOBase _procedure;

        public ToolSO ToolAsset => _tool;
        public IToolDef Tool => _tool;
        public ProcedureSOBase ProcedureAsset => _procedure;
        public IProcedureDef Procedure => _procedure;

        IProcedureDef IProcedureProvider.Procedure => Procedure;
        public string DisplayName => _tool ? _tool.Name : _procedure ? _procedure.Name : name;
    }
}
