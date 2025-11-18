// 2025-10-29 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

// MedMania.UI
// PatientCardPresenter.cs
// Responsibility: Minimal UI facade for patient state (plug into Unity UI later).

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // Import the TextMeshPro namespace
using MedMania.Core.Domain.Patients;
using MedMania.Core.Domain.Diseases;
using MedMania.Core.Domain.Procedures;
using MedMania.Core.Services;
using MedMania.Core.Services.Procedures;
using MedMania.Core.Services.Patients;

namespace MedMania.UI.PatientCard
{
    public sealed class PatientCardPresenter : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _name; // Changed to TextMeshProUGUI
        [SerializeField] private TextMeshProUGUI _state; // Changed to TextMeshProUGUI
        [SerializeField] private TextMeshProUGUI _waitingTimer;
        [SerializeField] private Image _waitingFill;
        [SerializeField] private Color _waitingSafeColor = new Color(0.34901962f, 0.8235294f, 0.37254903f, 1f);
        [SerializeField] private Color _waitingWarningColor = new Color(1f, 0.64705884f, 0f, 1f);
        [SerializeField] private Color _waitingCriticalColor = new Color(0.8784314f, 0f, 0f, 1f);
        [SerializeField] private Transform _testsRow;
        [SerializeField] private Transform _treatmentsRow;
        [SerializeField] private GameObject _procedureIconPrefab;
        [SerializeField] private GameObject _unknownTreatmentIconPrefab;
        [SerializeField] private Image _avatarImage;
        [SerializeField] private MonoBehaviour _iconProviderBehaviour;
        [SerializeField] private float _iconSpacing = 96f;

        private readonly Dictionary<IProcedureDef, ProcedureIconBinding> _iconLookup = new();
        private readonly Dictionary<Transform, int> _rowCounts = new();
        private readonly List<IProcedureDef> _pendingTreatmentDefs = new();
        private readonly List<GameObject> _treatmentPlaceholders = new();
        private bool _treatmentsRevealed;

        private static readonly Color IncompleteIconColor = Color.black;
        private static readonly Color CompleteIconColor = Color.green;

        private static PatientCardManager s_manager;

        private PatientCardManager _manager;
        private bool _hasBoundPatient;

        private IPatient _patient;
        private float _waitSecondsRemaining;
        private float _waitSecondsTotal;
        private bool _hasWaitTimer;
        private IProcedureIconProvider _iconProvider;
        private bool _iconProviderLookupAttempted;
        private IPatientAvatarIconService _avatarIconService;
        private bool _avatarIconServiceLookupAttempted;

        private sealed class ProcedureIconBinding
        {
            public readonly GameObject Root;
            public readonly TextMeshProUGUI Label;
            public readonly Image Icon;

            public ProcedureIconBinding(GameObject root, TextMeshProUGUI label, Image icon)
            {
                Root = root;
                Label = label;
                Icon = icon;
            }
        }

        private void Awake()
        {
            ResolveIconProvider();
            ResolveAvatarIconService();
        }

        public void BindPatient(IPatient patient)
        {
            EnsureManagerInitialized();

            if (_manager != null && ReferenceEquals(this, _manager.Root))
            {
                _manager.HandlePatientEvent(patient);
                return;
            }

            BindPatientInternal(patient);
        }

        internal void ApplyPatientBinding(IPatient patient)
        {
            BindPatientInternal(patient);
        }

        internal void ClearPatientBinding()
        {
            _patient = null;
            _hasBoundPatient = false;
            _treatmentsRevealed = false;
            _waitSecondsRemaining = 0f;
            _waitSecondsTotal = 0f;
            _hasWaitTimer = false;

            ClearProcedureRows();
            DestroyTreatmentPlaceholders(true);

            if (_name != null)
            {
                _name.text = string.Empty;
            }

            if (_state != null)
            {
                _state.text = string.Empty;
            }

            if (_waitingTimer != null)
            {
                _waitingTimer.text = string.Empty;
            }

            UpdateWaitingVisuals(0f);
            UpdateAvatarIcon();
        }

        internal bool HasBoundPatient => _hasBoundPatient;

        private void SetManager(PatientCardManager manager, bool isRoot)
        {
            _manager = manager;

            if (!isRoot && !_hasBoundPatient && gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }

        private void EnsureManagerInitialized()
        {
            if (_manager != null)
            {
                return;
            }

            if (s_manager == null)
            {
                s_manager = new PatientCardManager();
            }

            _manager = s_manager;

            if (_manager.Root == null)
            {
                _manager.SetRoot(this);
            }
        }

        private void BindPatientInternal(IPatient patient)
        {
            if (patient == null)
            {
                ClearPatientBinding();
                return;
            }

            _patient = patient;
            _hasBoundPatient = true;
            _treatmentsRevealed = ShouldRevealTreatments();

            if (_patient.Disease != null)
            {
                BuildProcedureRows(_patient.Disease);
                _waitSecondsRemaining = Mathf.Max(0f, _patient.Disease.MaxWaitSeconds);
                _waitSecondsTotal = _waitSecondsRemaining;
                _hasWaitTimer = _waitSecondsRemaining > 0f;
            }
            else
            {
                ClearProcedureRows();
                _waitSecondsRemaining = 0f;
                _waitSecondsTotal = 0f;
                _hasWaitTimer = false;
            }

            if (_waitingTimer != null)
            {
                _waitingTimer.text = FormatTime(_waitSecondsRemaining);
            }

            var normalizedRemaining = CalculateNormalizedRemaining();
            UpdateWaitingVisuals(normalizedRemaining);
            UpdateAvatarIcon();
        }

        private void Update()
        {
            if (_patient == null)
            {
                return;
            }
            if (_name) _name.text = _patient.DisplayName; // TextMeshProUGUI uses the same .text property
            if (_state) _state.text = _patient.State.ToString(); // TextMeshProUGUI uses the same .text property

            if (_hasWaitTimer)
            {
                _waitSecondsRemaining = Mathf.Max(0f, _waitSecondsRemaining - Time.deltaTime);
            }

            if (_waitingTimer)
            {
                _waitingTimer.text = FormatTime(_waitSecondsRemaining);
            }

            var normalizedRemaining = CalculateNormalizedRemaining();
            UpdateWaitingVisuals(normalizedRemaining);

            if (!_treatmentsRevealed && ShouldRevealTreatments())
            {
                RevealTreatments();
            }

            if (_avatarImage != null && !_avatarImage.enabled)
            {
                UpdateAvatarIcon();
            }
        }

        public void HandleProcedureCompleted(IPatient patient, IProcedureDef procedure)
        {
            if (patient == null || procedure == null)
            {
                return;
            }

            EnsureManagerInitialized();

            if (_manager != null && ReferenceEquals(this, _manager.Root))
            {
                _manager.HandleProcedureCompleted(patient, procedure);
                return;
            }

            HandleProcedureCompletedInternal(patient, procedure);
        }

        internal void HandleProcedureCompletedInternal(IPatient patient, IProcedureDef procedure)
        {
            if (patient == null || procedure == null)
            {
                return;
            }

            if (_patient == null)
            {
                return;
            }

            if (!ReferenceEquals(_patient, patient) && _patient.Id != patient.Id)
            {
                return;
            }

            UpdateIconCompletion(procedure, true);

            if (!_treatmentsRevealed && ShouldRevealTreatments())
            {
                RevealTreatments();
            }
        }

        private void ClearProcedureRows()
        {
            _iconLookup.Clear();
            _rowCounts.Clear();
            _pendingTreatmentDefs.Clear();
            DestroyTreatmentPlaceholders();

            DestroyChildren(_testsRow);
            DestroyChildren(_treatmentsRow);
        }

        private void BuildProcedureRows(IDiseaseDef disease)
        {
            ClearProcedureRows();

            if (_procedureIconPrefab == null) return;

            if (disease.Tests != null && _testsRow != null)
            {
                foreach (var test in disease.Tests)
                {
                    if (test == null) continue;
                    var binding = CreateIcon(_testsRow, test);
                    _iconLookup[test] = binding;
                }
            }

            if (disease.Treatments != null && _treatmentsRow != null)
            {
                foreach (var treatment in disease.Treatments)
                {
                    if (treatment == null) continue;
                    if (_treatmentsRevealed)
                    {
                        var binding = CreateIcon(_treatmentsRow, treatment);
                        _iconLookup[treatment] = binding;
                    }
                    else
                    {
                        CreateTreatmentPlaceholder(treatment);
                    }
                }
            }
        }

        private ProcedureIconBinding CreateIcon(Transform parent, IProcedureDef procedure)
        {
            ResolveIconProvider();
            var instance = Instantiate(_procedureIconPrefab, parent);

            var image = instance.GetComponent<Image>();
            var iconView = _iconProvider != null ? _iconProvider.GetIcon(procedure) : ProcedureIconView.Empty;
            var hasIcon = iconView.HasSprite && image != null;
            if (image != null)
            {
                image.color = IncompleteIconColor;
                image.sprite = hasIcon ? iconView.Sprite : null;
                image.enabled = hasIcon;
            }

            var label = instance.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                var labelText = !string.IsNullOrWhiteSpace(iconView.LabelOverride) ? iconView.LabelOverride : procedure.Name;
                label.text = labelText;
                label.gameObject.SetActive(!hasIcon);
            }

            if (instance.TryGetComponent<RectTransform>(out var rect))
            {
                ConfigureIconTransform(parent, rect);
            }

            var bindingIcon = hasIcon ? image : null;

            return new ProcedureIconBinding(instance, label, bindingIcon);
        }

        private void CreateTreatmentPlaceholder(IProcedureDef treatment)
        {
            _pendingTreatmentDefs.Add(treatment);

            if (_treatmentsRow == null)
            {
                return;
            }

            ResolveIconProvider();

            GameObject placeholder;

            if (_iconProvider != null && _unknownTreatmentIconPrefab != null && _procedureIconPrefab != null)
            {
                placeholder = Instantiate(_procedureIconPrefab, _treatmentsRow);

                var iconView = _iconProvider.GetPrefabIcon(_unknownTreatmentIconPrefab);
                var image = placeholder.GetComponent<Image>();
                var hasIcon = iconView.HasSprite && image != null;
                if (image != null)
                {
                    image.color = IncompleteIconColor;
                    image.sprite = hasIcon ? iconView.Sprite : null;
                    image.enabled = hasIcon;
                }

                var label = placeholder.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null)
                {
                    label.text = string.Empty;
                    label.gameObject.SetActive(!hasIcon);
                }
            }
            else if (_unknownTreatmentIconPrefab != null)
            {
                placeholder = Instantiate(_unknownTreatmentIconPrefab, _treatmentsRow);
            }
            else
            {
                return;
            }

            _treatmentPlaceholders.Add(placeholder);

            if (placeholder.TryGetComponent<RectTransform>(out var rect))
            {
                ConfigureIconTransform(_treatmentsRow, rect);
            }
        }

        private void ResolveIconProvider()
        {
            if (_iconProvider != null)
            {
                return;
            }

            if (_iconProviderBehaviour != null)
            {
                _iconProvider = _iconProviderBehaviour as IProcedureIconProvider;
                if (_iconProvider == null)
                {
                    Debug.LogError($"Assigned icon provider override on {nameof(PatientCardPresenter)} does not implement {nameof(IProcedureIconProvider)}.", this);
                }
                else
                {
                    return;
                }
            }

            if (_iconProviderLookupAttempted)
            {
                return;
            }

            _iconProviderLookupAttempted = true;

            try
            {
                _iconProvider = ServiceLocator.Find<IProcedureIconProvider>();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Unable to resolve {nameof(IProcedureIconProvider)}. {ex.Message}", this);
            }
        }

        private void ResolveAvatarIconService()
        {
            if (_avatarIconService != null)
            {
                return;
            }

            if (_avatarIconServiceLookupAttempted)
            {
                return;
            }

            _avatarIconServiceLookupAttempted = true;

            try
            {
                _avatarIconService = ServiceLocator.Find<IPatientAvatarIconService>();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Unable to resolve {nameof(IPatientAvatarIconService)}. {ex.Message}", this);
            }
        }

        private void UpdateAvatarIcon()
        {
            if (_avatarImage == null)
            {
                return;
            }

            if (_patient == null)
            {
                _avatarImage.sprite = null;
                _avatarImage.enabled = false;
                return;
            }

            ResolveAvatarIconService();

            if (_avatarIconService == null)
            {
                _avatarImage.sprite = null;
                _avatarImage.enabled = false;
                return;
            }

            var iconView = _avatarIconService.GetAvatarIcon(_patient);
            var hasSprite = iconView.HasSprite;
            _avatarImage.sprite = hasSprite ? iconView.Sprite : null;
            _avatarImage.enabled = hasSprite;
        }

        private void UpdateIconCompletion(IProcedureDef procedure, bool completed)
        {
            if (procedure == null) return;

            if (_iconLookup.TryGetValue(procedure, out var binding) && binding.Icon != null)
            {
                binding.Icon.color = completed ? CompleteIconColor : IncompleteIconColor;
            }
        }

        private void RevealTreatments()
        {
            if (_treatmentsRevealed)
            {
                return;
            }

            _treatmentsRevealed = true;

            var pending = _pendingTreatmentDefs.ToArray();
            DestroyTreatmentPlaceholders(true);
            _pendingTreatmentDefs.Clear();
            if (_treatmentsRow != null)
            {
                _rowCounts.Remove(_treatmentsRow);
            }

            if (_treatmentsRow == null)
            {
                return;
            }

            foreach (var treatment in pending)
            {
                if (treatment == null) continue;
                var binding = CreateIcon(_treatmentsRow, treatment);
                _iconLookup[treatment] = binding;
            }
        }

        private void DestroyTreatmentPlaceholders(bool destroyGameObjects)
        {
            if (!destroyGameObjects)
            {
                _treatmentPlaceholders.Clear();
                return;
            }

            foreach (var placeholder in _treatmentPlaceholders)
            {
                if (placeholder == null) continue;
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    DestroyImmediate(placeholder);
                    continue;
                }
#endif
                Destroy(placeholder);
            }

            _treatmentPlaceholders.Clear();
        }

        private void DestroyTreatmentPlaceholders()
        {
            DestroyTreatmentPlaceholders(false);
        }

        private void ConfigureIconTransform(Transform parent, RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            var usesManualLayout = parent == null || parent.GetComponent<LayoutGroup>() == null;
            if (usesManualLayout)
            {
                int index = 0;
                if (parent != null)
                {
                    _rowCounts.TryGetValue(parent, out index);
                    _rowCounts[parent] = index + 1;
                }

                rect.anchorMin = new Vector2(0f, 0.5f);
                rect.anchorMax = new Vector2(0f, 0.5f);
                rect.pivot = new Vector2(0f, 0.5f);
                rect.anchoredPosition = new Vector2(index * _iconSpacing, 0f);
            }
            else if (parent is RectTransform layoutParent)
            {
                _rowCounts.Remove(parent);
                LayoutRebuilder.MarkLayoutForRebuild(layoutParent);
            }
        }

        private bool ShouldRevealTreatments()
        {
            if (_patient == null)
            {
                return true;
            }

            if (_patient.TotalTestCount <= 0)
            {
                return true;
            }

            return _patient.AreAllTestsCompleted;
        }

        private void DestroyChildren(Transform parent)
        {
            if (parent == null) return;

            _rowCounts.Remove(parent);

            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                if (child == null) continue;

#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    DestroyImmediate(child.gameObject);
                    continue;
                }
#endif
                Destroy(child.gameObject);
            }
        }

        private static string FormatTime(float seconds)
        {
            var clamped = Mathf.Max(0f, seconds);
            int minutes = Mathf.FloorToInt(clamped / 60f);
            int secs = Mathf.FloorToInt(clamped % 60f);
            return $"{minutes:00}:{secs:00}";
        }

        private float CalculateNormalizedRemaining()
        {
            if (!_hasWaitTimer || _waitSecondsTotal <= Mathf.Epsilon)
            {
                return 0f;
            }

            return Mathf.Clamp01(_waitSecondsRemaining / Mathf.Max(Mathf.Epsilon, _waitSecondsTotal));
        }

        private void UpdateWaitingVisuals(float normalizedRemaining)
        {
            if (_waitingFill == null)
            {
                return;
            }

            var shouldShow = _hasWaitTimer && _waitSecondsTotal > 0f;
            var container = _waitingFill.transform.parent != null ? _waitingFill.transform.parent.gameObject : null;
            var target = container != null ? container : _waitingFill.gameObject;

            if (target.activeSelf != shouldShow)
            {
                target.SetActive(shouldShow);
            }

            if (!shouldShow)
            {
                return;
            }

            _waitingFill.fillAmount = Mathf.Clamp01(normalizedRemaining);
            _waitingFill.color = GetWaitColor(_waitSecondsRemaining);
        }

        private Color GetWaitColor(float secondsRemaining)
        {
            if (secondsRemaining < 30f)
            {
                return _waitingCriticalColor;
            }

            if (secondsRemaining < 60f)
            {
                return _waitingWarningColor;
            }

            return _waitingSafeColor;
        }

        private sealed class PatientCardManager
        {
            private PatientCardPresenter _root;
            private readonly List<PatientCardPresenter> _presenters = new();
            private readonly Dictionary<Guid, PatientCardPresenter> _assignments = new();
            private readonly HashSet<Guid> _activePatients = new();

            public PatientCardPresenter Root => _root;

            public void SetRoot(PatientCardPresenter presenter)
            {
                if (_root == null)
                {
                    _root = presenter;
                }

                RegisterPresenter(presenter, ReferenceEquals(_root, presenter));
            }

            public void HandlePatientEvent(IPatient patient)
            {
                if (patient == null)
                {
                    return;
                }

                var id = patient.Id;
                if (_activePatients.Contains(id))
                {
                    if (_assignments.TryGetValue(id, out var existing))
                    {
                        RemovePatient(id, existing);
                    }

                    _activePatients.Remove(id);
                    return;
                }

                if (patient.State == PatientState.ReadyForDischarge || patient.State == PatientState.Discharged)
                {
                    return;
                }

                var presenter = AcquirePresenter();
                _assignments[id] = presenter;
                _activePatients.Add(id);
                presenter.ApplyPatientBinding(patient);
            }

            public void HandleProcedureCompleted(IPatient patient, IProcedureDef procedure)
            {
                if (patient == null || procedure == null)
                {
                    return;
                }

                if (_assignments.TryGetValue(patient.Id, out var presenter) && presenter != null)
                {
                    presenter.HandleProcedureCompletedInternal(patient, procedure);
                }
            }

            private void RegisterPresenter(PatientCardPresenter presenter, bool isRoot)
            {
                if (!_presenters.Contains(presenter))
                {
                    _presenters.Add(presenter);
                }

                presenter.SetManager(this, isRoot);
            }

            private PatientCardPresenter AcquirePresenter()
            {
                foreach (var presenter in _presenters)
                {
                    if (!presenter.HasBoundPatient)
                    {
                        presenter.gameObject.SetActive(true);
                        return presenter;
                    }
                }

                if (_root == null)
                {
                    throw new InvalidOperationException("PatientCardManager requires a root presenter instance before creating clones.");
                }

                var cloneObject = UnityEngine.Object.Instantiate(_root.gameObject, _root.transform.parent);
                var presenterClone = cloneObject.GetComponent<PatientCardPresenter>();
                RegisterPresenter(presenterClone, false);
                presenterClone.gameObject.SetActive(true);
                return presenterClone;
            }

            private void RemovePatient(Guid id, PatientCardPresenter presenter)
            {
                _assignments.Remove(id);
                _activePatients.Remove(id);
                presenter.ClearPatientBinding();

                if (!ReferenceEquals(presenter, _root))
                {
                    _presenters.Remove(presenter);
                    UnityEngine.Object.Destroy(presenter.gameObject);
                }
                else
                {
                    presenter.gameObject.SetActive(false);
                }
            }
        }
    }
}
