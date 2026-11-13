# SPDX-License-Identifier: MIT
# Part of the Geoland library: unofficial, fan-made mods for HavenCraft.
# HavenCraft is the work of Dionic Software; this project is not affiliated with them.
#
# package.ps1: makes the release zip, dist\Geoland-library-<version>.zip, from what
# is already built. It does not compile anything: build the mods first.
#
# Inside the zip:
#   BepInEx\plugins\Geoland.*.dll    the mods, laid out as in the game folder
#   README.md, LICENSE, NOTICE, VERSION
#   install.cmd, install.ps1         the installer
#   CHANGELOG-<mod>.md               each mod's history
#   docs\How-It-Works.md             how the game is put together and how the mods hook in
#
# Works on Windows PowerShell 5.1 and later.

[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$ModNames = @('Geoland.Planting', 'Geoland.Harvest', 'Geoland.PlotLevels', 'Geoland.RecruitWishes', 'Geoland.Mines')
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path

# Each entry: the file on disk, and its path inside the zip.
$entries = @()
$missing = @()

function Add-Entry([string]$RelativeSource, [string]$PathInZip) {
    $source = Join-Path $Root $RelativeSource
    if (Test-Path -LiteralPath $source -PathType Leaf) {
        $script:entries += [pscustomobject]@{ Source = $source; PathInZip = $PathInZip }
    } else {
        $script:missing += $RelativeSource
    }
}

foreach ($name in $ModNames) {
    Add-Entry ($name + '\build\' + $name + '.dll') ('BepInEx/plugins/' + $name + '.dll')
}
foreach ($file in @('README.md', 'LICENSE', 'NOTICE', 'VERSION', 'install.cmd', 'install.ps1')) {
    Add-Entry $file $file
}
foreach ($name in $ModNames) {
    Add-Entry ($name + '\CHANGELOG.md') ('CHANGELOG-' + $name + '.md')
}
Add-Entry 'docs\How-It-Works.md' 'docs/How-It-Works.md'

if ($missing.Count -gt 0) {
    Write-Host ''
    Write-Host 'Cannot make the package. These files are missing:' -ForegroundColor Yellow
    foreach ($file in $missing) { Write-Host "  $file" -ForegroundColor Yellow }
    Write-Host 'Build the mods first (see "Building from source" in README.md), then run this again.' -ForegroundColor Yellow
    Write-Host ''
    exit 1
}

$distDir = Join-Path $Root 'dist'
if (-not (Test-Path -LiteralPath $distDir -PathType Container)) {
    New-Item -ItemType Directory -Path $distDir | Out-Null
}
# The library has a version of its own, kept in the VERSION file. Each mod inside keeps
# its own version too; the library's number names the set that was released together.
$versionFile = Join-Path $Root 'VERSION'
if (-not (Test-Path -LiteralPath $versionFile -PathType Leaf)) {
    Write-Host 'No VERSION file found next to package.ps1.'
    exit 1
}
$version = (Get-Content -LiteralPath $versionFile -TotalCount 1).Trim()
if ($version -notmatch '^\d+\.\d+\.\d+$') {
    Write-Host "The VERSION file must hold a version like 0.1.0, not '$version'."
    exit 1
}
$zipPath = Join-Path $distDir ('Geoland-library-' + $version + '.zip')
if (Test-Path -LiteralPath $zipPath) { Remove-Item -LiteralPath $zipPath -Force }

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

# Entries are written one by one, with forward slashes in their names, so the zip opens
# the same way in every unzip program.
$zip = [System.IO.Compression.ZipFile]::Open($zipPath, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($entry in $entries) {
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
            $zip, $entry.Source, $entry.PathInZip, [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
        Write-Host "  added: $($entry.PathInZip)"
    }
} finally {
    $zip.Dispose()
}

Write-Host ''
Write-Host "Package written: $zipPath"
Write-Host ''
exit 0
