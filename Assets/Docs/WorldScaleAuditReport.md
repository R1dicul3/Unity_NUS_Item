# World Scale Audit Report

Generated from the current project files. This report is informational only and does not imply that all listed items should be changed immediately.

## Current Baseline Evidence

- Main playable character sprites use 16 PPU.
- Main playable character frames are about 72 to 75 px tall, so the visible character height is about 4.5 to 4.7 Unity units.
- Gameplay scene player colliders use `bodySize: 1.6 x 4.5`, which is broadly aligned with the current visible character height.
- Existing gameplay scenes therefore appear to be built around a large-character 16 PPU baseline, not a small 1-unit character baseline.

## Player Parameter Drift

These were the first safe gameplay-feel normalization targets. Current values are:

- `Assets/Scenes/Gameplay/Scene_main.unity:6901` uses `moveSpeed: 9`.
- `Assets/Scenes/Gameplay/Scene_main.unity:6904` uses `jumpVelocity: 15`.
- `Assets/Scenes/Gameplay/Scene_main.unity:6912` uses `dashSpeed: 19`.
- `Assets/Scenes/Gameplay/Scene_2.unity:5122` uses `moveSpeed: 9`.
- `Assets/Scenes/Gameplay/Scene_2.unity:5125` uses `jumpVelocity: 16`.
- `Assets/Scenes/Gameplay/Scene_2.unity:5133` uses `dashSpeed: 19`.
- `Assets/Scenes/Gameplay/Scene_2.unity:39873` uses `moveSpeed: 9`.
- `Assets/Scenes/Gameplay/Scene_2.unity:39876` uses `jumpVelocity: 17`.
- `Assets/Scenes/Gameplay/Scene_2.unity:39884` uses `dashSpeed: 19`.

`Scene_2` keeps higher jump velocities because the room contains just-reachable platforms and lowering jump height would risk blocking progression.

## Grid Scale Drift

Current gameplay scenes use multiple grid sizes:

- `Assets/Scenes/Gameplay/Scene_main.unity:9363` uses `m_CellSize: 1 x 1`.
- `Assets/Scenes/Gameplay/Scene_main.unity:10834` uses `m_CellSize: 4 x 1`.
- `Assets/Scenes/Gameplay/Scene_2.unity:27977` uses `m_CellSize: 4 x 1`.
- `Assets/Scenes/Gameplay/Scene_2.unity:31639` uses `m_CellSize: 0.25 x 0.25`.

Risk: jump distance, platform spacing, door placement, and camera framing become hard to reason about because "one grid cell" does not have one gameplay meaning.

## Transform Scale Outliers

Scene_2 has the largest scale outliers and should be treated as higher migration risk:

- `Assets/Scenes/Gameplay/Scene_2.unity:4106` uses `m_LocalScale: 68.85 x 0.405`.
- `Assets/Scenes/Gameplay/Scene_2.unity:4351` uses `m_LocalScale: 20 x 67`.
- `Assets/Scenes/Gameplay/Scene_2.unity:5408` uses `m_LocalScale: 30 x 16`.
- `Assets/Scenes/Gameplay/Scene_2.unity:6484` uses `m_LocalScale: 60 x 35`.
- `Assets/Scenes/Gameplay/Scene_2.unity:33525` uses `m_LocalScale: 70 x 26`.
- `Assets/Scenes/Gameplay/Scene_2.unity:35091` uses `m_LocalScale: 40 x 23`.
- `Assets/Scenes/Gameplay/Scene_2.unity:37207` uses `m_LocalScale: 43 x 26`.
- `Assets/Scenes/Gameplay/Scene_2.unity:38465` uses `m_LocalScale: 63 x 35`.

Scene_main has repeated moderate outliers:

- `Assets/Scenes/Gameplay/Scene_main.unity:422` uses `m_LocalScale: 10 x 10`.
- `Assets/Scenes/Gameplay/Scene_main.unity:6952` uses `m_LocalScale: 10.67 x 10.67`.
- `Assets/Scenes/Gameplay/Scene_main.unity:7442` uses `m_LocalScale: 10 x 10`.
- `Assets/Scenes/Gameplay/Scene_main.unity:9592` uses `m_LocalScale: 10.67 x 10.67`.
- `Assets/Scenes/Gameplay/Scene_main.unity:10034` uses `m_LocalScale: 10 x 10`.
- `Assets/Scenes/Gameplay/Scene_main.unity:10220` uses `m_LocalScale: 10 x 10`.
- `Assets/Scenes/Gameplay/Scene_main.unity:10345` uses `m_LocalScale: 10 x 10`.
- `Assets/Scenes/Gameplay/Scene_main.unity:11678` uses `m_LocalScale: 10.67 x 10.67`.
- `Assets/Scenes/Gameplay/Scene_main.unity:11769` uses `m_LocalScale: 10.67 x 10.67`.

Risk: changing these values directly may move art, colliders, triggers, camera bounds, or puzzle elements. These should only be inspected in Unity scene view before any normalization.

### Allowed Background Exceptions

The `Scene_main` background objects listed below are currently treated as allowed exceptions, not immediate cleanup targets:

- `Assets/Scenes/Gameplay/Scene_main.unity:422` uses `back_0` with `m_LocalScale: 10 x 10`.
- `Assets/Scenes/Gameplay/Scene_main.unity:7442` uses `middle_0` with `m_LocalScale: 10 x 10`.
- `Assets/Scenes/Gameplay/Scene_main.unity:10034` uses `near_0` with `m_LocalScale: 10 x 10`.
- `Assets/Scenes/Gameplay/Scene_main.unity:10220` uses another `back_0` with `m_LocalScale: 10 x 10`.
- `Assets/Scenes/Gameplay/Scene_main.unity:10345` uses another `middle_0` with `m_LocalScale: 10 x 10`.

Reason: these objects are background-only SpriteRenderers with `m_DrawMode: 0` (`Simple`). In Simple mode, changing `localScale` directly changes visual size. Preserving appearance while normalizing scale would require changing asset PPU or SpriteRenderer draw mode, both of which are visual-design decisions.

## Zero Scale Objects

Zero-scale scene objects exist:

- `Assets/Scenes/Gameplay/Scene_main.unity:8070` uses `m_LocalScale: 0 x 0`.
- `Assets/Scenes/Gameplay/Scene_2.unity:28379` uses `m_LocalScale: 0 x 0`.
- `Assets/Scenes/Gameplay/Scene_2.unity:38723` uses `m_LocalScale: 0 x 0`.

Risk: these may be intentionally hidden placeholders. Do not delete or rescale them without checking scene references.

## Camera Area Presets

Current CameraArea sizes are within the proposed preset bands, but they span close to wide:

- Scene_main uses `4.5` and `11`.
- Scene_2 uses `5`, `6`, `8`, `10`, `11`, `12`, and `13`.

Risk: the same player speed can feel different between rooms because the character occupies very different screen heights.

## Camera Pixel Snapping

`PixelPerfectFollowCamera` defaults to `pixelsPerUnit: 16`. Both gameplay scene camera instances are now aligned to that baseline:

- `Assets/Scenes/Gameplay/Scene_main.unity:6603` uses `pixelsPerUnit: 16`.
- `Assets/Scenes/Gameplay/Scene_2.unity:39489` uses `pixelsPerUnit: 16`.

Main character and ground assets are mostly `16 PPU`, while several backgrounds and props use `100 PPU`.

Result: the main gameplay cameras now use the same pixel-grid baseline as the primary character and ground assets. If later rooms show visible stepping, use this as a camera-feel tuning point instead of changing sprite PPU globally.

## PPU Exceptions

Many imported art assets use 100 PPU, while the main playable character and main tile assets use 16 PPU. Not all 100 PPU assets are wrong; portraits, UI, photos, and background references can be exceptions.

High-priority gameplay-adjacent exceptions to inspect before use in normalized rooms:

- `Assets/Art/Character/Girl_1/Walk.png.meta` uses 100 PPU.
- `Assets/Art/Character/Girl_1/Idle.png.meta` uses 100 PPU.
- `Assets/Art/Props/Platform.png.meta` uses 24 PPU.
- `Assets/Art/Props/Public Bus.png.meta` uses 6 PPU.
- Several `Assets/Art/Props/Office-Furniture-Pixel-Art` assets use 100 PPU.
- Several `Assets/Art/Props/Pixel Art Vending Machines Pack` assets use 100 PPU.

Risk: changing PPU on assets already used in scenes will resize every instance. Treat PPU fixes as migration work, not safe cleanup.

## Recommended Next Gate

The next step after this report is not a global fix. The next step is choosing one small test room and deciding whether to normalize it. That will affect gameplay layout, camera framing, and possibly puzzle readability, so it should require manual review before implementation.
