// MedMania.Core.Data
// MotionPreset.cs
// Responsibility: Designer-authored tuning for hand motion verbs.

using DG.Tweening;
using UnityEngine;

namespace MedMania.Core.Data.ScriptableObjects
{
    [CreateAssetMenu(menuName = "MedMania/Hand/MotionPreset")]
    public sealed class MotionPreset : ScriptableObject
    {
        [Header("Timing")]
        [SerializeField, Range(0.01f, 10f)] private float _duration = 0.25f;
        [SerializeField] private Ease _ease = Ease.OutCubic;

        [Header("Punch/Shake")]
        [SerializeField] private float _punchStrength = 10f;
        [SerializeField, Range(1, 20)] private int _vibrato = 10;
        [SerializeField, Range(0f, 1f)] private float _elasticity = 0.5f;

        [Header("Offsets")]
        [SerializeField] private float _defaultReachOffsetCm = 0f;
        [SerializeField, Range(0f, 0.25f)] private float _hoverRadius = 0.01f;
        [SerializeField, Range(0.2f, 5f)] private float _hoverPeriod = 1.6f;
        [SerializeField, Range(-30f, 30f)] private float _hoverTiltDegrees = 3f;

        public float Duration => _duration;
        public Ease Ease => _ease;
        public float PunchStrength => _punchStrength;
        public int Vibrato => _vibrato;
        public float Elasticity => _elasticity;
        public float DefaultReachOffsetCm => _defaultReachOffsetCm;
        public float HoverRadius => _hoverRadius;
        public float HoverPeriod => _hoverPeriod;
        public float HoverTiltDegrees => _hoverTiltDegrees;
    }
}
