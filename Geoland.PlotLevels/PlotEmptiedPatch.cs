// SPDX-License-Identifier: MIT
// Part of the Geoland library: unofficial, fan-made mods for HavenCraft.
// HavenCraft is the work of Dionic Software; this project is not affiliated with them.
//
// PlotEmptiedPatch: when a plot loses its last ground cell, its upper levels go first.
//
// How it relates to the game: a plot that is deleted, or erased down to nothing, is
// dealt with in two steps. First the game announces "plot shapes changed" for it, and
// everything standing in it is filed under whatever plot is there now. Only then is the
// plot destroyed. Its upper levels have to be gone before that announcement. Otherwise
// an upstairs bed would still be answered for by the dying plot, would not be re-filed,
// and would stay listed under a plot number that no longer exists, which the next new
// plot may be given. So this patch runs just before the announcement and drops the
// levels of every plot in it that has no ground cells left.
using System;
using System.Collections.Generic;
using Geoland.Shared;
using HarmonyLib;

namespace Geoland.PlotLevels;

[HarmonyPatch(typeof(PlotModule), nameof(PlotModule.InvokeOnPlotsShapeChanged))]
internal static class PlotEmptiedPatch
{
    private static bool Prepare()
    {
        return GameAccess.CanKeepLevels;
    }

    // The parameter name is the game's; that is how the patch is handed it.
    private static void Prefix(List<PlotInfo> changedPlotInfos)
    {
        if (changedPlotInfos == null || LevelMap.CellCount == 0) return;
        try
        {
            foreach (PlotInfo plot in changedPlotInfos)
            {
                if (plot == null || plot.id == PlotModule.wildernessPlotID || plot.GetSize() > 0) continue;

                int dropped = LevelMap.DropLevelsOf(plot.id);
                if (dropped > 0)
                {
                    GameHook.Log.LogInfo("Plot " + plot.id + " has no ground cells left: removed its " + dropped + " upper levels");
                }
            }
        }
        catch (Exception e)
        {
            GameHook.Log.LogError("Could not remove the upper levels of a deleted plot: " + e);
        }
    }
}
