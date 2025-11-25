// MedMania.Presentation.Views
// PatientSequenceController.cs
// Responsibility: Runtime coordinator that spawns patients, listens for completed
// procedures, and respawns new patients after discharge while updating the HUD.

using System;
using System.Collections.Generic;
using UnityEngine;
using MedMania.Core.Data.ScriptableObjects;
using MedMania.Core.Domain.Patients;
using MedMania.Core.Domain.Procedures;
using MedMania.Core.Services;
using MedMania.Core.Services.Timing;
using MedMania.Core.Services.Patients;
using MedMania.Presentation.Views.Carry;
using MedMania.Presentation.Views.Procedures;
using UnityEngine.Events;

namespace MedMania.Presentation.Views.Patients
{
    public sealed class PatientSequenceController : MonoBehaviour
    {
        [Header("Patient Lifecycle")]
        [SerializeField] private PatientView _patientPrefab;
        [SerializeField] private DiseaseSO[] _diseases;
        [SerializeField] private CarrySlot[] _waitingSeats;
        [SerializeField, Min(0f)] private float _respawnDelaySeconds = 4f;
        [SerializeField] private string[] _patientNames =
        {
            "Alex",
            "Bailey",
            "Casey",
            "Devin"
        };

        [Header("Patient Appearance")]
        [SerializeField] private GameObject[] _patientAvatars = Array.Empty<GameObject>();

        [Header("Runtime References")]
        [SerializeField] private GameTimerService _gameTimer;
        [SerializeField] private ProcedureRunner _procedureRunner;
        [SerializeField] private PatientEvent _patientBound = new PatientEvent();
        [SerializeField] private MonoBehaviour _avatarIconServiceBehaviour;

        private int _nameIndex;
        private readonly Dictionary<CarrySlot, PatientView> _seatAssignments = new();
        private readonly Dictionary<PatientView, CarrySlot> _patientSeats = new();
        private readonly Dictionary<PatientView, IDisposable> _scheduledRespawns = new();
        private readonly Dictionary<PatientView, UnityAction<IPatient>> _readyListeners = new();
        private readonly Dictionary<PatientView, GameObject> _avatarAssignments = new();
        private IPatientAvatarIconService _avatarIconService;
        private bool _avatarIconServiceLookupAttempted;
        
        private void Awake()
        {
            EnsureTimer();
        }

        private void OnEnable()
        {
            if (_procedureRunner != null)
            {
                _procedureRunner.onPatientStarted.AddListener(HandlePatientProcedureStarted);
                _procedureRunner.onPatientCompleted.AddListener(HandlePatientProcedureCompleted);
                _procedureRunner.onPatientReset.AddListener(HandlePatientReset);
            }

            PopulateWaitingSeats();
        }

        private void OnDisable()
        {
            if (_procedureRunner != null)
            {
                _procedureRunner.onPatientStarted.RemoveListener(HandlePatientProcedureStarted);
                _procedureRunner.onPatientCompleted.RemoveListener(HandlePatientProcedureCompleted);
                _procedureRunner.onPatientReset.RemoveListener(HandlePatientReset);
            }

            CancelAllScheduledRespawns();
        }

        private void HandlePatientProcedureStarted(PatientView patientView, IProcedureDef _)
        {
            CancelScheduledRespawn(patientView);
        }

        private void HandlePatientProcedureCompleted(PatientView patientView, IProcedureDef _)
        {
            if (patientView == null)
            {
                return;
            }

            var patient = patientView.Domain;
            if (patient == null)
            {
                return;
            }

            if (patient.State == PatientState.ReadyForDischarge)
            {
                BeginDischargeCountdown(patientView);
            }
        }

        private void HandlePatientReset(PatientView patientView)
        {
            CancelScheduledRespawn(patientView);
        }

        private void PopulateWaitingSeats()
        {
            if (_waitingSeats == null || _waitingSeats.Length == 0)
            {
                return;
            }

            CleanupSeatAssignments();

            for (int i = 0; i < _waitingSeats.Length; i++)
            {
                var seat = _waitingSeats[i];
                if (seat == null)
                {
                    continue;
                }

                if (_seatAssignments.TryGetValue(seat, out var existing) && existing != null)
                {
                    continue;
                }

                SpawnPatient(seat);
            }
        }

        private void CleanupSeatAssignments()
        {
            if (_seatAssignments.Count == 0 && _patientSeats.Count == 0)
            {
                return;
            }

            var seatsToRemove = new List<CarrySlot>();
            foreach (var pair in _seatAssignments)
            {
                if (pair.Key == null || pair.Value == null)
                {
                    seatsToRemove.Add(pair.Key);
                }
            }

            for (int i = 0; i < seatsToRemove.Count; i++)
            {
                var seat = seatsToRemove[i];
                _seatAssignments.Remove(seat);
            }

            var patientsToRemove = new List<PatientView>();
            foreach (var pair in _patientSeats)
            {
                if (pair.Key == null || pair.Value == null)
                {
                    patientsToRemove.Add(pair.Key);
                }
            }

            for (int i = 0; i < patientsToRemove.Count; i++)
            {
                var patient = patientsToRemove[i];
                if (patient != null && _readyListeners.TryGetValue(patient, out var handler))
                {
                    patient.PatientReady.RemoveListener(handler);
                }

                _readyListeners.Remove(patient);

                if (_scheduledRespawns.TryGetValue(patient, out var handle))
                {
                    handle?.Dispose();
                    _scheduledRespawns.Remove(patient);
                }

                if (patient != null)
                {
                    UnregisterPatientAvatar(patient);
                }

                _patientSeats.Remove(patient);
            }
        }

        private void SpawnPatient(CarrySlot seat)
        {
            if (_patientPrefab == null)
            {
                Debug.LogWarning("PatientSequenceController requires a PatientView prefab reference.", this);
                return;
            }

            var anchor = seat != null ? seat.Anchor : transform;
            if (seat != null && !seat.IsEmpty)
            {
                seat.Clear();
            }

            var instance = Instantiate(_patientPrefab, anchor.position, anchor.rotation, anchor);
            if (!instance.gameObject.activeSelf)
            {
                instance.gameObject.SetActive(true);
            }

            ApplyRandomAppearance(instance);

            if (seat != null && instance.TryGetComponent(out PatientCarryView carryView))
            {
                if (!seat.TrySwap(carryView, out _))
                {
                    seat.TryPlace(carryView);
                }
            }

            RegisterPatient(instance, seat);

            if (_procedureRunner != null && instance.TryGetComponent(out PatientProcedureProgressDisplay progressDisplay))
            {
                progressDisplay.BindRunner(_procedureRunner);
            }
        }

        private void ApplyRandomAppearance(PatientView patientView)
        {
            if (patientView == null)
            {
                return;
            }

            _avatarAssignments.Remove(patientView);

            var originalAvatar = FindAvatarMesh(patientView);
            if (originalAvatar == null)
            {
                return;
            }

            if (_patientAvatars == null || _patientAvatars.Length == 0)
            {
                _avatarAssignments[patientView] = originalAvatar.gameObject;
                return;
            }

            var avatarPrefab = GetRandomAvatar();
            if (avatarPrefab == null)
            {
                _avatarAssignments[patientView] = originalAvatar.gameObject;
                return;
            }

            if (!avatarPrefab.TryGetComponent<Renderer>(out var prefabRenderer))
            {
                prefabRenderer = avatarPrefab.GetComponentInChildren<Renderer>();
            }

            if (prefabRenderer == null)
            {
                Debug.LogWarning($"Patient avatar prefab '{avatarPrefab.name}' is missing a Renderer component.", avatarPrefab);
                _avatarAssignments[patientView] = originalAvatar.gameObject;
                return;
            }

            var parent = originalAvatar.parent;
            var siblingIndex = originalAvatar.GetSiblingIndex();
            var localPosition = originalAvatar.localPosition;
            var localRotation = originalAvatar.localRotation;
            var localScale = originalAvatar.localScale;

            var avatarInstance = Instantiate(avatarPrefab, parent, false);
            var instanceTransform = avatarInstance.transform;
            instanceTransform.SetSiblingIndex(siblingIndex);
            instanceTransform.localPosition = localPosition;
            instanceTransform.localRotation = localRotation;
            instanceTransform.localScale = localScale;

            _avatarAssignments[patientView] = avatarPrefab;

            Destroy(originalAvatar.gameObject);
        }

        private Transform FindAvatarMesh(PatientView patientView)
        {
            if (patientView == null)
            {
                return null;
            }

            return FindMeshChildRecursive(patientView.AvatarRoot);
        }

        private Transform FindMeshChildRecursive(Transform parent)
        {
            if (parent == null)
            {
                return null;
            }

            if (parent.TryGetComponent<SkinnedMeshRenderer>(out _) || parent.TryGetComponent<Renderer>(out _))
            {
                return parent;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                var found = FindMeshChildRecursive(child);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private GameObject GetRandomAvatar()
        {
            if (_patientAvatars == null || _patientAvatars.Length == 0)
            {
                return null;
            }

            int index = UnityEngine.Random.Range(0, _patientAvatars.Length);
            return _patientAvatars[index];
        }

        private void RegisterPatientAvatar(PatientView view, IPatient patient)
        {
            if (view == null || patient == null)
            {
                return;
            }

            ResolveAvatarIconService();
            if (_avatarIconService == null)
            {
                return;
            }

            if (!_avatarAssignments.TryGetValue(view, out var avatarPrefab) || avatarPrefab == null)
            {
                var fallback = FindAvatarMesh(view);
                avatarPrefab = fallback != null ? fallback.gameObject : null;
                if (avatarPrefab != null)
                {
                    _avatarAssignments[view] = avatarPrefab;
                }
            }

            if (avatarPrefab == null)
            {
                _avatarIconService.ClearAvatar(patient);
                return;
            }

            _avatarIconService.RegisterAvatar(patient, avatarPrefab);
        }

        private void UnregisterPatientAvatar(PatientView patientView)
        {
            if (patientView == null)
            {
                return;
            }

            var patient = patientView.Domain;
            _avatarAssignments.Remove(patientView);

            if (patient == null)
            {
                return;
            }

            ResolveAvatarIconService();
            _avatarIconService?.ClearAvatar(patient);
        }

        private void ResolveAvatarIconService()
        {
            if (_avatarIconService != null)
            {
                return;
            }

            if (_avatarIconServiceBehaviour != null)
            {
                _avatarIconService = _avatarIconServiceBehaviour as IPatientAvatarIconService;
                if (_avatarIconService == null)
                {
                    Debug.LogError($"Assigned avatar icon service override on {nameof(PatientSequenceController)} does not implement {nameof(IPatientAvatarIconService)}.", this);
                }
                else
                {
                    return;
                }
            }

            if (_avatarIconServiceLookupAttempted)
            {
                return;
            }

            _avatarIconServiceLookupAttempted = true;

            if (!ServiceLocator.TryGet(out _avatarIconService))
            {
                Debug.LogWarning($"Unable to resolve {nameof(IPatientAvatarIconService)}.", this);
            }
        }

        private void RegisterPatient(PatientView patientView, CarrySlot seat)
        {
            if (patientView == null)
            {
                return;
            }

            if (seat != null)
            {
                _seatAssignments[seat] = patientView;
            }

            _patientSeats[patientView] = seat;

            var readyHandler = new UnityAction<IPatient>(patient => OnPatientReady(patientView, patient));
            _readyListeners[patientView] = readyHandler;
            patientView.PatientReady.AddListener(readyHandler);

            patientView.Configure(GetRandomDisease(), GetNextName());
        }

        private void OnPatientReady(PatientView view, IPatient patient)
        {
            if (view != null && _readyListeners.TryGetValue(view, out var handler))
            {
                view.PatientReady.RemoveListener(handler);
                _readyListeners.Remove(view);
            }

            RegisterPatientAvatar(view, patient);

            _patientBound?.Invoke(patient);
        }

        private DiseaseSO GetRandomDisease()
        {
            if (_diseases == null || _diseases.Length == 0)
            {
                return null;
            }

            int index = UnityEngine.Random.Range(0, _diseases.Length);
            return _diseases[index];
        }

        private void BeginDischargeCountdown(PatientView patientView)
        {
            if (patientView == null)
            {
                return;
            }

            EnsureTimer();

            if (_gameTimer == null)
            {
                CompleteDischarge(patientView);
                return;
            }

            if (_scheduledRespawns.ContainsKey(patientView))
            {
                return;
            }

            var handle = _gameTimer.Schedule(_respawnDelaySeconds, () => CompleteDischarge(patientView));
            if (handle != null)
            {
                _scheduledRespawns[patientView] = handle;
            }
        }

        private void CompleteDischarge(PatientView patientView)
        {
            if (patientView == null)
            {
                return;
            }

            CancelScheduledRespawn(patientView);

            if (_readyListeners.TryGetValue(patientView, out var handler))
            {
                patientView.PatientReady.RemoveListener(handler);
                _readyListeners.Remove(patientView);
            }

            CarrySlot seat = null;
            if (_patientSeats.TryGetValue(patientView, out seat))
            {
                _patientSeats.Remove(patientView);
            }

            if (seat != null && _seatAssignments.TryGetValue(seat, out var occupant) && occupant == patientView)
            {
                _seatAssignments.Remove(seat);
            }

            var patient = patientView.Domain;
            if (patient != null)
            {
                _patientBound?.Invoke(patient);

                if (patient.State == PatientState.ReadyForDischarge)
                {
                    patient.TryDischarge();
                }
            }

            _patientBound?.Invoke(null);

            UnregisterPatientAvatar(patientView);

            Destroy(patientView.gameObject);

            if (seat != null)
            {
                seat.Clear();
                SpawnPatient(seat);
            }
        }

        private void CancelScheduledRespawn(PatientView patientView)
        {
            if (patientView == null)
            {
                return;
            }

            if (_scheduledRespawns.TryGetValue(patientView, out var handle))
            {
                handle?.Dispose();
                _scheduledRespawns.Remove(patientView);
            }
        }

        private void CancelAllScheduledRespawns()
        {
            foreach (var handle in _scheduledRespawns.Values)
            {
                handle?.Dispose();
            }

            _scheduledRespawns.Clear();
        }

        private void EnsureTimer()
        {
            ServiceLocator.TryGet(out _gameTimer);
        }

        private string GetNextName()
        {
            if (_patientNames == null || _patientNames.Length == 0)
            {
                return "Patient";
            }

            var name = _patientNames[_nameIndex % _patientNames.Length];
            _nameIndex = (_nameIndex + 1) % _patientNames.Length;
            return name;
        }
    }
}
