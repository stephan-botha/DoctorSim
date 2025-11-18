# Core.Domain (MedMania.Core.Domain)
Purpose: Pure game rules and state. No scene references. No ScriptableObjects.

Depends On: (none)
Used By: Core.Data, Core.Services, Presentation.Input, Presentation.Views, UI

Key Public Types:
- IPatient: Read/write patient state + operations.
- Patient: Concrete implementation of patient state machine.
- IDiseaseDef, IProcedureDef: Contracts consumed by domain.
- IToolDef, IEquipmentDef, ICarryable: Inventory abstractions.
- IProcedureContext: Context provided by runner (time, cancellation).
- ProcedureRun: Pure logic runner using IProcedureDef + IProcedureContext.

Rules:
- Add new domain operations via interfaces, keep SRP, prefer small immutable messages.
- Do NOT reference MonoBehaviours, SOs, or Services.
