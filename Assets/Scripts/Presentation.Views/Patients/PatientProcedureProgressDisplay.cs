// MedMania.Presentation.Views
// PatientProcedureProgressDisplay.cs
// Responsibility: World-space progress indicator that follows a patient and updates from ProcedureRunner events.

using UnityEngine;
using UnityEngine.UI;
using MedMania.Core.Domain.Procedures;
using MedMania.Presentation.Views.Procedures;

namespace MedMania.Presentation.Views.Patients
{
    [RequireComponent(typeof(PatientView))]
    public sealed class PatientProcedureProgressDisplay : MonoBehaviour
    {
        [SerializeField] private Canvas _canvas;
        [SerializeField] private Image _fillImage;
        [SerializeField] private bool _billboardToCamera = true;
        [SerializeField] private bool _hideOnAwake = true;

        private PatientView _patientView;
        private ProcedureRunner _boundRunner;

        private void Awake()
        {
            _patientView = GetComponent<PatientView>();

            if (_canvas == null)
            {
                _canvas = GetComponentInChildren<Canvas>(true);
            }

            if (_fillImage == null && _canvas != null)
            {
                _fillImage = _canvas.GetComponentInChildren<Image>(true);
            }

            if (_hideOnAwake)
            {
                SetFill(0f);
                SetVisible(false);
            }
        }

        private void OnDisable()
        {
            if (_boundRunner != null)
            {
                Unsubscribe(_boundRunner);
                _boundRunner = null;
            }
        }

        public void BindRunner(ProcedureRunner runner)
        {
            if (_boundRunner == runner)
            {
                return;
            }

            if (_boundRunner != null)
            {
                Unsubscribe(_boundRunner);
            }

            _boundRunner = runner;

            if (_boundRunner != null)
            {
                Subscribe(_boundRunner);
            }
        }

        private void LateUpdate()
        {
            if (!_billboardToCamera || _canvas == null)
            {
                return;
            }

            var cam = Camera.main;
            if (cam == null)
            {
                return;
            }

            var target = _canvas.transform;
            var forward = cam.transform.forward;
            var up = Vector3.up;
            target.rotation = Quaternion.LookRotation(forward, up);
        }

        public void HandleProcedureStarted(PatientView patient, IProcedureDef procedure)
        {
            if (!IsTargetPatient(patient))
            {
                return;
            }

            SetVisible(true);
            var progress = procedure != null && procedure.DurationSeconds <= 0f ? 1f : 0f;
            SetFill(progress);
        }

        public void HandleProcedureProgress(PatientView patient, float progress)
        {
            if (!IsTargetPatient(patient))
            {
                return;
            }

            SetVisible(true);
            SetFill(progress);
        }

        public void HandleProcedureCompleted(PatientView patient, IProcedureDef procedure)
        {
            if (!IsTargetPatient(patient))
            {
                return;
            }

            SetFill(1f);
            SetVisible(false);
        }

        public void HandleProcedureReset(PatientView patient)
        {
            if (!IsTargetPatient(patient))
            {
                return;
            }

            SetFill(0f);
            SetVisible(false);
        }

        private void Subscribe(ProcedureRunner runner)
        {
            runner.onPatientStarted.AddListener(HandleProcedureStarted);
            runner.onPatientProgress.AddListener(HandleProcedureProgress);
            runner.onPatientCompleted.AddListener(HandleProcedureCompleted);
            runner.onPatientReset.AddListener(HandleProcedureReset);
        }

        private void Unsubscribe(ProcedureRunner runner)
        {
            runner.onPatientStarted.RemoveListener(HandleProcedureStarted);
            runner.onPatientProgress.RemoveListener(HandleProcedureProgress);
            runner.onPatientCompleted.RemoveListener(HandleProcedureCompleted);
            runner.onPatientReset.RemoveListener(HandleProcedureReset);
        }

        private bool IsTargetPatient(PatientView patient)
        {
            return patient != null && patient == _patientView;
        }

        private void SetVisible(bool visible)
        {
            if (_canvas == null)
            {
                return;
            }

            if (_canvas.gameObject.activeSelf != visible)
            {
                _canvas.gameObject.SetActive(visible);
            }
        }

        private void SetFill(float progress)
        {
            if (_fillImage == null)
            {
                return;
            }

            _fillImage.fillAmount = Mathf.Clamp01(progress);
        }
    }
}
