// SPDX-License-Identifier: MIT
// Part of the Geoland library: unofficial, fan-made mods for HavenCraft.
// HavenCraft is the work of Dionic Software; this project is not affiliated with them.
//
// AnnounceModPatch: tells the game, by name and version, that this plugin is running.
//
// How it relates to the game: the game detects third-party mod loaders. When it does,
// it marks every save written afterwards as modded and says so in its error reports.
// It also keeps a list of the mods in use, which goes into its error reports and into
// the description of every save.
//
// This patch runs after the function that returns the list and adds this plugin's
// name and version to it. That is the author's own choice, made so that anyone
// reading an error report or a save's description can see which mods were in use.
// Nothing here prevents, hides or undoes the game's detection or its marks, and
// nothing built on this may.
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
