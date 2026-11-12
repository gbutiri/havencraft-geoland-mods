// SPDX-License-Identifier: MIT
// Part of the Geoland library: unofficial, fan-made mods for HavenCraft.
// HavenCraft is the work of Dionic Software; this project is not affiliated with them.
//
// AnnounceModPatch: tells the game, by name and version, that this plugin is running.
//
// How it relates to the game: the game keeps a list of the mods in use. It puts that
// list in its error reports, next to whether an unofficial mod loader was detected,
// and writes it into the description of every save. In this version of the game the
// list is always empty, because the game has no way yet of loading a mod itself. An
// error report from a modded game therefore says that unofficial mods were detected
// but not which.
//
// This patch runs after the function that returns the list and adds this plugin to
// it, so the developer can see exactly what was installed. It was asked for by the
// developer: mods must be known to the game's mod detection. Nothing here hides
// anything from that detection or changes what it finds.
using System.Collections.Generic;
using HarmonyLib;

namespace Geoland.Shared;

[HarmonyPatch(typeof(ModModule), nameof(ModModule.GetMods))]
internal static class AnnounceModPatch
{
    private static void Postfix(HashSet<string> __result)
    {
        if (__result != null && GameHook.PluginLabel != null) __result.Add(GameHook.PluginLabel);
    }
}
