// SPDX-License-Identifier: MIT
// Part of the Geoland library: unofficial, fan-made mods for HavenCraft.
// HavenCraft is the work of Dionic Software; this project is not affiliated with them.
//
// HandHarvestPatch: when the player harvests an object by hand, gives only what is
// really left on it.
//
// How it relates to the game: to the game a harvestable object is full or empty, and a
// harvest by hand gives everything its kind of object yields. This plugin lets a
// villager take one item and leave the rest (see HarvestWork), and the game has no way
// to show or know that. Without this patch, a bush whose fiber a villager had already
// taken would give the player the fiber a second time.
//
// The list of what a kind of object yields belongs to the kind, not to the one object,
// so it cannot simply be changed. This patch swaps in a shorter list for the length of
// the one harvest and puts the full list back straight afterwards, whether the harvest
// succeeded or not.
using System;
using System.Collections.Generic;
using System.Linq;
using Geoland.Shared;
using HarmonyLib;

namespace Geoland.Harvest;

[HarmonyPatch(typeof(ResourceHarvestWOComponent), nameof(ResourceHarvestWOComponent.Harvest))]
internal static class HandHarvestPatch
{
    private static bool Prepare()
    {
        return GameAccess.Available;
    }

    // What is handed from Prefix to Finalizer: the description whose list was swapped,
    // and the full list to put back.
    private sealed class Swap
    {
        internal ResourceHarvestWOCDescription Description;
        internal List<ItemStack> FullList;
    }

    private static void Prefix(ResourceHarvestWOComponent __instance, out Swap __state)
    {
        __state = null;
        try
        {
            ICollection<string> taken = HarvestWork.ItemsAlreadyTaken(__instance);
            if (taken == null) return;

            ResourceHarvestWOCDescription description = GameAccess.DescriptionOf(__instance);
            List<ItemStack> left = description.itemsOnHarvest.Where(stack => !taken.Contains(stack.item.itemName)).ToList();
            __state = new Swap { Description = description, FullList = description.itemsOnHarvest };
            description.itemsOnHarvest = left;
        }
        catch (Exception e)
        {
            __state = null;
            GameHook.Log.LogError("Could not work out what is left on a half-collected object; the game's own harvest stands: " + e);
        }
    }

    // A finalizer runs even if the harvest threw, so the full list is always put back.
    private static void Finalizer(Swap __state)
    {
        if (__state != null) __state.Description.itemsOnHarvest = __state.FullList;
    }
}
