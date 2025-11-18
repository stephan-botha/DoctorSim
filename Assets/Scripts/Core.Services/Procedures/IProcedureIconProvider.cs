// MedMania.Core.Services
// IProcedureIconProvider.cs
// Responsibility: Provide cached icon sprites or view models for procedures.

using UnityEngine;
using MedMania.Core.Domain.Procedures;

namespace MedMania.Core.Services.Procedures
{
    public interface IProcedureIconProvider
    {
        ProcedureIconView GetIcon(IProcedureDef procedure);

        ProcedureIconView GetPrefabIcon(GameObject prefab, Vector2Int size = default, Color? background = null, string labelOverride = null);
    }

    public readonly struct ProcedureIconView
    {
        public static readonly ProcedureIconView Empty = new(null, null);

        public ProcedureIconView(Sprite sprite, string labelOverride)
        {
            Sprite = sprite;
            LabelOverride = labelOverride;
        }

        public Sprite Sprite { get; }

        public string LabelOverride { get; }

        public bool HasSprite => Sprite != null;
    }
}
