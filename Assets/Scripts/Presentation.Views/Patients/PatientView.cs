// MedMania.Presentation.Views
// PatientView.cs
// Responsibility: Bridge scene object to domain IPatient. Holds reference to DiseaseSO.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using MedMania.Core.Domain.Patients;
using MedMania.Core.Data.ScriptableObjects;
using MedMania.Core.Domain.Diseases;

namespace MedMania.Presentation.Views.Patients
{
    [Serializable]
    public sealed class PatientEvent : UnityEvent<IPatient> { }

    public sealed class PatientView : MonoBehaviour
    {
        private static readonly List<PatientView> s_Active = new List<PatientView>();

        [SerializeField] private string _displayName = "Patient";
        [SerializeField] private DiseaseSO _disease;
        [SerializeField] private Transform _avatarRoot;
        [SerializeField] private PatientEvent _patientReady = new PatientEvent();

        public IPatient Domain { get; private set; }
        public PatientEvent PatientReady => _patientReady;
        public Transform AvatarRoot => _avatarRoot != null ? _avatarRoot : transform;
        public static IReadOnlyList<PatientView> Active => s_Active;

        private void Awake()
        {
            RebuildDomain();
        }

        private void OnEnable()
        {
            if (!s_Active.Contains(this))
            {
                s_Active.Add(this);
            }
        }

        private void OnDisable()
        {
            s_Active.Remove(this);
        }

        private void OnDestroy()
        {
            s_Active.Remove(this);
        }

        /// <summary>
        /// Reassign the backing data for the patient view and immediately rebuild
        /// the domain-side patient instance. Useful when reusing view prefabs.
        /// </summary>
        public void Configure(DiseaseSO disease, string displayName = null)
        {
            if (!string.IsNullOrEmpty(displayName))
            {
                _displayName = displayName;
            }

            if (disease != null)
            {
                _disease = disease;
            }

            RebuildDomain();
        }

        private void RebuildDomain()
        {
            if (_disease == null)
            {
                Debug.LogWarning($"PatientView on {name} is missing a DiseaseSO assignment.", this);
                Domain = null;
                return;
            }

            IDiseaseDef disease = _disease;
            Domain = new Patient(_displayName, disease);
            _patientReady?.Invoke(Domain);
        }
    }
}
