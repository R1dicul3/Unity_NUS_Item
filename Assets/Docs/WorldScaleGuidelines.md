# World Scale Guidelines

This document defines the safe baseline for future level, asset, and movement work. It does not require changing existing scenes immediately.

## Baseline

- Main pixel-art gameplay assets should use 16 PPU unless there is a deliberate exception.
- The current main character is about 4.5 Unity units tall.
- Character collision should stay close to the visible body height. For the current character, `1.6 x 4.5` is a reasonable existing reference.
- One gameplay room should use one primary grid scale. Avoid mixing `1 x 1`, `4 x 1`, and `0.25 x 0.25` grids in the same playable space unless the reason is documented.
- Camera sizes should be chosen from a small set of readable presets instead of being tuned freely per trigger.

## Suggested Camera Presets

- Close: `4.5` to `6`
- Standard: `8` to `10`
- Wide: `11` to `13`

Use close cameras for interaction-heavy rooms, standard cameras for platforming, and wide cameras only when the room layout or puzzle needs extra context.

## Asset Import Rules

- Use 16 PPU for new character, tile, and pixel environment assets.
- If an imported asset uses 24 PPU, 100 PPU, or another value, treat it as an exception and document why.
- Prefer fixing PPU at import time over compensating with extreme Transform scale values in scenes.

## Scene Layout Rules

- Prefer Tilemap/Grid layout for walkable surfaces.
- Avoid using extreme Transform scale values such as `70 x 26` or `68.85 x 0.405` for gameplay-critical objects.
- If a stretched object is only decorative background, keep its collider disabled or separate from gameplay collision.
- Keep trigger, door, camera-area, and puzzle object dimensions aligned with the same room scale.

## Player Movement Tuning Frame

Do not tune movement in isolation. Tune these as one group:

- player visible height
- player collider size
- ground check offset and size
- platform height and gap distance
- jump velocity and gravity scale
- dash speed and duration
- camera size

The useful reference is not raw units alone, but player-height ratios. For example, jump apex, dash distance, platform gaps, and ledge height should be described as fractions or multiples of the player height.

## Safe Migration Plan

1. Keep existing scenes unchanged.
2. Add audit warnings for PPU, Grid, CameraArea, and Transform scale outliers.
3. Pick one test room and normalize only that room.
4. Retune movement against that room.
5. Migrate older rooms one at a time after the test room feels correct.

