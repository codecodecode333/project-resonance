# Project Goal

Build a small mobile tactical roguelike that will actually be released.

Core gameplay:
- Grid-based tactical combat
- Height and LOS
- Enemy Intent
- Run-based character builds
- Short mobile-friendly encounters

# Architecture

- Game rules must not depend on UI, Sprite, Animation, VFX or input.
- ScriptableObjects contain static definitions only.
- Mutable runtime state must use runtime objects.
- Keep Unit state separate from Unit presentation.
- Keep Grid logic separate from Tilemap presentation.
- Do not recreate a monolithic BattleController.

# Legacy

- LegacyReference is read-only.
- Never modify files inside LegacyReference.
- Do not copy legacy classes directly into Assets without explicit approval.
- Reuse algorithms and rules selectively.

# Scope

- Make small feature-scoped changes.
- Do not add unrelated systems.
- Do not perform broad refactors unless explicitly requested.
- Do not maximize test count.
- Test important rules, boundaries and failure paths.

# Priority

1. Fun combat
2. Complete game loop
3. Mobile usability
4. Release
5. Architecture refinement

Shipping the game is more important than architectural complexity.

# Handoff

- Use `PROJECT_STATUS.md` as the ChatGPT ↔ Codex handoff file.
- At the end of every Codex task, update it briefly with completed work, verification, current commit state, and the next milestone.
- Keep the workflow: ChatGPT design confirmation → Codex implementation → Codex testing → Git commit → ChatGPT review of that commit.
