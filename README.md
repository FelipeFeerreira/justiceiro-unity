# Justiceiro — Arena Shooter Prototype

A fast-paced first-person shooter prototype set in a fictional hillside environment. Level geometry, visual assets and sound effects are generated through code.

**Stack:** Unity 6000.6.0f1 · C# · editor automation

## Implemented systems

- Double-jump, sprint and rechargeable dashes.\n- Four weapon types, projectiles and area damage.\n- Enemy archetypes, arena waves, key-gated progression and a boss.\n- Procedural meshes and textures, with synthesized runtime sound effects.\n- Editor automation for reconstructing the level and main menu.

## Open the project

1. Install the editor version recorded in `ProjectSettings/ProjectVersion.txt` through Unity Hub.
2. Add this repository as a project and let Unity restore the packages in `Packages/manifest.json`.
3. Use **Justiceiro → Abrir Fase** in the editor if you need to open or generate the demo scene.
4. Enter Play mode.

If the level is missing, use **Justiceiro → Reconstruir Fase**. Controls: WASD move, Shift sprint, Space jump, Left Ctrl dash, mouse buttons attack, 1–4 switch weapons, E finisher, Esc pause and R restart.

Scene-building commands generate or replace scene content; use them on a fresh clone before customizing the levels.

## Code structure

Assets/Scripts contains Player, Weapons, Enemies, Level, Core and UI. Assets/Editor contains procedural art and level/menu builders.

The repository includes Assets, Packages and ProjectSettings. Unity caches, local editor state and compiled builds are excluded; let the editor regenerate them.

## Project status

This is a development prototype and code portfolio, not a released game. The systems above were identified in the source; a fresh editor import, playthrough and platform build have not been verified during this publication pass. See [verification notes](docs/PORTFOLIO_VALIDATION.md). No performance or release-readiness claims are made.
