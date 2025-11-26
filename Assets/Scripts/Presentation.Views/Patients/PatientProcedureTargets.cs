// MedMania.Presentation.Views
// PatientProcedureTargets.cs
// Responsibility: Map procedure definitions to patient-specific interaction targets and raycast colliders.

using System;
using UnityEngine;
using MedMania.Core.Data.ScriptableObjects;
using MedMania.Core.Domain.Procedures;
using MedMania.Presentation.Input.Staff;

namespace MedMania.Presentation.Views.Patients
{
    public sealed class PatientProcedureTargets : MonoBehaviour, IProcedureTargetProvider
    {
        [SerializeField] private TargetEntry[] _targets = Array.Empty<TargetEntry>();

        public bool TryGetTarget(IProcedureDef procedure, out Transform target)
        {
            target = null;

            if (procedure == null || _targets == null)
            {
                return false;
            }

            for (int i = 0; i < _targets.Length; i++)
            {
                if (_targets[i].Matches(procedure))
                {
                    target = _targets[i].Target;
                    return target != null;
                }
            }

            return false;
        }

        public bool TryGetTarget(Collider sourceCollider, IProcedureDef procedure, out Transform target)
        {
            target = null;

            if (procedure == null || sourceCollider == null || _targets == null)
            {
                return false;
            }

            for (int i = 0; i < _targets.Length; i++)
            {
                if (_targets[i].Collider == sourceCollider && _targets[i].Matches(procedure))
                {
                    target = _targets[i].Target;
                    return target != null;
                }
            }

            return false;
        }

        [Serializable]
        public struct TargetEntry
        {
            [SerializeField] private ProcedureSOBase _procedure;
            [SerializeField] private Transform _target;
            [SerializeField] private Collider _collider;

            public Transform Target => _target != null ? _target : _collider != null ? _collider.transform : null;
            public Collider Collider => _collider;

            public bool Matches(IProcedureDef procedure)
            {
                return _procedure != null && procedure == _procedure;
            }
        }
    }
}
