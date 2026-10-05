# Natural Backwalls

An [Oxygen Not Included](https://www.klei.com/games/oxygen-not-included) mod that gives every biome the natural backwall tiles the Aquatic Planet Pack places behind its beach, reef, kelp forest, and abyss. Each biome gets a material chosen from that biome's own solids, and the coverage is adjustable.

Status: **work in progress**, not yet tested in-game.

## What it does

The Aquatic Planet Pack adds no special algorithm for backwalls. Its subworlds name a noise tree (`backwallNoise`), and its biome files carry a `<biome>_backwall` element band list that the noise samples, exactly like the ordinary terrain bands. The game only writes a backwall where the sampled element is a solid, so a Vacuum band means "no backwall there". This mod adds the same two pieces of data to every other subworld and biome on the per-world copy of the worldgen settings, right before the game computes the band thresholds.

- **Material per biome.** The options screen lists one dropdown per biome family (Sandstone, Forest, Tundra, Caustic, Marsh, Ocean, Oily, Rust, Magma, Barren, and the Spaced Out, Frosty Planet, and Prehistoric Planet biomes when those DLCs are active). Each dropdown offers the solids that occur in that biome, plus "None". Defaults are the biome's filler rock, so digging a backwall never hands out a resource the biome does not already contain.
- **Coverage.** The fraction of cells that get a backwall. The default of 0.2 matches the Aquatic pack's reef and kelp forest. Starting biomes (Sandstone, Forest, Swamp, Garden) use a separate value that defaults to full coverage, like the pack's beach.
- **Surface stays open.** Space and surface subworlds are never touched, because a cell with a backwall no longer counts as open to space, which would break solar panels, rockets, and meteor showers.
- **New worlds only.** Backwalls exist only in world generation. Existing colonies are unchanged, and the mod can be added or removed without affecting a save beyond that.
- Biomes the Aquatic pack already configures are left alone.

The noise tree is a copy of the pack's reef noise, shipped with the mod, so the Aquatic Planet Pack is not required.

## Installing

As a local mod:

1. Download `NaturalBackwalls-<version>.zip` from the [latest release](https://github.com/isochronous/natural-backwalls/releases/latest).
2. Extract it into a new folder named `NaturalBackwalls` inside the game's local mods folder, so that `mod.yaml` ends up directly inside it (create `local` if it does not exist):
   - Windows: `Documents\Klei\OxygenNotIncluded\mods\local\NaturalBackwalls`
   - Linux: `~/.config/unity3d/Klei/Oxygen Not Included/mods/local/NaturalBackwalls`
3. Enable it in the game's Mods menu and restart.

## Building

Requires the .NET SDK and a copy of the game. The build resolves the game folder from `-p:GameFolder=...`, the `ONI_GAME_FOLDER` environment variable, or the usual Steam locations, and deploys straight into the local mods folder:

```
git clone --recurse-submodules https://github.com/isochronous/natural-backwalls.git
dotnet build natural-backwalls/src/NaturalBackwalls -c Release
```

## Notes for modders

- The patch is a postfix on `SettingsCache.CloneInToNewWorld`, so it sees the subworlds and biomes of the world being generated, including those added by traits, and never modifies the global cache.
- A subworld is enabled only if at least one of its biomes resolves to a solid material. Biomes without a mapping get a Vacuum-only band list, so a subworld with backwall noise can never fall back to its terrain bands (which would produce gas and ore backwalls).
- Material lists live in `BiomeGroups.cs`, gathered from the worldgen yaml of build 744825; unknown or non-solid names are dropped at runtime.
