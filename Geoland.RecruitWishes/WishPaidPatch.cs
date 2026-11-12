// SPDX-License-Identifier: MIT
// Part of the Geoland library: unofficial, fan-made mods for HavenCraft.
// HavenCraft is the work of Dionic Software; this project is not affiliated with them.
//
// WishPaidPatch: when a wish has been paid, hands the game the price it expects.
//
// How it relates to the game: after the join panel has taken the goods it calls
// TravelerModule.ConvinceNPC with the traveler and the price that was paid. The game
// makes the traveler a villager and then looks that price up in its own list, to record
// it as used. A wish is not in that list.
//
// This patch runs before that function. If the price is one of our wishes, it is swapped
// for the game's last price, the one the wish stood in for, so the game's record of used
// prices stays exactly as it would be without the plugin.
using System;
using System.Collections.Generic;
using Geoland.Shared;
using HarmonyLib;

namespace Geoland.RecruitWishes;

[HarmonyPatch(typeof(TravelerModule), nameof(TravelerModule.ConvinceNPC))]
internal static class WishPaidPatch
{
    private static void Prefix(TravelerModule __instance, NPC npc, ref List<ItemAmount> price)
    {
        try
        {
            if (!TravelerWishes.IsWish(price)) return;

            List<List<ItemAmount>> prices = GameAccess.JoiningPrices(__instance);
            GameHook.Log.LogInfo("traveler " + (uint)npc.GetID() + " joined for "
                + price[0].amount + " x " + price[0].item.itemName);
            TravelerWishes.Forget(npc);
            price = prices[prices.Count - 1];
        }
        catch (Exception e)
        {
            GameHook.Log.LogError("Could not hand the game its own price after a wish was paid: " + e);
        }
    }
}
