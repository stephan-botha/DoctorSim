// MedMania.Presentation.Views
// ToolView.cs
// Responsibility: Scene representation for tools, exposing carry + procedure metadata.

using UnityEngine;
using UnityEngine.Animations;
using MedMania.Core.Data.ScriptableObjects;
using MedMania.Core.Domain.Inventory;
using MedMania.Core.Domain.Procedures;
using MedMania.Presentation.Input.Staff;
using MedMania.Presentation.Views.Procedures;

namespace MedMania.Presentation.Views.Inventory
{
    public sealed class ToolView : MonoBehaviour, ICarryable, IProcedureProvider, IProcedureContextSource, IProcedureAnchorHandler
    {
        [SerializeField] private ToolSO _tool;
        [SerializeField] private ProcedureSOBase _procedure;
        [SerializeField] private ParentConstraint _parentConstraint;

        private ProcedureRunner _procedureRunner;
        private bool _hasAnchor;

        public ToolSO ToolAsset => _tool;
        public IToolDef Tool => _tool;
        public ProcedureSOBase ProcedureAsset => _procedure;
        public IProcedureDef Procedure => _procedure;

        public IProcedureRunInputContext ProcedureContext => _procedureRunner;

        IProcedureDef IProcedureProvider.Procedure => Procedure;
        public string DisplayName => _tool ? _tool.Name : _procedure ? _procedure.Name : name;

        private void Awake()
        {
            ValidateConstraint();
            RefreshProcedureRunner();
        }

        private void OnEnable()
        {
            RefreshProcedureRunner();
        }

        private void OnDisable()
        {
            DetachFromRunner();
            ApplyInteractionAnchor(null);
        }

        private void OnTransformParentChanged()
        {
            RefreshProcedureRunner();
        }

        private void OnValidate()
        {
            ValidateConstraint();
        }

        public void ApplyInteractionAnchor(Transform anchor)
        {
            if (_parentConstraint == null)
            {
                return;
            }

            if (anchor == null)
            {
                _parentConstraint.weight = 0f;
                _parentConstraint.SetSources(System.Array.Empty<ConstraintSource>());
                _parentConstraint.constraintActive = false;
                _hasAnchor = false;
                return;
            }

            var source = new ConstraintSource { sourceTransform = anchor, weight = 1f };
            if (!_hasAnchor || _parentConstraint.sourceCount == 0)
            {
                _parentConstraint.SetSources(new[] { source });
            }
            else
            {
                _parentConstraint.SetSource(0, source);
            }

            _parentConstraint.constraintActive = true;
            _hasAnchor = true;
        }

        public void SetConstraintWeight(float weight)
        {
            if (_parentConstraint == null)
            {
                return;
            }

            _parentConstraint.weight = Mathf.Clamp01(weight);
        }

        private void RefreshProcedureRunner()
        {
            var runner = GetComponentInParent<ProcedureRunner>();
            if (ReferenceEquals(_procedureRunner, runner))
            {
                return;
            }

            DetachFromRunner();

            _procedureRunner = runner;
            if (_procedureRunner != null)
            {
                _procedureRunner.onInteractionAnchorResolved.AddListener(ApplyInteractionAnchor);
            }
        }

        private void DetachFromRunner()
        {
            if (_procedureRunner != null)
            {
                _procedureRunner.onInteractionAnchorResolved.RemoveListener(ApplyInteractionAnchor);
            }

            _procedureRunner = null;
        }

        private void ValidateConstraint()
        {
            if (_parentConstraint == null)
            {
                _parentConstraint = GetComponent<ParentConstraint>();
            }
        }
    }
}
