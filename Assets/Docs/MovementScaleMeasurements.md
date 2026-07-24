# Movement Scale Measurements

This file converts current movement values into player-height ratios. It is a tuning reference only.

## Reference Body

- Current gameplay player body size: `1.6 x 4.5`.
- Current visible character height is about `4.5` to `4.7` Unity units.
- Use `4.5` units as the working player-height reference.

## Current Profiles

| Source | Move Speed | Jump Velocity | Gravity Scale | Dash Speed | Dash Duration | Dash Distance |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| `Player.prefab` | 9 | 15 | 3.2 | 19 | 0.16 | 3.04 |
| `Scene_main` Player | 9 | 15 | 3 | 19 | 0.16 | 3.04 |
| `Scene_2` Player2 | 9 | 16 | 3 | 19 | 0.16 | 3.04 |
| `Scene_2` Player | 9 | 17 | 3 | 19 | 0.16 | 3.04 |

## Player-Height Ratios

| Source | Body Heights / Second | Approx Jump Apex | Apex / Body Height | Dash / Body Height |
| --- | ---: | ---: | ---: | ---: |
| `Player.prefab` | 2.00 | 3.58 | 0.80 | 0.68 |
| `Scene_main` Player | 2.00 | 3.82 | 0.85 | 0.68 |
| `Scene_2` Player2 | 2.00 | 4.35 | 0.97 | 0.68 |
| `Scene_2` Player | 2.00 | 4.91 | 1.09 | 0.68 |

Approx jump apex uses `jumpVelocity^2 / (2 * 9.81 * gravityScale)`.

## Reading

- The tested movement profile has been applied to `Scene_main` and the base `Player.prefab`.
- Horizontal speed is now about `2.0` body heights per second across current gameplay players.
- `Scene_2` keeps higher jump velocities because existing platforms are designed around just-reachable jumps.
- Dash distance is now about `0.68` body heights.
- Dash trail feedback is created at runtime only for characters that currently have dash ability.
- Landing and dash now have lightweight visual scale feedback on the character visual only; physics, collider size, jump height, and dash distance are unchanged.
- `Player.prefab` still uses `gravityScale: 3.2`, while gameplay scenes use `gravityScale: 3`. This is intentionally left as a later review point.

## Likely Feel Impact

- `Scene_main` may feel heavy or underpowered, especially if the room asks for platforming.
- `Scene_2` may feel closer to action-platformer movement, but its two players still differ in jump and ability availability.
- The main issue is not only raw speed. Different rooms are currently tuned around different movement profiles.

## Applied Test Profile

The confirmed test profile is:

- `moveSpeed: 9`
- `jumpVelocity: 15`
- `gravityScale: 3`
- `dashSpeed: 19`
- `dashDuration: 0.16`
- keep `acceleration: 70`
- keep `deceleration: 85`

This profile sits between the old `Scene_main` and `Scene_2` values:

- speed is about `2.0` body heights per second
- jump apex is about `3.82` units, or `0.85` body heights
- dash distance is about `3.04` units, or `0.68` body heights

## Remaining Review Points

Further changes may still affect:

- `Player.prefab` gravity scale if changed from `3.2` to `3`
- `Scene_2` puzzle timing and platform reachability if jump velocity is lowered
- dash cooldown consistency, because `Scene_main` uses `0.55` and `Scene_2` uses `1`
- ability identity, because `Scene_2` still intentionally has one basic character and one powered character

These were left unchanged because they define level reachability or character identity.
