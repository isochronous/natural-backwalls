# BiomePreview

Single-biome backwall preview that runs the game's own noise trees and band tables outside the game. The
mod's main README describes what it shows; this file is about building and distributing it.

## Development build

```
dotnet build tools/BiomePreview -c Release
tools/BiomePreview/bin/Release/net8.0-windows/BiomePreview.exe
```

The build records the game folder from the common build props in `gamelibs.txt` next to the exe, and
copies the mod's noise trees from `src/NaturalBackwalls/worldgen/noise` to `worldgen/noise` next to the exe.

## Finding the game

At run time the tool looks for the game's `Managed` folder in this order: `gamelibs.txt` next to the
exe, the environment variable `ONI_GAME_FOLDER`, the folder remembered from an earlier run
(`%LocalAppData%\BiomePreview\gamefolder.txt`), every Steam library on the machine, and finally a
folder picker (window mode only), whose answer is remembered. See `GameLocator.cs`.

## Noise patterns

The pattern dropdown lists the mod's named patterns first and then every noise tree found: the game's
own, base game and each DLC folder present, the tool's `worldgen/noise` folder, and any overlay. A tree
the game already has keeps the game's version. Any yaml in the game's noise format dropped into the
tool's folder is listed on the next start; the "Add noise tree yaml..." button copies a file there.

## Distributing

```
pwsh tools/BiomePreview/publish.ps1 [-Version 1.2.3]
```

writes `tools/BiomePreview/dist/BiomePreview-<version>.zip`: a self-contained single-file Windows x64
build (no .NET install needed), the noise trees, and `README-dist.txt` as `README.txt`. The game's
assemblies are never included; the published exe finds the user's install by itself. `publish/` and
`dist/` are ignored by git.
