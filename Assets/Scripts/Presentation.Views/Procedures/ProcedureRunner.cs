// MedMania.Presentation.Views
// ProcedureRunner.cs
// Responsibility: Finds patient in range, constructs IProcedureContext from GameTimerService, runs ProcedureRun.

using UnityEngine;
using UnityEngine.Events;
using MedMania.Core.Domain.Procedures;
using MedMania.Core.Services.Timing;
using MedMania.Core.Services;
using MedMania.Core.Domain.Inventory;
using MedMania.Core.Domain.Patients;
using MedMania.Presentation.Views.Inventory;

namespace MedMania.Presentation.Views.Procedures
{
    public sealed class ProcedureRunner : MonoBehaviour, IProcedureContext
    {
        [SerializeField] private float _range = 1.5f;
        [SerializeField] private MonoBehaviour _performerSource;
        [SerializeField] private UnityEvent<IProcedureDef> _onStarted = new();
        [SerializeField] private UnityEvent<float> _onProgress = new();
        [SerializeField] private UnityEvent<IProcedureDef> _onCompleted = new();
        [SerializeField] private UnityEvent<Patients.PatientView, IProcedureDef> _onPatientStarted = new();
        [SerializeField] private UnityEvent<Patients.PatientView, float> _onPatientProgress = new();
        [SerializeField] private UnityEvent<Patients.PatientView, IProcedureDef> _onPatientCompleted = new();
        [SerializeField] private UnityEvent<IPatient, IProcedureDef> _onDomainPatientCompleted = new();
        [SerializeField] private UnityEvent<Patients.PatientView> _onPatientReset = new();
        [SerializeField] private UnityEvent<Transform> _onInteractionAnchorResolved = new();

        private IGameTimer _timer;
        private System.IDisposable _activeRun;
        private Patients.PatientView _activePatient;
        private IProcedureDef _activeProcedure;
        private float _activeDuration;
        private float _activeStartTime;
        private bool _isRunning;
        private IProcedurePerformer _performer;
        private EquipmentView _activeEquipmentView;
        private IEquipmentDef _activeEquipmentDef;
        private Transform _activeInteractionAnchor;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static bool s_GcAllocationLogged;
#endif

        public UnityEvent<IProcedureDef> onStarted => _onStarted;
        public UnityEvent<float> onProgress => _onProgress;
        public UnityEvent<IProcedureDef> onCompleted => _onCompleted;
        public UnityEvent<Patients.PatientView, IProcedureDef> onPatientStarted => _onPatientStarted;
        public UnityEvent<Patients.PatientView, float> onPatientProgress => _onPatientProgress;
        public UnityEvent<Patients.PatientView, IProcedureDef> onPatientCompleted => _onPatientCompleted;
        public UnityEvent<IPatient, IProcedureDef> onDomainPatientCompleted => _onDomainPatientCompleted;
        public UnityEvent<Patients.PatientView> onPatientReset => _onPatientReset;
        public UnityEvent<Transform> onInteractionAnchorResolved => _onInteractionAnchorResolved;
        public Transform ActiveInteractionAnchor => _activeInteractionAnchor;

        private void Awake()
        {
            if (_performerSource == null)
            {
                _performer = GetComponentInParent<IProcedurePerformer>();
                _performerSource = _performer as MonoBehaviour;
            }
            else
            {
                _performer = _performerSource as IProcedurePerformer;
            }

            if (_performerSource != null && _performer == null)
            {
                Debug.LogError($"Assigned performer on {nameof(ProcedureRunner)} does not implement {nameof(IProcedurePerformer)}.", this);
            }
        }

        private void OnEnable()
        {
            _timer = ServiceLocator.Find<GameTimerService>();

            if (_performer == null && _performerSource == null)
            {
                _performer = GetComponentInParent<IProcedurePerformer>();
                _performerSource = _performer as MonoBehaviour;
            }

            if (_performer != null)
            {
                _performer.PerformRequested += TryRunNearby;
            }
        }

        private void OnDisable()
        {
            if (_performer != null)
            {
                _performer.PerformRequested -= TryRunNearby;
            }

            CancelActiveRun();
        }

        public System.IDisposable Schedule(System.TimeSpan duration, System.Action onCompleted, System.Threading.CancellationToken _ = default)
        {
            if (_timer == null)
            {
                _timer = ServiceLocator.Find<GameTimerService>();
            }

            return _timer.Schedule((float)duration.TotalSeconds, onCompleted);
        }

        public bool IsEquipmentAvailable(IEquipmentDef equipment)
        {
            return equipment == _activeEquipmentDef;
        }

        public void TryRunNearby(IProcedureDef procedure)
        {
            if (procedure == null)
            {
                CancelActiveRun();
                return;
            }

            if (_activeRun != null)
            {
                return;
            }

            if (!TryResolvePatient(procedure, out var patient))
            {
                return;
            }

            _activePatient = patient;
            _activeProcedure = procedure;
            _activeDuration = Mathf.Max(0f, procedure.DurationSeconds);
            _activeStartTime = Time.time;

            var run = ProcedureRun.TryRun(patient.Domain, procedure, this,
                onBegan: () => HandleRunStarted(procedure),
                onCompleted: () => HandleRunCompleted(procedure));

            if (run == null)
            {
                ResetActiveRunState();
                return;
            }

            _activeRun = run;
        }

        private void Update()
        {
            if (_activeRun == null) return;

            if (_performer == null || _performer.HeldProcedure != _activeProcedure)
            {
                CancelActiveRun();
                return;
            }

            if (_activeEquipmentView != null)
            {
                if (_activeEquipmentView.Equipment != _activeEquipmentDef)
                {
                    CancelActiveRun();
                    return;
                }

                if (!_activeEquipmentView.TryGetPatient(out var occupant) || occupant != _activePatient)
                {
                    CancelActiveRun();
                    return;
                }

                var anchor = _activeEquipmentView.InteractionAnchor;
                if (Vector3.Distance(transform.position, anchor.position) > _range)
                {
                    CancelActiveRun();
                    return;
                }
            }
            else
            {
                if (_activePatient == null)
                {
                    CancelActiveRun();
                    return;
                }

                if (Vector3.Distance(transform.position, _activePatient.transform.position) > _range)
                {
                    CancelActiveRun();
                    return;
                }
            }

            if (_isRunning && _activeDuration > 0f)
            {
                var elapsed = Mathf.Max(0f, Time.time - _activeStartTime);
                var progress = Mathf.Clamp01(elapsed / _activeDuration);
                _onProgress?.Invoke(progress);
                if (_activePatient != null)
                {
                    _onPatientProgress?.Invoke(_activePatient, progress);
                }
            }
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

                float d = Vector3.Distance(transform.position, p.transform.position);
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
                    Debug.LogWarning($"{nameof(ProcedureRunner)}.{nameof(FindClosestPatient)} allocated {delta} bytes on first measurement. Cached registry should allow zero-allocation lookups.", this);
                }
                else
                {
                    Debug.Log($"{nameof(ProcedureRunner)}.{nameof(FindClosestPatient)} allocation delta: {delta} bytes after switching to the cached registry.", this);
                }
            }
#endif

            return best;
        }

        private void HandleRunStarted(IProcedureDef procedure)
        {
            _isRunning = true;
            var initialProgress = _activeDuration <= 0f ? 1f : 0f;
            var patient = _activePatient;

            _onProgress?.Invoke(initialProgress);
            if (patient != null)
            {
                _onPatientProgress?.Invoke(patient, initialProgress);
                _onPatientStarted?.Invoke(patient, procedure);
            }

            _onInteractionAnchorResolved?.Invoke(_activeInteractionAnchor);
            _onStarted?.Invoke(procedure);
        }

        private void HandleRunCompleted(IProcedureDef procedure)
        {
            _isRunning = false;
            var patient = _activePatient;

            _onProgress?.Invoke(1f);
            if (patient != null)
            {
                _onPatientProgress?.Invoke(patient, 1f);
                _onPatientCompleted?.Invoke(patient, procedure);

                var domainPatient = patient.Domain;
                if (domainPatient != null)
                {
                    _onDomainPatientCompleted?.Invoke(domainPatient, procedure);
                }
            }

            _onCompleted?.Invoke(procedure);
            _onInteractionAnchorResolved?.Invoke(null);
            ResetActiveRunState();
        }

        private void CancelActiveRun()
        {
            var run = _activeRun;
            var patient = _activePatient;

            if (run != null)
            {
                run.Dispose();
                if (patient != null)
                {
                    _onPatientReset?.Invoke(patient);
                }
            }

            _onInteractionAnchorResolved?.Invoke(null);
            ResetActiveRunState();
        }

        private void ResetActiveRunState()
        {
            _activeRun = null;
            _activePatient = null;
            _activeProcedure = null;
            _activeDuration = 0f;
            _activeStartTime = 0f;
            _isRunning = false;
            _activeEquipmentView = null;
            _activeEquipmentDef = null;
            _activeInteractionAnchor = null;
        }

        private bool TryResolvePatient(IProcedureDef procedure, out Patients.PatientView patient)
        {
            patient = null;

            if (procedure == null)
            {
                return false;
            }

            var requiredEquipment = procedure.RequiredEquipment;
            if (requiredEquipment != null)
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

                    var equipmentDef = view.Equipment;
                    if (equipmentDef == null || equipmentDef != requiredEquipment)
                    {
                        continue;
                    }

                    if (!view.TryGetPatient(out var occupant) || occupant == null)
                    {
                        continue;
                    }

                    var anchor = view.InteractionAnchor != null ? view.InteractionAnchor : view.transform;
                    float distance = Vector3.Distance(transform.position, anchor.position);
                    if (distance > _range)
                    {
                        continue;
                    }

                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestPatient = occupant;
                        bestView = view;
                        bestDef = equipmentDef;
                    }
                }

                if (bestPatient == null)
                {
                    _activeEquipmentView = null;
                    _activeEquipmentDef = null;
                    _activeInteractionAnchor = null;
                    return false;
                }

                patient = bestPatient;
                _activeEquipmentView = bestView;
                _activeEquipmentDef = bestDef;
                _activeInteractionAnchor = bestView != null && bestView.InteractionAnchor != null
                    ? bestView.InteractionAnchor
                    : bestView != null ? bestView.transform : null;
                return true;
            }

            _activeEquipmentView = null;
            _activeEquipmentDef = null;

            patient = FindClosestPatient();
            _activeInteractionAnchor = patient != null ? patient.transform : null;
            return patient != null;
        }
    }
}
