# Core.Services (MedMania.Core.Services)
Purpose: Cross-cutting runtime services (timing, service location).

Depends On: Core.Domain
Used By: Presentation.Views, UI, Tests

Rules:
- No authoring data or scene-binding logic here.
- Prefer interfaces; implement with swappable components.
