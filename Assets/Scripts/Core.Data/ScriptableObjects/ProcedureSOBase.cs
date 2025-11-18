// MedMania.Core.Data
// ProcedureSOBase.cs

using UnityEngine;
using MedMania.Core.Domain.Procedures;
using MedMania.Core.Domain.Inventory;

namespace MedMania.Core.Data.ScriptableObjects
{
    public abstract class ProcedureSOBase : ScriptableObject, IProcedureDef
    {
        [SerializeField] private string _name = "Unnamed";
        [SerializeField] private float _durationSeconds = 5f;
        [Header("Requires (choose one)")]
        [SerializeField] private ToolSO _requiredTool;
        [SerializeField] private EquipmentSO _requiredEquipment;

        [Header("Icon")]
        [SerializeField] private Sprite _iconSprite;
        [SerializeField] private GameObject _iconPrefab;
        [SerializeField] private Vector2Int _iconRenderTextureSize = new(256, 256);
        [SerializeField] private Color _iconRenderBackground = Color.clear;

        public string Name => _name;
        public abstract ProcedureKind Kind { get; }
        public float DurationSeconds => _durationSeconds;
        public IToolDef RequiredTool => _requiredTool;
        public IEquipmentDef RequiredEquipment => _requiredEquipment;
        public Sprite IconSprite => _iconSprite;
        public GameObject IconPrefab => _iconPrefab;
        public Vector2Int IconRenderTextureSize => _iconRenderTextureSize;
        public Color IconRenderBackground => _iconRenderBackground;
    }
}
