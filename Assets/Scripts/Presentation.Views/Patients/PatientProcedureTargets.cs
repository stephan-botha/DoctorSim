// MedMania.Presentation.Views
// PatientProcedureTargets.cs
// Responsibility: Map procedures to patient rig anchors for interaction targeting.

using System;
using System.Collections.Generic;
using UnityEngine;
using MedMania.Core.Data.ScriptableObjects;
using MedMania.Core.Domain.Procedures;

namespace MedMania.Presentation.Views.Patients
{
    [DisallowMultipleComponent]
    public sealed class PatientProcedureTargets : MonoBehaviour
    {
        [Serializable]
        public struct ProcedureAnchor
        {
            [SerializeField] private Transform _anchor;
            [SerializeField] private ProcedureSOBase _procedure;

            public Transform Anchor => _anchor;
            public IProcedureDef Procedure => _procedure;
        }

        private static readonly Dictionary<PatientView, PatientProcedureTargets> s_Registry = new();
        private static readonly int s_InteractionLayer = LayerMask.NameToLayer("Interaction");

        [SerializeField] private PatientView _patient;
        [SerializeField] private ProcedureAnchor[] _anchors = Array.Empty<ProcedureAnchor>();

        public PatientView Patient => _patient != null ? _patient : (_patient = GetComponentInParent<PatientView>());
        public IReadOnlyList<ProcedureAnchor> Anchors => _anchors;

        public static bool TryGetTargets(PatientView patient, out PatientProcedureTargets targets)
        {
            if (patient == null)
            {
                targets = null;
                return false;
            }

            return s_Registry.TryGetValue(patient, out targets);
        }

        public Transform ResolveAnchor(IProcedureDef procedure)
        {
            if (procedure != null)
            {
                for (int i = 0; i < _anchors.Length; i++)
                {
                    var entry = _anchors[i];
                    if (entry.Procedure == procedure && entry.Anchor != null)
                    {
                        return entry.Anchor;
                    }
                }
            }

            return Patient != null ? Patient.transform : transform;
        }

        private void Awake()
        {
            if (_patient == null)
            {
                _patient = GetComponentInParent<PatientView>();
            }
        }

        private void OnEnable()
        {
            Register();
        }

        private void OnDisable()
        {
            Unregister();
        }

        private void OnDestroy()
        {
            Unregister();
        }

        private void Register()
        {
            if (Patient == null || !Patient.isActiveAndEnabled)
            {
                return;
            }

            s_Registry[Patient] = this;
        }

        private void Unregister()
        {
            if (Patient == null)
            {
                return;
            }

            if (s_Registry.TryGetValue(Patient, out var existing) && ReferenceEquals(existing, this))
            {
                s_Registry.Remove(Patient);
            }
        }

        private void OnValidate()
        {
            if (_patient == null)
            {
                _patient = GetComponentInParent<PatientView>();
            }

            for (int i = 0; i < _anchors.Length; i++)
            {
                var entry = _anchors[i];
                var anchor = entry.Anchor;
                if (anchor == null)
                {
                    continue;
                }

                if (Patient != null && anchor != Patient.transform && !anchor.IsChildOf(Patient.transform))
                {
                    Debug.LogWarning($"Anchor {anchor.name} on {name} must be under the patient rig hierarchy.", anchor);
                }

                var collider = anchor.GetComponent<Collider>();
                if (collider == null)
                {
                    Debug.LogWarning($"Anchor {anchor.name} on {name} requires a {nameof(Collider)} for interaction raycasts.", anchor);
                }
                else if (s_InteractionLayer >= 0 && collider.gameObject.layer != s_InteractionLayer)
                {
                    Debug.LogWarning($"Anchor {anchor.name} on {name} should be on the Interaction layer for raycasts.", anchor);
                }
            }
        }
    }
}
