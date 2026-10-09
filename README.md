# Natural Backwalls Everywhere

An [Oxygen Not Included](https://www.klei.com/games/oxygen-not-included) mod that gives every biome the natural backwall tiles the Aquatic Planet Pack places behind its beach, reef, kelp forest, and abyss. Each biome gets a material chosen from that biome's own solids, and the coverage is adjustable.

Status: **work in progress**; generates on new worlds, defaults still being tuned.

## What it does

The Aquatic Planet Pack adds no special algorithm for backwalls. Its subworlds name a noise tree (`backwallNoise`), and its biome files carry a `<biome>_backwall` element band list that the noise samples, exactly like the ordinary terrain bands. The game only writes a backwall where the sampled element is a solid, so a Vacuum band means "no backwall there". This mod adds the same two pieces of data to every other subworld and biome on the per-world copy of the worldgen settings, right before the game computes the band thresholds.

- **Material and coverage per biome.** The options screen has one row per biome family (Sandstone, Forest, Tundra, Caustic, Marsh, Ocean, Oily, Rust, Magma, Barren, and the Spaced Out, Frosty Planet, and Prehistoric Planet biomes when those DLCs are active): a dropdown offering the solids that occur in that biome plus "None", and a text box with the fraction of cells that get a backwall. Default materials are the biome's filler rock, so digging a backwall never hands out a resource the biome does not already contain. The per-biome coverage applies when that biome is not the one the colony starts in; it defaults to 0.4, or 0.15 for the biomes whose zone defaults to the kelp forest pattern, which cover about the same share of cells. The pack's own reef and kelp forest use 0.2, but rock biomes hide most backwalls behind solid tiles, so they need more to look alike.
- **Pattern per zone.** A separate section picks, per subworld zone (Sandstone, Marsh, Magma, ...), the noise tree the patches follow; a tree belongs to a subworld, not a biome, and every subworld declares a zone. The mod ships copies of the pack's four trees (reef, kelp forest, abyss, beach) under its own names, so none of them needs the DLC, plus the base game's Strange tree. They differ a lot in density: for a 0.4 band the reef, abyss and beach trees cover about 45% of cells, the kelp forest about 78%, Strange about 80%; the kelp forest reaches the reef's 0.4 density at about 0.15, which is its default. Defaults: kelp forest for lush and marshy biomes (Caustic, Marsh, Swamp, Nectar, Garden, Wetlands), abyss for hot and rocky ones (Oily, Rust, Magma, Barren, Wasteland, Metallic, Radioactive), reef elsewhere; the pack's own biomes keep their trees unless changed. The dropdown also shows each tree's density.
- **Starting biome.** One separate coverage value applies to whatever biome the world's starting subworld uses, whichever it turns out to be. It defaults to 1, like the pack's fully walled beach.
- **Feature rooms get backwalls too.** Worldgen carves a subworld's features (the slime biome's TallRoom and BlobRoom caves, lakes, geodes) after claiming their cells, and the game's backwall pass only covers unclaimed cells, so those rooms stay bare in the Aquatic pack's own logic. Since most open space in a rock biome is feature rooms, that made backwalls far rarer in caves than the coverage suggested. The mod runs the same placement over feature cells (option "Backwalls behind carved caves and lakes", on by default), for the biomes it configures.
- **POIs and geysers keep their holes.** They are templates, and stamping a template overwrites the backwall under its footprint with the template's own (empty) backwall data. Fixing that would mean intercepting a sim message during the settle on global state, so the mod leaves it alone, as the Aquatic pack's own worlds do under their older templates.
- **Other planetoids (Spaced Out).** Off by default, like the Aquatic pack, whose other planetoids have no backwalls: only the asteroid the colony starts on gets them. The check reads the generator's public starting-world flag at the start of each world's generation and clears the mod's own change for the others; nothing else is touched.
- **Export worldgen overlay.** A button in the options writes the current settings as a mod-style folder of worldgen YAML, `NaturalBackwalls-overlay` next to your saves: each affected subworld file with its `backwallNoise` line, each affected biome file with its `_backwall` tables, and the shipped noise trees. External renderers that layer mod folders over the game's files can show the result, for example onimaxxing.com/worldgen ("Upload YAML"), which runs the game's worldgen in the browser and draws backwalls, or the preview tool below. Two things have no YAML form and are missing from such a render: the feature-room pass (feature caves show bare) and the starting-biome coverage (each biome's own coverage is used).
- **Surface stays open.** Space and surface subworlds are never touched, because a cell with a backwall no longer counts as open to space, which would break solar panels, rockets, and meteor showers.
- **New worlds only.** Backwalls exist only in world generation. Existing colonies are unchanged, and the mod can be added or removed without affecting a save beyond that.
- **Aquatic pack biomes** (Beach, Reef, Kelp Forest, Abyss) are listed too, with the pack's own values as defaults: Salt at full coverage, Coquina and Granite at 0.2, Basalt at 0.14. While a row stays at its defaults the pack's worldgen data is used untouched, so the beach's algae pockets keep their Dirt backwall; change the material or coverage and the whole biome follows your setting.

The noise tree is a copy of the pack's reef noise, shipped with the mod, so the Aquatic Planet Pack is not required.

## Other worldgen mods

The mod only adds data to the per-world copy of the worldgen settings and only for what it recognises, so other worldgen mods keep working:

- Every patch is guarded: if anything in the mod's pass fails, the pass is skipped with a warning in the log and worldgen continues exactly as the game and other mods set it up.
- Subworlds from other mods are handled like the game's: they are zoned, and the zone picks the pattern. A subworld that already carries a backwall noise tree, from the Aquatic pack or any mod, keeps it, and a biome that already has a `_backwall` table keeps that table.
- Biomes the mod does not know get a backwall of their own most common solid (from their band table, skipping neutronium and abyssalite) at the "Coverage in biomes from other mods" setting, or stay bare when that option is off. Custom elements work the same way, since the mod asks the element loader rather than carrying its own list.
- Mods that rewrite cluster generation in code, such as Cluster Generation Manager, run their own passes after the mod's; they may scale the mod's band tables along with everything else, which is harmless, and subworlds they add afterwards simply get no backwalls.

## Installing

As a local mod:

1. Download `NaturalBackwalls-<version>.zip` from the [latest release](https://github.com/isochronous/natural-backwalls-everywhere/releases/latest).
2. Extract it into a new folder named `NaturalBackwalls` inside the game's local mods folder, so that `mod.yaml` ends up directly inside it (create `local` if it does not exist):
   - Windows: `Documents\Klei\OxygenNotIncluded\mods\local\NaturalBackwalls`
   - Linux: `~/.config/unity3d/Klei/Oxygen Not Included/mods/local/NaturalBackwalls`
3. Enable it in the game's Mods menu and restart.

## Building

Requires the .NET SDK and a copy of the game. The build resolves the game folder from `-p:GameFolder=...`, the `ONI_GAME_FOLDER` environment variable, or the usual Steam locations, and deploys straight into the local mods folder:

```
git clone --recurse-submodules https://github.com/isochronous/natural-backwalls-everywhere.git
dotnet build natural-backwalls-everywhere/src/NaturalBackwalls -c Release
```

## Notes for modders

- The patch is a postfix on `SettingsCache.CloneInToNewWorld`, so it sees the subworlds and biomes of the world being generated, including those added by traits, and never modifies the global cache.
- A subworld is enabled only if at least one of its biomes resolves to a solid material. Biomes without a mapping get a Vacuum-only band list, so a subworld with backwall noise can never fall back to its terrain bands (which would produce gas and ore backwalls).
- Material lists live in `BiomeGroups.cs`, gathered from the worldgen yaml of build 744825; unknown or non-solid names are dropped at runtime.

## Previewing a biome without the game

`tools/BiomePreview` is a small .NET 8 Windows tool (a window when started without arguments, a command line otherwise) that runs the game's own noise trees and band tables (from `Assembly-CSharp-firstpass` and `LibNoiseDotNet`, loaded straight from the game folder) for one biome, and writes an HTML page with the foreground, the backwalls, an outline mode that shows backwalls through solid tiles, and the numbers: open cells, backwalls placed, backwalls visible. It skips everything above the biome level (overworld layout, POIs, borders, rivers, sim settle), so it is a rough preview of shape and density, in about a second.

```
dotnet build tools/BiomePreview -c Release                           # development build: finds the game through gamelibs.txt
pwsh tools/BiomePreview/publish.ps1                                   # self-contained zip for others, see tools/BiomePreview/README.md
tools/BiomePreview/bin/Release/BiomePreview.exe                       # the window: pick subworld, biome, material, coverage, seed; toggle the foreground and outlines
tools/BiomePreview/bin/Release/BiomePreview.exe --subworld subworlds/marsh/HotMarsh --coverage 0.4 --seed 1234 --out marsh.html
tools/BiomePreview/bin/Release/BiomePreview.exe --subworld dlc5::subworlds/kelpforest/KelpForestBasic --out kelp.html     # vanilla backwall band
```

`--noise` also takes the path of a noise tree yaml file, and the window has an "Add noise tree yaml..." button that puts such files in the pattern list, so a tree straight out of a noise-generation tool can be previewed; `--overlay <folder>` layers a whole mod-style worldgen folder over the game's files (several separated by `;`). `--biome` picks a biome key other than the subworld's heaviest, `--size WxH` (default 160x120), `--offset X,Y` moves the noise window, `--norm WxH` is the world-sized area the backwall noise is normalised over (default 256x384; the game normalises each tree over every cell that uses it). The mod logs a matching census per zone type in Player.log at every world load ("Backwall census"), so tool and game can be compared directly. Without `--coverage` the biome's vanilla `_backwall` table is used if it has one, else 0.4. The window lists biomes rather than subworlds, since only the biome bands and the noise setup affect the output; a second dropdown appears when a biome is generated under more than one noise setup.
