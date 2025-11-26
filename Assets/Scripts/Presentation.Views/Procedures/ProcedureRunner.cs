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
using MedMania.Presentation.Input.Staff;
using MedMania.Presentation.Views.Inventory;

namespace MedMania.Presentation.Views.Procedures
{
    public sealed class ProcedureRunner : MonoBehaviour, IProcedureContext, IProcedureRunInputContext
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
        [SerializeField] private Camera _targetingCamera;

        private IGameTimer _timer;
        private System.IDisposable _activeRun;
        private Patients.PatientView _activePatient;
        private IProcedureDef _activeProcedure;
        private IProcedurePerformer _performer;
        private EquipmentView _activeEquipmentView;
        private IEquipmentDef _activeEquipmentDef;
        private Transform _activeInteractionAnchor;
        private ProcedureProgressTracker _progressTracker;
        private ProcedureTargetResolver _targetResolver;
        private ProcedureRunEvents _events;
        private event System.Action<IProcedureDef> _runStarted;
        private event System.Action<IProcedureDef> _runCompleted;

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
        public bool HasActiveRun => _activeRun != null;

        event System.Action<IProcedureDef> IProcedureRunInputContext.RunStarted
        {
            add => _runStarted += value;
            remove => _runStarted -= value;
        }

        event System.Action<IProcedureDef> IProcedureRunInputContext.RunCompleted
        {
            add => _runCompleted += value;
            remove => _runCompleted -= value;
        }

        private void Awake()
        {
            _progressTracker = new ProcedureProgressTracker();
            _targetResolver = new ProcedureTargetResolver(transform, _range);
            _events = new ProcedureRunEvents(_onStarted, _onProgress, _onCompleted, _onPatientStarted, _onPatientProgress, _onPatientCompleted, _onDomainPatientCompleted, _onPatientReset, _onInteractionAnchorResolved);

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
            _targetResolver.UpdateRange(_range);

            if (!ServiceLocator.TryGet(out _timer))
            {
                Debug.LogWarning($"Unable to resolve {nameof(GameTimerService)} for {nameof(ProcedureRunner)}.", this);
            }

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
            ServiceLocator.TryGet(out _timer);

            if (_timer == null)
            {
                Debug.LogWarning($"Cannot schedule procedure run because {nameof(GameTimerService)} is unavailable.", this);
                return null;
            }

            return _timer.Schedule((float)duration.TotalSeconds, onCompleted);
        }

        public bool IsEquipmentAvailable(IEquipmentDef equipment)
        {
            return equipment == _activeEquipmentDef;
        }

        public bool TryValidateTarget(IProcedureDef procedure, out Transform interactionAnchor)
        {
            interactionAnchor = null;

            var hasRay = TryBuildTargetingRay(out var rayOrigin, out var rayDirection);
            if (!_targetResolver.TryResolve(procedure, hasRay ? rayOrigin : Vector3.zero, hasRay ? rayDirection : Vector3.zero, out _, out _, out _, out interactionAnchor))
            {
                return false;
            }

            return true;
        }

        public bool TryCancelActiveRun()
        {
            if (_activeRun == null)
            {
                return false;
            }

            CancelActiveRun();
            return true;
        }

        public void ResetCachedTarget()
        {
            _targetResolver.ResetCachedTarget();
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

            var targetingRay = TryBuildTargetingRay(out var rayOrigin, out var rayDirection);

            if (!_targetResolver.TryResolve(procedure, targetingRay ? rayOrigin : Vector3.zero, targetingRay ? rayDirection : Vector3.zero, out var patient, out var equipmentView, out var equipmentDef, out var interactionAnchor))
            {
                return;
            }

            _activePatient = patient;
            _activeProcedure = procedure;
            _activeEquipmentView = equipmentView;
            _activeEquipmentDef = equipmentDef;
            _activeInteractionAnchor = interactionAnchor;
            _progressTracker.Begin(procedure.DurationSeconds);

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

            if (!_targetResolver.IsCachedTargetStillValid(_activeProcedure, out var patient, out var equipmentView, out var equipmentDef, out var anchor))
            {
                CancelActiveRun();
                return;
            }

            _activePatient = patient;
            _activeEquipmentView = equipmentView;
            _activeEquipmentDef = equipmentDef;
            _activeInteractionAnchor = anchor;

            if (_progressTracker.IsRunning && _progressTracker.HasDuration)
            {
                var progress = _progressTracker.GetProgress();
                _events.NotifyProgress(progress, _activePatient);
            }
        }

        private void HandleRunStarted(IProcedureDef procedure)
        {
            var initialProgress = _progressTracker.InitialProgress;
            var patient = _activePatient;

            _events.NotifyStarted(procedure, initialProgress, patient, _activeInteractionAnchor);
            _runStarted?.Invoke(procedure);
        }

        private void HandleRunCompleted(IProcedureDef procedure)
        {
            var patient = _activePatient;

            _events.NotifyCompleted(procedure, patient);
            _events.ClearInteractionAnchor();
            _runCompleted?.Invoke(procedure);
            ResetActiveRunState();
        }

        private void CancelActiveRun()
        {
            var procedure = _activeProcedure;
            var run = _activeRun;
            var patient = _activePatient;

            if (run != null)
            {
                run.Dispose();
                _events.NotifyReset(patient);
            }

            _events.ClearInteractionAnchor();
            if (procedure != null)
            {
                _runCompleted?.Invoke(procedure);
            }
            ResetActiveRunState();
        }

        private void ResetActiveRunState()
        {
            _activeRun = null;
            _activePatient = null;
            _activeProcedure = null;
            _activeEquipmentView = null;
            _activeEquipmentDef = null;
            _activeInteractionAnchor = null;
            _progressTracker.Stop();
            _targetResolver.ResetCachedTarget();
        }

        private bool TryBuildTargetingRay(out Vector3 origin, out Vector3 direction)
        {
            var camera = _targetingCamera != null ? _targetingCamera : Camera.main;
            if (camera == null)
            {
                origin = Vector3.zero;
                direction = Vector3.zero;
                return false;
            }

            var cameraTransform = camera.transform;
            origin = cameraTransform.position;
            direction = cameraTransform.forward;
            return true;
        }
    }
}
