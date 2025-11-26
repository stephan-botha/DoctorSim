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
        private Patients.PatientView _cachedPatient;
        private EquipmentView _cachedEquipmentView;
        private IEquipmentDef _cachedEquipmentDef;
        private IProcedureDef _cachedProcedure;
        private Transform _cachedAnchor;

        private static readonly int s_InteractionLayer = LayerMask.NameToLayer("Interaction");

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

        public bool TryResolve(IProcedureDef procedure, Vector3 rayOrigin, Vector3 rayDirection, out Patients.PatientView patient, out EquipmentView equipmentView, out IEquipmentDef equipmentDef, out Transform interactionAnchor)
        {
            ResetCachedTarget();

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
                if (TryResolveEquipmentTarget(requiredEquipment, out patient, out equipmentView, out equipmentDef, out interactionAnchor))
                {
                    CacheResolvedTarget(patient, procedure, equipmentView, equipmentDef, interactionAnchor);
                    return true;
                }

                return false;
            }

            if (!TryResolvePatientTarget(procedure, rayOrigin, rayDirection, out patient, out interactionAnchor))
            {
                return false;
            }

            CacheResolvedTarget(patient, procedure, null, null, interactionAnchor);
            return true;
        }

        public bool IsCachedTargetStillValid(IProcedureDef procedure, out Patients.PatientView patient, out EquipmentView equipmentView, out IEquipmentDef equipmentDef, out Transform interactionAnchor)
        {
            patient = _cachedPatient;
            equipmentView = _cachedEquipmentView;
            equipmentDef = _cachedEquipmentDef;
            interactionAnchor = null;

            if (procedure == null || _cachedProcedure != procedure)
            {
                return false;
            }

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

            interactionAnchor = _cachedAnchor != null ? _cachedAnchor : ResolvePatientAnchor(patient, procedure);
            return IsWithinRange(interactionAnchor);
        }

        public void ResetCachedTarget()
        {
            _cachedPatient = null;
            _cachedEquipmentView = null;
            _cachedEquipmentDef = null;
            _cachedProcedure = null;
            _cachedAnchor = null;
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

        private bool TryResolvePatientTarget(IProcedureDef procedure, Vector3 rayOrigin, Vector3 rayDirection, out Patients.PatientView patient, out Transform interactionAnchor)
        {
            patient = null;
            interactionAnchor = null;

            var hasRay = rayDirection.sqrMagnitude > 0.0001f;
            if (hasRay)
            {
                var direction = rayDirection.normalized;
                var ray = new Ray(rayOrigin, direction);
                var layerMask = s_InteractionLayer >= 0 ? 1 << s_InteractionLayer : Physics.DefaultRaycastLayers;

                if (Physics.Raycast(ray, out var hit, _range, layerMask, QueryTriggerInteraction.Ignore))
                {
                    patient = hit.collider.GetComponentInParent<Patients.PatientView>();
                    if (patient != null)
                    {
                        interactionAnchor = ResolveHitAnchor(hit.transform, patient, procedure);
                        if (interactionAnchor != null && IsWithinRange(interactionAnchor))
                        {
                            return true;
                        }
                    }
                }
            }

            patient = FindClosestPatient();
            interactionAnchor = patient != null ? ResolvePatientAnchor(patient, procedure) : null;
            return patient != null && interactionAnchor != null;
        }

        private Transform ResolveHitAnchor(Transform hitTransform, Patients.PatientView patient, IProcedureDef requestedProcedure)
        {
            if (Patients.PatientProcedureTargets.TryGetTargets(patient, out var targets))
            {
                var anchors = targets.Anchors;
                for (int i = 0; i < anchors.Count; i++)
                {
                    var entry = anchors[i];
                    var anchor = entry.Anchor;
                    if (anchor == null)
                    {
                        continue;
                    }

                    if (hitTransform == anchor || hitTransform.IsChildOf(anchor))
                    {
                        var mappedProcedure = entry.Procedure;
                        if (mappedProcedure != null && mappedProcedure != requestedProcedure)
                        {
                            return null;
                        }

                        _cachedAnchor = anchor;
                        return anchor;
                    }
                }
            }

            var anchorFallback = ResolvePatientAnchor(patient, requestedProcedure);
            _cachedAnchor = anchorFallback;
            return anchorFallback;
        }

        private void CacheResolvedTarget(Patients.PatientView patient, IProcedureDef procedure, EquipmentView equipmentView, IEquipmentDef equipmentDef, Transform interactionAnchor)
        {
            _cachedPatient = patient;
            _cachedProcedure = procedure;
            _cachedEquipmentView = equipmentView;
            _cachedEquipmentDef = equipmentDef;
            _cachedAnchor = interactionAnchor;
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
