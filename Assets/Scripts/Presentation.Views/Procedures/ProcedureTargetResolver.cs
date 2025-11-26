// MedMania.Presentation.Views
// ProcedureTargetResolver.cs

using UnityEngine;
using MedMania.Core.Domain.Procedures;
using MedMania.Core.Domain.Inventory;
using MedMania.Presentation.Views.Inventory;

namespace MedMania.Presentation.Views.Procedures
{
    public sealed class ProcedureTargetResolver
    {
        private readonly Transform _origin;
        private float _range;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static bool s_GcAllocationLogged;
#endif

        public ProcedureTargetResolver(Transform origin, float range)
        {
            _origin = origin;
            _range = range;
        }

        public void UpdateRange(float range)
        {
            _range = range;
        }

        public bool TryResolve(IProcedureDef procedure, out Patients.PatientView patient, out EquipmentView equipmentView, out IEquipmentDef equipmentDef, out Transform interactionAnchor)
        {
            patient = null;
            equipmentView = null;
            equipmentDef = null;
            interactionAnchor = null;

            if (procedure == null)
            {
                return false;
            }

            var requiredEquipment = procedure.RequiredEquipment;
            if (requiredEquipment != null)
            {
                return TryResolveEquipmentTarget(requiredEquipment, out patient, out equipmentView, out equipmentDef, out interactionAnchor);
            }

            patient = FindClosestPatient();
            interactionAnchor = patient != null ? ResolvePatientAnchor(patient, procedure) : null;
            return patient != null;
        }

        public bool IsTargetStillValid(Patients.PatientView patient, EquipmentView equipmentView, IEquipmentDef equipmentDef, IProcedureDef procedure, out Transform interactionAnchor)
        {
            interactionAnchor = null;

            if (equipmentView != null)
            {
                if (equipmentView.Equipment != equipmentDef)
                {
                    return false;
                }

                if (!equipmentView.TryGetPatient(out var occupant) || occupant != patient)
                {
                    return false;
                }

                interactionAnchor = ResolveAnchor(equipmentView);
                return IsWithinRange(interactionAnchor);
            }

            if (patient == null)
            {
                return false;
            }

            interactionAnchor = ResolvePatientAnchor(patient, procedure);
            return IsWithinRange(interactionAnchor);
        }

        private bool TryResolveEquipmentTarget(IEquipmentDef requiredEquipment, out Patients.PatientView patient, out EquipmentView equipmentView, out IEquipmentDef equipmentDef, out Transform interactionAnchor)
        {
            EquipmentView bestView = null;
            IEquipmentDef bestDef = null;
            Patients.PatientView bestPatient = null;
            float bestDistance = float.MaxValue;

            var equipmentViews = EquipmentView.Active;
            for (int i = 0; i < equipmentViews.Count; i++)
            {
                var view = equipmentViews[i];
                if (view == null)
                {
                    continue;
                }

                var equipment = view.Equipment;
                if (equipment == null || equipment != requiredEquipment)
                {
                    continue;
                }

                if (!view.TryGetPatient(out var occupant) || occupant == null)
                {
                    continue;
                }

                var anchor = ResolveAnchor(view);
                float distance = Vector3.Distance(_origin.position, anchor.position);
                if (distance > _range)
                {
                    continue;
                }

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestPatient = occupant;
                    bestView = view;
                    bestDef = equipment;
                }
            }

            if (bestPatient == null)
            {
                patient = null;
                equipmentView = null;
                equipmentDef = null;
                interactionAnchor = null;
                return false;
            }

            patient = bestPatient;
            equipmentView = bestView;
            equipmentDef = bestDef;
            interactionAnchor = ResolveAnchor(bestView);
            return true;
        }

        private Patients.PatientView FindClosestPatient()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            long allocationBefore = System.GC.GetAllocatedBytesForCurrentThread();
#endif

            var patients = Patients.PatientView.Active;
            Patients.PatientView best = null; float bestDist = float.MaxValue;
            for (int i = 0; i < patients.Count; i++)
            {
                var p = patients[i];
                if (p == null)
                {
                    continue;
                }

                float d = Vector3.Distance(_origin.position, p.transform.position);
                if (d < bestDist && d <= _range)
                {
                    best = p;
                    bestDist = d;
                }
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!s_GcAllocationLogged)
            {
                s_GcAllocationLogged = true;
                long allocationAfter = System.GC.GetAllocatedBytesForCurrentThread();
                long delta = allocationAfter - allocationBefore;

                if (delta != 0)
                {
                    Debug.LogWarning($"{nameof(ProcedureTargetResolver)}.{nameof(FindClosestPatient)} allocated {delta} bytes on first measurement. Cached registry should allow zero-allocation lookups.");
                }
                else
                {
                    Debug.Log($"{nameof(ProcedureTargetResolver)}.{nameof(FindClosestPatient)} allocation delta: {delta} bytes after switching to the cached registry.");
                }
            }
#endif

            return best;
        }

        private Transform ResolvePatientAnchor(Patients.PatientView patient, IProcedureDef procedure)
        {
            if (Patients.PatientProcedureTargets.TryGetTargets(patient, out var targets))
            {
                return targets.ResolveAnchor(procedure);
            }

            return patient != null ? patient.transform : null;
        }

        private Transform ResolveAnchor(EquipmentView view)
        {
            return view != null && view.InteractionAnchor != null
                ? view.InteractionAnchor
                : view != null ? view.transform : null;
        }

        private bool IsWithinRange(Transform anchor)
        {
            if (anchor == null)
            {
                return false;
            }

            return Vector3.Distance(_origin.position, anchor.position) <= _range;
        }
    }
}
