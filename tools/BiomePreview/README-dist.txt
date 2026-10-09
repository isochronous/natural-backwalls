BiomePreview - single-biome backwall preview for Oxygen Not Included
=====================================================================

Runs the game's own worldgen noise trees and biome band tables for one biome, outside the game, and
shows the foreground, the backwalls, and the numbers (open cells, backwalls placed, backwalls visible).
It skips everything above the biome level (overworld layout, POIs, borders, rivers, sim settle), so it is
a rough preview of shape and density, in about a second. Made for the Natural Backwalls Everywhere mod:
https://github.com/isochronous/natural-backwalls-everywhere

Requirements
------------
- Windows x64. Two downloads exist: BiomePreview-<version>.zip has the .NET runtime bundled and needs
  nothing installed; BiomePreview-<version>-net8.zip is much smaller and needs the .NET 8 Desktop
  Runtime (x64) from https://dotnet.microsoft.com/download/dotnet/8.0 installed first.
- Oxygen Not Included installed. The tool loads the game's own assemblies and worldgen files from your
  install; nothing of the game is included in this zip. It finds the game through Steam by itself. If it
  cannot, it asks for the game folder once (the one containing OxygenNotIncluded.exe) and remembers it.
  You can also set the environment variable ONI_GAME_FOLDER to that folder.

Using it
--------
Start BiomePreview.exe. Pick a biome (and a noise setup, when the biome has several), a coverage, a
backwall noise pattern and a seed; toggle the foreground and the outline mode that shows backwalls
through solid tiles. "Use the biome's vanilla backwall band" previews an Aquatic Planet Pack biome as the
game makes it.

Noise patterns
--------------
The pattern list holds the Natural Backwalls Everywhere patterns first, then every noise tree the game has (base
game and any DLC you own), then whatever is in the folder worldgen\noise next to the exe. The mod's
own trees ship in that folder. To preview your own tree, drop a yaml in the game's worldgen/noise
format into that folder, or use "Add noise tree yaml...", which copies the file there for you.

Command line
------------
BiomePreview --subworld subworlds/marsh/HotMarsh --coverage 0.4 --seed 1234 --out marsh.html
BiomePreview --subworld dlc5::subworlds/kelpforest/KelpForestBasic --out kelp.html
Run without arguments for the window; with --subworld and no --out it writes preview.html. Other
options: --biome, --size WxH, --offset X,Y, --norm WxH, --noise <tree or yaml path>, --overlay <mod
folder with worldgen files, layered over the game's; several separated by ;>.
