# Tool Procedure Refactor Guide

This guide walks through the scene and prefab changes required for procedure-targeted tools. It focuses on aligning patient targets, tool constraints, and input bindings so that tools snap to the correct hand and respond to left-mouse actions while preserving existing interaction keys.

## Patient prefabs: procedure targets

1. **Add the `PatientProcedureTargets` component** to each patient prefab that supports targeted procedures.
2. Under the patient root, **create child transforms for each procedure target** (e.g., `Targets/Heart`, `Targets/IVArm`).
   - Place them at the exact world positions where tools should collide.
   - Add a **trigger collider** to each target transform (e.g., `CapsuleCollider` or `BoxCollider`) and set the layer to the interaction layer used for tool collisions.
   - Keep the collider radius/size just large enough to detect the tool tip without clipping neighboring anatomy.
3. In the `PatientProcedureTargets` inspector, **assign each target transform to the matching procedure enum/ID**. Unassigned entries should be `None` to avoid null references at runtime.
4. **Prefab checklist (patients)**:
   - Patient prefab contains `PatientProcedureTargets` on the root or a top-level child.
   - Each target transform has a trigger collider and correct interaction layer.
   - Target names match the procedure identifiers used by `PatientProcedureRunner`.
   - Optional gizmo/visual helper is disabled in production but available in-editor for placement.

### Inspector snippet

```text
Patient (Prefab)
└── Components
    • PatientProcedureTargets
        - Heart: Targets/Heart (Trigger Collider, Layer: Interactable)
        - IVArm: Targets/IVArm (Trigger Collider, Layer: Interactable)
```

## Tool prefabs: constraint setup

1. **Add a `ParentConstraint` component** to every tool prefab that needs to follow the player hand.
2. In the constraint’s source list, **bind the player hand transform** (usually the right-hand bone or hand socket) as the first source.
   - Keep the source weight at `0` in the prefab. The runtime controller (e.g., `PatientProcedureRunner` or the tool equip script) should drive the weight to `1` when equipped and back to `0` when released.
   - Ensure `Maintain Offset` is enabled if the tool origin does not align with the hand socket.
3. Verify the tool still includes existing pickup/equipment scripts that listen for **`E`** so that the interaction flow is unchanged.
4. **Prefab checklist (tools)**:
   - `ParentConstraint` present with player hand as Source 0.
   - Source weight defaults to `0`; driven at runtime when equipped.
   - Tool collider/rigidbody layers remain on the interaction layer for target detection.
   - Existing `E` bindings for pickup/equipment remain intact.

### Inspector snippet

```text
Scalpel (Prefab)
└── Components
    • Rigidbody (Layer: ToolInteraction)
    • Collider (Trigger)
    • ParentConstraint
        - Sources[0]: Player/Armature/RightHand (Weight: 0, Maintain Offset: ✓)
    • ToolEquipController (uses E)
```

## Input wiring

- **Left Mouse Button (LMB)** should be mapped to the tool action (e.g., `UseTool` or `PerformProcedure`). Ensure the action is in the default input asset and consumed by the equipped tool script.
- Preserve the **`E` key** for pickup/equip interactions. Do not remap or overload `E` when adding the LMB action.

## Runtime flow and dependencies

- At runtime, the tool script should **fetch the active `PatientProcedureRunner`** via a service locator, scene query, or serialized reference set by the spawner. Avoid storing hard references on prefabs that point back to tool instances to prevent circular dependencies.
- The runner should **resolve patient targets from `PatientProcedureTargets`** and expose them to the equipped tool. Keep the direction of dependency one-way: tools depend on the runner, and the runner depends on patient targets. Avoid tools directly referencing patient prefabs or runners caching tool components for reuse.
- When enabling the `ParentConstraint` weight, the runner or equip script should verify that the hand transform is valid before toggling the weight to avoid constraint errors in scenes without a player rig.

## Layer and collider guidance

- **Interaction layer**: ensure both tool colliders and patient target colliders share the designated interaction layer so physics queries and trigger events fire correctly.
- **Gizmos**: enable `Draw Gizmos` on target components during placement to visualize collider bounds; disable in release builds to avoid clutter.

## Quick placement workflow

1. Duplicate an existing patient with targets for reference.
2. Move target children to the correct anatomy locations using local pivots.
3. Set colliders to `Is Trigger` and confirm interaction layers.
4. Add/verify `ParentConstraint` on the tool prefab and assign the player hand source.
5. Test in Play Mode: pick up with **`E`**, equip tool, press **LMB** near a target to trigger the procedure, and drop to confirm constraint weight resets.
