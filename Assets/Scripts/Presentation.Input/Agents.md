# Presentation.Input (MedMania.Presentation.Input)
Purpose: Translate player input into domain commands.

Depends On: Core.Domain
Bridging Contracts (implemented in Presentation.Views):
- `ICarrySlot` → `CarrySlot`
- `IProcedureProvider` → `ToolView`, `EquipmentView`
- `IProcedureStation` → `EquipmentView`

Used By: Scenes

Rules:
- Keep responsibilities narrow (movement, pick/drop, invoke procedure attempts).
- Drive view components only via the contracts above or Unity primitives—no direct `Presentation.Views` dependencies.
- No data authoring or UI here.
