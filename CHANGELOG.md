# Changelog

## Unreleased

- Added a preview image, so the mod shows one in the game's mod list.

## 0.1.0 - 2026-10-09

- First version: natural backwall tiles in every biome, built from the Aquatic Planet Pack's own worldgen data (its backwall noise plus a per-biome element band). New worlds only.
- One options row per biome family: a material dropdown offering that biome's solids, and a coverage box (default 0.4, or 0.15 for the denser kelp forest pattern). The Aquatic Planet Pack's own biomes keep their vanilla values as defaults.
- A Patterns section with a dropdown per subworld zone: the pack's reef, kelp forest, abyss, and beach patterns ship with the mod, plus the base game's Strange.
- One starting-biome coverage (default 1) that applies to whatever biome the colony starts in.
- Backwalls on Spaced Out's other planetoids are optional and off by default.
- Feature rooms (carved caves, lakes, geodes), which the game's own pass leaves bare, get backwalls too; optional, on by default.
- Biomes from other worldgen mods get their most common solid (optional). Every patch is guarded, so a failure leaves worldgen untouched. Surface and space subworlds stay open.
- A per-zone backwall census is logged at every world load, and an options button exports the settings as a worldgen YAML overlay for external renderers.
