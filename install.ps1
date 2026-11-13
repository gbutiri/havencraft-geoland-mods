# SPDX-License-Identifier: MIT
# Part of the Geoland library: unofficial, fan-made mods for HavenCraft.
# HavenCraft is the work of Dionic Software; this project is not affiliated with them.
#
# install.ps1: copies the Geoland mods into a HavenCraft game folder, or removes them.
#
#   install.ps1                       find the game through Steam and install
#   install.ps1 -GameDir "<folder>"   install into the folder that holds HavenCraft.exe
#   install.ps1 -DryRun               say what would be done, and change nothing
#   install.ps1 -Uninstall            remove the four Geoland mods again
#
# The only files this script ever writes or deletes are the four Geoland DLLs in the
# game's BepInEx\plugins folder (and that folder itself, if it does not exist yet).
# It does not install BepInEx, the mod loader: that has to be in place first.
#
# Works on Windows PowerShell 5.1 and later.

[CmdletBinding()]
param(
    [string]$GameDir,
    [switch]$DryRun,
    [switch]$Uninstall
)

$ErrorActionPreference = 'Stop'

$ModNames = @('Geoland.Planting', 'Geoland.Harvest', 'Geoland.PlotLevels', 'Geoland.RecruitWishes')
$GameExe = 'HavenCraft.exe'
$BepInExUrl = 'https://github.com/BepInEx/BepInEx/releases'
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

function Stop-WithMessage([string[]]$Lines) {
    Write-Host ''
    foreach ($line in $Lines) { Write-Host $line -ForegroundColor Yellow }
    Write-Host ''
    exit 1
}

# Every Steam library on this computer: Steam's own folder, plus the ones it lists in
# libraryfolders.vdf.
function Get-SteamLibraries {
    $libraries = @()
    $steamPath = $null
    try {
        $steamPath = (Get-ItemProperty -Path 'HKCU:\Software\Valve\Steam' -Name 'SteamPath' -ErrorAction Stop).SteamPath
    } catch {
        return $libraries
    }
    if ([string]::IsNullOrWhiteSpace($steamPath)) { return $libraries }
    $steamPath = $steamPath -replace '/', '\'
    $libraries += $steamPath

    foreach ($vdf in @((Join-Path $steamPath 'steamapps\libraryfolders.vdf'), (Join-Path $steamPath 'config\libraryfolders.vdf'))) {
        if (-not (Test-Path -LiteralPath $vdf -PathType Leaf)) { continue }
        foreach ($line in (Get-Content -LiteralPath $vdf)) {
            if ($line -match '^\s*"path"\s+"([^"]+)"') {
                $libraries += ($Matches[1] -replace '\\\\', '\')
            }
        }
    }
    return $libraries
}

# Folders named HavenCraft* under steamapps\common in any Steam library, each listed once.
function Find-GameFolders {
    $found = @()
    $seen = @{}
    foreach ($library in (Get-SteamLibraries)) {
        $common = Join-Path $library 'steamapps\common'
        if (-not (Test-Path -LiteralPath $common -PathType Container)) { continue }
        foreach ($dir in (Get-ChildItem -LiteralPath $common -Directory -Filter 'HavenCraft*' -ErrorAction SilentlyContinue)) {
            $key = $dir.FullName.TrimEnd('\').ToLowerInvariant()
            if ($seen.ContainsKey($key)) { continue }
            $seen[$key] = $true
            $found += $dir.FullName
        }
    }
    return $found
}

# Where a mod's DLL is, next to this script: the release zip's layout first, then a
# source checkout's. Returns $null when it is in neither place.
function Find-ModFile([string]$Name) {
    $inZip = Join-Path $ScriptDir ("BepInEx\plugins\" + $Name + ".dll")
    if (Test-Path -LiteralPath $inZip -PathType Leaf) { return $inZip }
    $inSource = Join-Path $ScriptDir ($Name + "\build\" + $Name + ".dll")
    if (Test-Path -LiteralPath $inSource -PathType Leaf) { return $inSource }
    return $null
}

Write-Host ''
if ($Uninstall) { Write-Host 'Geoland library: uninstall' } else { Write-Host 'Geoland library: install' }
if ($DryRun) { Write-Host '(dry run: nothing will be changed)' }
Write-Host ''

# 1. Which game folder.
if (-not [string]::IsNullOrWhiteSpace($GameDir)) {
    $GameDir = $GameDir.Trim().Trim('"').TrimEnd('\')
    if (-not (Test-Path -LiteralPath $GameDir -PathType Container)) {
        Stop-WithMessage @("This folder does not exist: $GameDir")
    }
    $GameDir = (Resolve-Path -LiteralPath $GameDir).ProviderPath
} else {
    $candidates = @(Find-GameFolders)
    if ($candidates.Count -eq 0) {
        Stop-WithMessage @(
            'Could not find HavenCraft in a Steam library.',
            'Run this again and say where the game is (the folder that holds HavenCraft.exe):',
            '',
            '  install.cmd -GameDir "C:\path\to\HavenCraft"'
        )
    }
    if ($candidates.Count -gt 1) {
        $lines = @('Found more than one HavenCraft folder:', '')
        foreach ($candidate in $candidates) { $lines += "  $candidate" }
        $lines += ''
        $lines += 'Run this again and say which one, for example:'
        $lines += ''
        $lines += ('  install.cmd -GameDir "' + $candidates[0] + '"')
        Stop-WithMessage $lines
    }
    $GameDir = $candidates[0]
}
Write-Host "Game folder: $GameDir"

# 2. Is it the game, and is it closed.
if (-not (Test-Path -LiteralPath (Join-Path $GameDir $GameExe) -PathType Leaf)) {
    Stop-WithMessage @(
        "$GameExe is not in that folder, so it is not a HavenCraft game folder.",
        'Pass the folder that holds HavenCraft.exe with -GameDir "<folder>".'
    )
}
if (@(Get-Process -Name 'HavenCraft' -ErrorAction SilentlyContinue).Count -gt 0) {
    Stop-WithMessage @('HavenCraft is running. Close the game, then run this again.')
}

$pluginsDir = Join-Path $GameDir 'BepInEx\plugins'

# 3. Uninstall: remove the four Geoland DLLs and nothing else.
if ($Uninstall) {
    $removed = 0
    foreach ($name in $ModNames) {
        $target = Join-Path $pluginsDir ($name + '.dll')
        if (-not (Test-Path -LiteralPath $target -PathType Leaf)) {
            Write-Host "  not installed:  $name.dll"
            continue
        }
        if ($DryRun) {
            Write-Host "  would remove:   $target"
        } else {
            Remove-Item -LiteralPath $target -Force
            Write-Host "  removed:        $target"
        }
        $removed++
    }
    Write-Host ''
    if ($DryRun) {
        Write-Host "Dry run: $removed file(s) would be removed. Nothing was changed."
    } else {
        Write-Host "Done: $removed file(s) removed."
    }
    Write-Host 'BepInEx, the mod loader, was left in place. The README says how to remove it too.'
    Write-Host 'Settings files in BepInEx\config were left in place.'
    Write-Host 'Saves written while the mod loader was installed stay marked as modded.'
    Write-Host ''
    exit 0
}

# 4. Install: the mod loader has to be there first.
if (-not (Test-Path -LiteralPath (Join-Path $GameDir 'BepInEx\core') -PathType Container)) {
    Stop-WithMessage @(
        'BepInEx, the mod loader, is not installed in that folder (no BepInEx\core).',
        'Install BepInEx 5 first:',
        "  1. Download the Windows x64 build of BepInEx 5 from $BepInExUrl",
        "  2. Unzip it into the game folder, so that winhttp.dll sits next to $GameExe.",
        '  3. Start the game once and quit, so BepInEx creates its folders.',
        'Then run this again.'
    )
}

# 5. Which DLLs are next to this script.
$toCopy = @()
$missing = @()
foreach ($name in $ModNames) {
    $source = Find-ModFile $name
    if ($null -eq $source) { $missing += $name } else { $toCopy += $source }
}
if ($toCopy.Count -eq 0) {
    Stop-WithMessage @(
        'Found none of the Geoland DLLs next to this script.',
        'They are expected in BepInEx\plugins\ beside install.ps1 (the release zip),',
        'or in Geoland.*\build\ (a source checkout, after building).'
    )
}

# 6. Copy.
if (-not (Test-Path -LiteralPath $pluginsDir -PathType Container)) {
    if ($DryRun) {
        Write-Host "  would create:   $pluginsDir"
    } else {
        New-Item -ItemType Directory -Path $pluginsDir | Out-Null
        Write-Host "  created:        $pluginsDir"
    }
}
$copied = 0
foreach ($source in $toCopy) {
    $target = Join-Path $pluginsDir (Split-Path -Leaf $source)
    if ([string]::Equals($source, $target, [System.StringComparison]::OrdinalIgnoreCase)) {
        Write-Host "  already there:  $target"
        continue
    }
    if ($DryRun) {
        Write-Host "  would copy:     $source"
        Write-Host "            to:   $target"
    } else {
        Copy-Item -LiteralPath $source -Destination $target -Force
        Write-Host "  copied:         $target"
    }
    $copied++
}
foreach ($name in $missing) {
    Write-Host "  not found, skipped: $name.dll" -ForegroundColor Yellow
}

Write-Host ''
if ($DryRun) {
    Write-Host "Dry run: $copied file(s) would be copied. Nothing was changed."
} else {
    Write-Host "Done: $copied file(s) copied."
}
Write-Host ''
Write-Host 'Remember:'
Write-Host '  - With the mod loader installed, the game marks every save it writes as modded.'
Write-Host '    That mark is permanent. Keep a backup of your saves from before.'
Write-Host '  - Please do not send the developer bug reports from a modded game.'
Write-Host '  - The log is BepInEx\LogOutput.log in the game folder.'
Write-Host ''
exit 0
