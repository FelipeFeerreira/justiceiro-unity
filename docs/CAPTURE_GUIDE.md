# Capture guide — screenshots and GIF

This repository has no image yet, and that is the single biggest gap in how it
reads as a portfolio piece. A reviewer spends seconds here; one good frame does
more than three paragraphs.

## Setup

1. Open the project in the editor version recorded in
   `ProjectSettings/ProjectVersion.txt`.
2. Run **Justiceiro → Abrir Fase** (or **Reconstruir Fase** on a fresh clone),
   then enter Play mode.
3. Set the Game view to **16:9**, 1600×900, and disable gizmos.
4. Use a clean HUD state: full health, a weapon equipped, enemies visible.

## Required shots

| File | Frame |
| --- | --- |
| `docs/screenshots/01-arena.png` | Wide view of the hillside arena with enemies approaching and the weapon in frame |
| `docs/screenshots/02-combat.png` | Mid-combat: projectiles, area damage or a boss telegraph visible |
| `docs/screenshots/03-hud.png` | Close view of the HUD: health, ammo, weapon slots, key-gated objective |
| `docs/screenshots/04-menu.png` | Main menu built by the editor automation |

## GIF (10–12 s)

- Record a single loop: dash into the arena → two weapon swaps → kill an enemy.
- 1280×720, 15 fps, under 5 MB. ScreenToGif on Windows or Unity Recorder both work.
- Save as `docs/screenshots/gameplay.gif`.

## Paste into the README

Place this right after the intro paragraph, replacing the placeholder path:

```markdown
![Justiceiro gameplay](docs/screenshots/gameplay.gif)

| Arena | Combat | HUD | Main menu |
| --- | --- | --- | --- |
| ![](docs/screenshots/01-arena.png) | ![](docs/screenshots/02-combat.png) | ![](docs/screenshots/03-hud.png) | ![](docs/screenshots/04-menu.png) |
```

## Honesty rule

If a frame comes from a specific editor version and a specific commit, say so in
the caption ("Captured in Unity 6000.6.0f1, commit `abc1234`"). Do not retouch the
HUD or add effects that are not in the build.
