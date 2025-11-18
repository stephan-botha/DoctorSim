# MedMania Codebase Agents

Principles: modularity, SRP, decoupling, data-driven design (ScriptableObjects), testability, readable APIs.

## Layers & Allowed Dependencies
- Core.Domain: pure gameplay/domain types. No Unity lifecycle assumptions beyond minimal MonoBehaviours where unavoidable.
- Core.Data: ScriptableObject definitions; may reference Domain interfaces/types. No Services/UI/View logic.
- Core.Services: runtime services (timing, locators). Can reference Domain. No Data authoring, no Views/UI.
- Presentation.Input: player input to domain commands. Depends only on Domain.
  - View access happens through bridging interfaces (`ICarrySlot`, `IProcedureProvider`, `IProcedureStation`) implemented in Presentation.Views.
- Presentation.Views: scene behaviours connecting Data + Services + Domain to visuals. Depends on Domain, Data, Services.
- UI: Presenters/Adapters to display state. Depends on Domain + Services.

## Extension
- Add new diseases/tests/treatments via SO assets (Core.Data) with no code changes.
- Add new procedures by implementing IProcedureDef (Domain) and authoring TestSO/TreatmentSO (Data).
- Add services by implementing interfaces, register in ServiceLocator (Services).
