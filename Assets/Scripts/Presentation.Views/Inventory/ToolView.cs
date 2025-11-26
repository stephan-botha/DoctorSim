// MedMania.Presentation.Views
// ToolView.cs
// Responsibility: Scene representation for tools, exposing carry + procedure metadata.

using UnityEngine;
using UnityEngine.Animations;
using MedMania.Core.Data.ScriptableObjects;
using MedMania.Core.Domain.Inventory;
using MedMania.Core.Domain.Procedures;
using MedMania.Presentation.Input.Staff;
using MedMania.Presentation.Views.Patients;
using MedMania.Presentation.Views.Procedures;

namespace MedMania.Presentation.Views.Inventory
{
    public sealed class ToolView : MonoBehaviour, ICarryable, IProcedureProvider
    {
        [SerializeField] private ToolSO _tool;
        [SerializeField] private ProcedureSOBase _procedure;
        [SerializeField] private ProcedureRunner _procedureRunner;
        [SerializeField, Tooltip("Parent constraint driving the tool toward the procedure target during interaction.")]
        private ParentConstraint _parentConstraint;
        [SerializeField, Tooltip("Weight change speed when snapping to a target.")] private float _attachLerpSpeed = 8f;
        [SerializeField, Tooltip("Weight change speed when releasing to the default pose.")] private float _detachLerpSpeed = 12f;

        private float _targetConstraintWeight;

        public ToolSO ToolAsset => _tool;
        public IToolDef Tool => _tool;
        public ProcedureSOBase ProcedureAsset => _procedure;
        public IProcedureDef Procedure => _procedure;

        IProcedureDef IProcedureProvider.Procedure => Procedure;
        public string DisplayName => _tool ? _tool.Name : _procedure ? _procedure.Name : name;

        private void Awake()
        {
            if (_parentConstraint == null)
            {
                _parentConstraint = GetComponent<ParentConstraint>();
            }

            if (_procedureRunner == null)
            {
                _procedureRunner = GetComponentInParent<ProcedureRunner>();
            }

            if (_parentConstraint != null)
            {
                _parentConstraint.weight = 0f;
                _parentConstraint.constraintActive = false;
            }
        }

        private void OnEnable()
        {
            BindRunner(_procedureRunner);
        }

        private void OnDisable()
        {
            BindRunner(null);
            SetConstraintTarget(null, 0f);
        }

        private void Update()
        {
            UpdateConstraintWeight();
        }

        public void BindRunner(ProcedureRunner runner)
        {
            if (_procedureRunner == runner)
            {
                return;
            }

            if (_procedureRunner != null)
            {
                _procedureRunner.onInteractionAnchorResolved.RemoveListener(HandleAnchorResolved);
                _procedureRunner.onPatientReset.RemoveListener(HandleReset);
                _procedureRunner.onCompleted.RemoveListener(HandleCompletion);
            }

            _procedureRunner = runner;

            if (_procedureRunner != null)
            {
                _procedureRunner.onInteractionAnchorResolved.AddListener(HandleAnchorResolved);
                _procedureRunner.onPatientReset.AddListener(HandleReset);
                _procedureRunner.onCompleted.AddListener(HandleCompletion);
            }
        }

        private void HandleAnchorResolved(Transform anchor)
        {
            float targetWeight = anchor != null ? 1f : 0f;
            SetConstraintTarget(anchor, targetWeight);
        }

        private void HandleReset(PatientView _)
        {
            SetConstraintTarget(null, 0f);
        }

        private void HandleCompletion(IProcedureDef _)
        {
            SetConstraintTarget(null, 0f);
        }

        private void SetConstraintTarget(Transform anchor, float targetWeight)
        {
            if (_parentConstraint == null)
            {
                _targetConstraintWeight = 0f;
                return;
            }

            var source = new ConstraintSource { sourceTransform = anchor, weight = 1f };

            if (anchor != null)
            {
                if (_parentConstraint.sourceCount == 0)
                {
                    _parentConstraint.AddSource(source);
                }
                else
                {
                    _parentConstraint.SetSource(0, source);
                }

                _parentConstraint.constraintActive = true;
            }
            else
            {
                _parentConstraint.constraintActive = _parentConstraint.weight > 0f;
            }

            _targetConstraintWeight = Mathf.Clamp01(targetWeight);
        }

        private void UpdateConstraintWeight()
        {
            if (_parentConstraint == null)
            {
                return;
            }

            var speed = _targetConstraintWeight >= _parentConstraint.weight ? _attachLerpSpeed : _detachLerpSpeed;
            var nextWeight = Mathf.MoveTowards(_parentConstraint.weight, _targetConstraintWeight, speed * Time.deltaTime);
            _parentConstraint.weight = nextWeight;

            if (Mathf.Approximately(nextWeight, 0f))
            {
                _parentConstraint.constraintActive = false;
            }
        }
    }
}
