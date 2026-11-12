// SPDX-License-Identifier: MIT
// Part of the Geoland library: unofficial, fan-made mods for HavenCraft.
// HavenCraft is the work of Dionic Software; this project is not affiliated with them.
//
// SavedNamesPatch: keeps a farm's work orders when a save made with an earlier version
// of the plugin is loaded.
//
// How it relates to the game: a save file lists each plot's work orders by the internal
// name of their recipe, with the amount and mode the player set. When a save is loaded,
// an order whose recipe the game does not know is dropped, and a plot whose orders then
// no longer match its kind has all of them reset. Up to version 0.8.0 this plugin's
// recipes had different internal names. This patch runs before the game takes in the
// order books read from the save and replaces each old name with the current one, so
// the orders are found and the player's settings are kept. The next save holds the
// current names.
using System;
using System.Collections.Generic;
using System.Reflection;
using Geoland.Shared;
using HarmonyLib;

namespace Geoland.Harvest;

[HarmonyPatch]
internal static class SavedNamesPatch
{
    private static bool Prepare()
    {
        return GameAccess.Available && GameAccess.CanTranslateSavedNames;
    }

    private static MethodBase TargetMethod()
    {
        return GameAccess.OrderBooksLoadFromSave;
    }

    // The parameter's class is not public, so it arrives here as a plain object.
    private static void Prefix(object serial)
    {
        try
        {
            int renamed = 0;
            foreach (OrderBookSaveManager.OrderBookSerializable orderBook in GameAccess.SavedOrderBooks(serial))
            {
                List<OrderBookSaveManager.WorkOrderSerializable> orders = orderBook.workOrders;
                if (orders == null) continue;

                for (int i = 0; i < orders.Count; i++)
                {
                    string item = HarvestNames.ItemOfOldRecipe(orders[i].workRecipeName);
                    if (item == null) continue;

                    // The saved order is a value, so it is changed and put back.
                    OrderBookSaveManager.WorkOrderSerializable order = orders[i];
                    order.workRecipeName = HarvestNames.RecipeFor(item);
                    orders[i] = order;
                    renamed++;
                }
            }
            if (renamed > 0)
            {
                GameHook.Log.LogInfo("save from an earlier version: " + renamed + " harvest work orders given their current names");
            }
        }
        catch (Exception e)
        {
            GameHook.Log.LogError("Could not translate old work order names in this save; farms that had harvest work orders may show them reset: " + e);
        }
    }
}
