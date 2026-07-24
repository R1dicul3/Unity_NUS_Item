# Gameplay Function Map

This map captures implementation-level intent that must be preserved during world-scale migration. It is based on code and scene references, not on undocumented design intent.

## Player And Camera

- `PlatformerPlayerController` owns movement, jump, dash, collider alignment, audio footsteps, and basic Animator booleans.
- Scene player instances currently use `bodySize: 1.6 x 4.5` and `groundCheckOffset: 2.25`.
- `PixelPerfectFollowCamera` follows the active player, clamps to `CameraArea` bounds, smooths position/orthographic size, and optionally snaps to a pixel grid.
- `CameraArea` is not just visual framing. Its collider bounds constrain the camera, and its `cameraSize` changes visible information density.

Migration risk: moving or resizing CameraArea can change route readability, puzzle visibility, and character-switch camera recovery.

## Room Doors

- `RoomDoor` handles in-scene room teleport.
- It can use `targetSpawn`, or auto-link to the nearest aligned door if the target is missing.
- After teleport it updates the follow camera target and transitions to `targetCameraArea`.
- It also plays door audio, optional transition music, applies exit velocity, and shows/hides the interact prompt.

Migration risk: moving doors, target spawns, or CameraArea references can break room traversal, exit direction, prompt timing, and camera transitions.

## Scene Transitions

- `SceneTransitionDoor` loads a target scene after player interaction.
- `SceneTransition` in `ChangeScene.cs` loads a target scene immediately on trigger enter.

Migration risk: trigger scale or position changes can make scene exits too easy to miss or trigger unintentionally.

## Tutorial And Interaction Prompts

- `TutorialMessageTrigger` uses trigger overlap plus optional interaction to show tutorial text.
- When `showAboveObject` is enabled, it positions UI by converting the trigger object's world position plus `textOffset` into screen coordinates.
- `RoomDoor` and `CoffeeInteractable` use `InteractPromptController.Instance.Show(transform)`.

Migration risk: moving or scaling trigger objects can move prompt anchors and tutorial text positions.

## Character Switching

- `CharacterSwitcher2D` toggles active/inactive player objects.
- On switch, it retargets `PixelPerfectFollowCamera` and calls `RefreshCameraBoundsToTarget`.
- `RefreshCameraBoundsToTarget` finds the current CameraArea by checking colliders at the active character position.

Migration risk: if CameraArea colliders do not cover intended playable positions, switching characters can clear camera bounds or lock to the wrong room.

## Hazards And Puzzles

- `Room1FloorSpikes` pauses time and shows a retry dialog when the player stays in collision with spikes.
- `RoomPillarPuzzle2D` is connected to retry/restart and camera-area movement in several places.

Migration risk: hazard collider changes can alter difficulty or failure timing. Puzzle object spacing should not be normalized without manual design review.

## Safe Migration Rule

Before changing any room, classify objects inside that room:

- High risk: player, doors, target spawns, CameraArea, scene transition triggers, tutorial triggers, hazards, puzzle objects, AirWall, collision-bearing platforms.
- Medium risk: interactable props, prompt anchors, moving platforms, objects with colliders but no gameplay script.
- Lower risk: background-only SpriteRenderers on decorative layers without colliders or scripts.

Only lower-risk objects should be considered for automatic normalization. High-risk and medium-risk objects require manual review.

