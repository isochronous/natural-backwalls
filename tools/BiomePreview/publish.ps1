<#
.SYNOPSIS
  Build a self-contained BiomePreview zip for people who do not have the repo or .NET installed.

.DESCRIPTION
  Publishes a single-file, self-contained Windows x64 build to publish/win-x64, puts the mod's noise
  trees next to it (worldgen/noise, where the tool also picks up any yaml a user drops in), adds the
  README for the zip, and writes dist/BiomePreview-<version>.zip. The game's own assemblies are not
  included: the tool loads them from the user's install, which it finds by itself (GameLocator).

.PARAMETER Version
  Version string for the zip name. Defaults to today's date.
#>
[CmdletBinding()]
param(
    [string]$Version = (Get-Date -Format 'yyyy.MM.dd')
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$here = $PSScriptRoot
$out = Join-Path $here 'publish\win-x64'
$dist = Join-Path $here 'dist'

if (Test-Path $out) { Remove-Item -Recurse -Force $out }
dotnet publish (Join-Path $here 'BiomePreview.csproj') -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=none -o $out --nologo -v q
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

# The noise trees are copied by the project file; make sure they arrived, and drop anything a dev build left behind.
if (-not (Test-Path (Join-Path $out 'worldgen\noise\NaturalBackwallsReef.yaml'))) { throw "noise trees missing from $out" }
Remove-Item -Force (Join-Path $out 'gamelibs.txt') -ErrorAction SilentlyContinue
Copy-Item (Join-Path $here 'README-dist.txt') (Join-Path $out 'README.txt')

New-Item -ItemType Directory -Force $dist | Out-Null
$zip = Join-Path $dist "BiomePreview-$Version.zip"
if (Test-Path $zip) { Remove-Item -Force $zip }
Compress-Archive -Path (Join-Path $out '*') -DestinationPath $zip
Write-Host "Wrote $zip ($([math]::Round((Get-Item $zip).Length / 1MB, 1)) MB)"
