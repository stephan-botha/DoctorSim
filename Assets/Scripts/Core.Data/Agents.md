# Core.Data (MedMania.Core.Data)
Purpose: Data-driven authoring via ScriptableObjects.

Depends On: Core.Domain
Used By: Presentation.Views, Tests

Public Types:
- DiseaseSO, TestSO, TreatmentSO, ToolSO, EquipmentSO

Rules:
- Only data, no logic beyond simple validation/mapping to domain interfaces.
- Keep serialized fields private + [SerializeField]. Expose read-only props.
