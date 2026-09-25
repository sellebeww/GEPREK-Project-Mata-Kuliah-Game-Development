# Gameplay and menu polish — 24 September 2026

## Changes

- Fryers reject collection until chicken is ready or burnt, including collection by staff.
- The mortar preserves incoming chicken quality through preparation and interrupted holds.
- Interaction selection uses facing as a small preference and keeps the current target stable.
- Target changes notify the HUD even when both stations show identical hints.
- Disabled or destroyed targets are ignored safely; disabling the interactor releases its hold.
- The main menu uses generated warung artwork, a clearer button panel, control hints, and confirmation before replacing an existing save.

The illustration loads from Resources at runtime. Rebuilding the scene with GeprekBuilder is not required.

## Validation

- Compiled all gameplay scripts and Editor scripts with the Unity installation's C# compiler and the project's assembly references.
- Imported an isolated copy of Assets, Packages, and ProjectSettings in Unity 6000.5.9f1.
- Executed the nine regression cases in GeprekGameplayTests via a temporary Unity batch harness with NUnit assertions. All passed.
- Loaded the generated artwork and rendered the actual menu UI at 1280 × 720. Inspected the result for clipping and overlaps.
- Preview: `Screenshots/menu_polished.png`.
- Generation tool and exact prompt: `Docs/GENERATED_ART.md`.

This validates the changed mechanics and menu rendering. It is not a complete playthrough of all 15 days or a mobile-device test.

## Regression checks

The cases are in `Assets/_Project/Editor/GeprekGameplayTests.cs`. They require an empty Edit Mode scene so they cannot affect an active game session. To run them manually, save your scene, open an empty scene, and use Window → General → Test Runner → EditMode, filtering for GeprekGameplayTests.

```text
PASS Cannot collect cooking chicken
PASS Can collect cooked chicken
PASS Can collect burnt chicken
PASS Prep preserves quality 0.45
PASS Prep preserves quality 0.82
PASS Prep preserves quality 1
PASS Same-hint targets notify UI
PASS Facing and disabled targets
PASS Destroyed targets release safely
PASS Menu artwork loads and preview rendered
```
