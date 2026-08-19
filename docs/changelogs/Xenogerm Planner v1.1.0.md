# Xenogerm Planner v1.1.0

## Added

- Added plan creation from existing xenotypes.
  - You can create a plan from any premade xenotype available in your game, including xenotypes added by mods.
  - You can also create a plan from your saved custom xenotypes.
- Added a shared source picker for creating plans from xenogerm templates and xenotypes.
- Added separate collapsible `Premade xenotypes` and `Saved xenotypes` groups when choosing a xenotype.
- Added localized source-selection UI for English, Russian, and Ukrainian.
- Added trader advisory notifications.
  - Xenogerm Planner can now notify you when an orbital trader or visiting trader caravan offers a genepack that may help complete one of your plans.
  - Trader offers follow the selected planning mode, including the stricter `Exact gene set` mode.
  - Genepacks offered by traders do not count toward plan readiness until you actually acquire them.
  - Multiple useful offers from the same trader are grouped into a single notification instead of producing separate messages.
  - Offers that were already available when loading a save or switching to a map do not generate a burst of old notifications.
  - Notifications for visiting caravans can jump directly to the trader pawn. Orbital trader notifications remain text-only.
  - Trader advisory notifications are available in English, Russian, and Ukrainian.

## Changed

- Unified new-plan creation under a single `Create` action with three options:
  - `From scratch`
  - `From xenogerm template`
  - `From xenotype`
- Creating a plan from a xenogerm template now opens the Plan Editor instead of creating the plan immediately.
- Plans created from xenogerm templates or xenotypes can now be reviewed and edited before saving.
- Planning mode and readiness notifications for these plans can now be configured in the Plan Editor like any other new plan.
- New plans created from a template or xenotype start with the normal defaults: `All planned genes` mode and readiness notifications enabled.
- The Planner now checks that a selected xenogerm template is still available before opening it in the Plan Editor.
- Saved custom xenotypes are loaded through RimWorld's normal xenotype-loading system and are handled safely if a saved xenotype is missing, invalid, empty, or cannot be loaded.
- Saved xenotype information is refreshed automatically when its source file changes.
- Vanilla and modded premade xenotypes are shown together under `Premade xenotypes`.