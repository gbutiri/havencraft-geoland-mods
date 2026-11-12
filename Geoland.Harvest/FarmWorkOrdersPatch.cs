// SPDX-License-Identifier: MIT
// Part of the Geoland library: unofficial, fan-made mods for HavenCraft.
// HavenCraft is the work of Dionic Software; this project is not affiliated with them.
//
// FarmWorkOrdersPatch: lists the harvest work orders on the Farm plot.
//
// How it relates to the game: which work orders a kind of plot offers is a fixed list in
// the game's code, one list per kind of plot. This patch runs after the function that
// fills the list and, for the Farm, appends the harvest work orders in alphabetical
// order of their titles. The Farm was chosen because harvesting plants is what its
// workers already do; the other kinds of plot are left as they are.
using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace Geoland.Harvest;

[HarmonyPatch(typeof(WorkPlotUtil), nameof(WorkPlotUtil.GetWorkRecipesByPlotType))]
internal static class FarmWorkOrdersPatch
{
    // The game's internal name for the Farm kind of plot.
    private const string FarmPlotType = "farm";

    private static void Postfix(string plotType, RecipeModule recipeModule, List<WorkRecipe> workRecipes)
    {
        if (plotType != FarmPlotType) return;
        foreach (HarvestOrder order in HarvestCatalog.Orders.OrderBy(order => order.Title, StringComparer.Ordinal))
        {
            WorkRecipe recipe = recipeModule.TryGetRecipe(order.RecipeName);
            if (recipe != null && !workRecipes.Contains(recipe)) workRecipes.Add(recipe);
        }
    }
}
