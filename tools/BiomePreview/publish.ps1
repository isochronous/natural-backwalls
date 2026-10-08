<#
.SYNOPSIS
  Build BiomePreview zips for people who do not have the repo.

.DESCRIPTION
  Publishes single-file Windows x64 builds, puts the mod's noise trees next to them (worldgen/noise,
  where the tool also picks up any yaml a user drops in), adds the README for the zip, and writes
  the zips to dist/:
    BiomePreview-<version>.zip         self-contained, nothing to install (about 65 MB)
    BiomePreview-<version>-net8.zip    needs the .NET 8 Desktop Runtime x64 (about 1 MB)
  The game's own assemblies are never included: the tool loads them from the user's install, which it
  finds by itself (GameLocator).

.PARAMETER Version
  Version string for the zip names. Defaults to today's date.
.PARAMETER Mode
  SelfContained, FrameworkDependent, or Both (default).
#>
[CmdletBinding()]
param(
    [string]$Version = (Get-Date -Format 'yyyy.MM.dd'),
    [ValidateSet('SelfContained', 'FrameworkDependent', 'Both')]
    [string]$Mode = 'Both'
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$here = $PSScriptRoot
$dist = Join-Path $here 'dist'
New-Item -ItemType Directory -Force $dist | Out-Null

function Publish-Variant([bool]$selfContained, [string]$suffix) {
    $out = Join-Path $here ('publish\win-x64' + $suffix)
    if (Test-Path $out) { Remove-Item -Recurse -Force $out }
    $sc = if ($selfContained) { 'true' } else { 'false' }
    dotnet publish (Join-Path $here 'BiomePreview.csproj') -c Release -r win-x64 --self-contained $sc `
        -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=none -o $out --nologo -v q
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

    # The noise trees are copied by the project file; make sure they arrived, and drop anything a dev build left behind.
    if (-not (Test-Path (Join-Path $out 'worldgen\noise\NaturalBackwallsReef.yaml'))) { throw "noise trees missing from $out" }
    Remove-Item -Force (Join-Path $out 'gamelibs.txt') -ErrorAction SilentlyContinue
    Copy-Item (Join-Path $here 'README-dist.txt') (Join-Path $out 'README.txt')

    $zip = Join-Path $dist "BiomePreview-$Version$suffix.zip"
    if (Test-Path $zip) { Remove-Item -Force $zip }
    Compress-Archive -Path (Join-Path $out '*') -DestinationPath $zip
    Write-Host "Wrote $zip ($([math]::Round((Get-Item $zip).Length / 1MB, 1)) MB)"
}

if ($Mode -ne 'FrameworkDependent') { Publish-Variant $true '' }
if ($Mode -ne 'SelfContained') { Publish-Variant $false '-net8' }
