// SPDX-License-Identifier: MIT
// Part of the Geoland library: unofficial, fan-made mods for HavenCraft.
// HavenCraft is the work of Dionic Software; this project is not affiliated with them.
//
// PlotLookupPatch: the one place where the game is given a different answer.
//
// How it relates to the game: every question in play of the kind "which plot is this
// in?" ends in one function of the plot module, PlotModule.GetPlot. It is handed the
// full position, height included, then drops the height and reads its flat grid. Homes
// counting their beds, chests, workplaces and plot signs all ask through it; "which
// plot is this object in?" passes the object's centre.
//
// This patch runs after that function. If an upper level covers the position, the
// level's plot is the answer. Otherwise the game's own answer stands. Nothing else in
// the game is told about levels, and nothing else needs to be.
using HarmonyLib;
using UnityEngine;

namespace Geoland.PlotLevels;

[HarmonyPatch(typeof(PlotModule), nameof(PlotModule.GetPlot), new[] { typeof(Vector3) })]
internal static class PlotLookupPatch
{
    private static bool Prepare()
    {
        return GameAccess.CanKeepLevels;
    }

    private static void Postfix(PlotModule __instance, Vector3 worldPos, ref ushort __result)
    {
        if (LevelMap.TryLookup(__instance, worldPos, out ushort plot)) __result = plot;
    }
}
