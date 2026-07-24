# Scale And Feel Change Log

This document records the project discussion and implementation trail for the world-scale, feel, guidance, and乱码 cleanup pass.

## Conversation Trail

1. The project was first reset by pulling the remote `new` branch over the local worktree, then updated again from remote.
2. A build-focused pass identified several issues: pause-time save UI close timing, scattered `Time.timeScale` writes, save-slot overwrite messaging mismatch, and committed build artifacts. These were intentionally left unchanged when they did not reproduce.
3. The next focus shifted to large-scale乱码. The cause was treated as text encoding corruption in comments, Tooltip/Header strings, and some logs. The agreed approach was to repair readable text without touching gameplay behavior.
4. The project direction discussion moved to platformer feel and guidance. The working hypothesis was that feel problems came from world scale, camera framing, movement ratios, inconsistent PPU, mixed Grid sizes, and limited animation/feedback.
5. A world-scale audit found the active character is about `4.5` to `4.7` Unity units tall, built around `16 PPU`. The current player collider is broadly aligned with that visual size.
6. Safe documentation and editor audit tooling were added before changing scene geometry, so future scale work can be inspected room by room.
7. Movement tuning was normalized conservatively. `Scene_main` and `Player.prefab` moved toward the tested profile. `Scene_2` kept higher jumps because its platforms include just-reachable jumps.
8. The user confirmed the tested feel and authorized continuing without stopping for each design gate.
9. Final work proceeded through feedback, guidance text, script readability, build-warning cleanup, and documentation.

## Code And Scene Changes

- `PlatformerPlayerController`
  - Normalizes default movement profile around `moveSpeed 9`, `jumpVelocity 15`, and `dashSpeed 19` through scene/prefab values.
  - Updates walking animation logic so walking only plays while grounded and actually moving horizontally.
  - Adds safe Animator parameter writes for `IsGrounded`, `IsDashing`, and `VerticalSpeed`.
  - Adds runtime dash trail creation only for characters that currently have dash ability.
  - Adds lightweight landing squash and dash stretch on the visual transform only. Physics, collider size, jump height, and dash distance are not changed by this feedback.

- `Walk_0.controller`
  - Adds Animator parameters needed by the controller code: `IsGrounded`, `IsDashing`, and `VerticalSpeed`.

- `Scene_main`
  - Sets gameplay camera pixel snapping baseline from `128` to `16`.
  - Applies the confirmed movement profile to the scene player.
  - Cleans tutorial text: jump casing, vending-machine prompt, and interaction wording.

- `Scene_2`
  - Sets gameplay camera pixel snapping baseline from `128` to `16`.
  - Normalizes horizontal speed and dash speed to the confirmed profile.
  - Keeps existing higher jump velocities to preserve platform reachability.
  - Cleans tutorial text for character switching and coffee/platform guidance.

- `Player.prefab`
  - Updates base player movement values to match the confirmed profile.

- Camera and interaction scripts
  - Cleans乱码 in `CameraArea`, `PixelPerfectFollowCamera`, `CharacterSwitcher2D`, `RoomDoor`, `SceneTransitionDoor`, `TutorialMessageTrigger`, and `VendingMachine`.
  - Keeps runtime behavior equivalent except for replacing one obsolete `FindObjectOfType<T>()` fallback with `FindFirstObjectByType<T>()`.

- `RoomPillarPuzzle2D`
  - Uses the serialized `alignmentTolerance` field for completion tolerance instead of a hard-coded local constant.
  - Removes the unused `restartPlatformName` field.

- `WorldScaleAuditWindow`
  - Adds project/scene scale audit menu tools.
  - Adds selected `CameraArea` analysis with risk counts, preset classification, and reference player screen-height percentage.

## Documentation Added

- `WorldScaleGuidelines.md`: project-level scale baseline and migration strategy.
- `WorldScaleAuditReport.md`: current PPU, Grid, CameraArea, movement, and transform-scale findings.
- `MovementScaleMeasurements.md`: movement profile converted into player-height ratios.
- `GameplayFunctionMap.md`: functional intent map for player, camera, doors, tutorials, save state, hazards, and puzzle systems.
- `ScaleFeelChangeLog.md`: this conversation and implementation record.

## Validation

- Encoding-corruption scan across `Assets/Scripts`, `Assets/Scenes/Gameplay`, and `Assets/Docs` no longer finds the tracked corruption markers or the old character-switching typo.
- `dotnet build DesignTesting.slnx` succeeds with `0` warnings and `0` errors after the final code pass.

## Deliberately Not Changed

- Pause-time save UI close behavior.
- Centralized `Time.timeScale` state management.
- Save-slot overwrite semantics.
- Removing committed build artifacts.
- Global PPU rewrites for imported assets.
- Scene geometry, platform placement, door placement, trigger placement, or CameraArea sizes.
