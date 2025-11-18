// MedMania.Presentation.Views
// PatientAvatarIconService.cs
// Responsibility: Cache patient avatar sprites rendered via the ProcedureIconProvider pipeline.

using System;
using System.Collections.Generic;
using UnityEngine;
using MedMania.Core.Domain.Patients;
using MedMania.Core.Services;
using MedMania.Core.Services.Patients;
using MedMania.Core.Services.Procedures;

namespace MedMania.Presentation.Views.Patients
{
    [DefaultExecutionOrder(-450)]
    public sealed class PatientAvatarIconService : MonoBehaviour, IPatientAvatarIconService
    {
        [SerializeField] private MonoBehaviour _iconProviderBehaviour;

        private readonly Dictionary<Guid, ProcedureIconView> _iconCache = new();
        private readonly Dictionary<Guid, GameObject> _avatarSources = new();

        private IProcedureIconProvider _iconProvider;
        private bool _iconProviderLookupAttempted;

        private void Awake()
        {
            ResolveIconProvider();
        }

        public void RegisterAvatar(IPatient patient, GameObject avatarPrefab)
        {
            if (patient == null)
            {
                return;
            }

            var id = patient.Id;

            if (avatarPrefab == null)
            {
                _avatarSources.Remove(id);
                _iconCache.Remove(id);
                return;
            }

            _avatarSources[id] = avatarPrefab;

            var provider = ResolveIconProvider();
            if (provider == null)
            {
                _iconCache.Remove(id);
                return;
            }

            var icon = provider.GetPrefabIcon(avatarPrefab);
            if (icon.HasSprite)
            {
                _iconCache[id] = icon;
            }
            else
            {
                _iconCache.Remove(id);
            }
        }

        public void ClearAvatar(IPatient patient)
        {
            if (patient == null)
            {
                return;
            }

            var id = patient.Id;
            _avatarSources.Remove(id);
            _iconCache.Remove(id);
        }

        public ProcedureIconView GetAvatarIcon(IPatient patient)
        {
            if (patient == null)
            {
                return ProcedureIconView.Empty;
            }

            var id = patient.Id;

            if (_iconCache.TryGetValue(id, out var cached))
            {
                return cached;
            }

            if (_avatarSources.TryGetValue(id, out var avatarPrefab) && avatarPrefab != null)
            {
                var provider = ResolveIconProvider();
                if (provider != null)
                {
                    var icon = provider.GetPrefabIcon(avatarPrefab);
                    if (icon.HasSprite)
                    {
                        _iconCache[id] = icon;
                        return icon;
                    }
                }
            }

            return ProcedureIconView.Empty;
        }

        private IProcedureIconProvider ResolveIconProvider()
        {
            if (_iconProvider != null)
            {
                return _iconProvider;
            }

            if (_iconProviderBehaviour != null)
            {
                _iconProvider = _iconProviderBehaviour as IProcedureIconProvider;
                if (_iconProvider == null)
                {
                    Debug.LogError($"Assigned icon provider override on {nameof(PatientAvatarIconService)} does not implement {nameof(IProcedureIconProvider)}.", this);
                }
                else
                {
                    return _iconProvider;
                }
            }

            if (_iconProviderLookupAttempted)
            {
                return _iconProvider;
            }

            _iconProviderLookupAttempted = true;

            if (!ServiceLocator.TryGet(out _iconProvider))
            {
                Debug.LogWarning($"Unable to resolve {nameof(IProcedureIconProvider)}.", this);
            }

            return _iconProvider;
        }
    }
}
