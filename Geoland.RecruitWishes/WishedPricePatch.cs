// SPDX-License-Identifier: MIT
// Part of the Geoland library: unofficial, fan-made mods for HavenCraft.
// HavenCraft is the work of Dionic Software; this project is not affiliated with them.
//
// WishedPricePatch: gives each waiting traveler a wish once the game's own prices run out.
//
// How it relates to the game: TravelerModule.GetNPCsToConvince is the one function the
// game asks for what each waiting traveler wants. It pairs travelers with the joining
// prices not yet used, and gives every traveler the last price once none is left. The
// join panel shows the answer, checks the player has the goods and takes them.
//
// This patch runs after that function and changes its answer in one case only: where a
// traveler was given the last price and that price has already been paid once, the price
// is replaced with the traveler's wish. Until then the game's own list is still in use
// and the answer is left alone.
using System;
using System.Collections.Generic;
using Geoland.Shared;
using HarmonyLib;

namespace Geoland.RecruitWishes;

[HarmonyPatch(typeof(TravelerModule), nameof(TravelerModule.GetNPCsToConvince))]
internal static class WishedPricePatch
{
    private static void Postfix(TravelerModule __instance, List<(NPC, List<ItemAmount>)> __result)
    {
        if (!TravelerWishes.Ready || __result == null || __result.Count == 0) return;
        try
        {
            List<List<ItemAmount>> prices = GameAccess.JoiningPrices(__instance);
            TravelerData paidSoFar = __instance.TryGetTravelerData();
            if (prices == null || prices.Count == 0 || paidSoFar == null) return;

            int last = prices.Count - 1;
            if (!paidSoFar.ContainsBoughtPrice(last)) return;

            List<ItemAmount> repeatedPrice = prices[last];
            for (int i = 0; i < __result.Count; i++)
            {
                (NPC traveler, List<ItemAmount> price) = __result[i];
                if (ReferenceEquals(price, repeatedPrice))
                {
                    __result[i] = (traveler, TravelerWishes.For(traveler));
                }
            }
        }
        catch (Exception e)
        {
            GameHook.Log.LogError("Could not work out traveler wishes; the game's own price stays: " + e);
        }
    }
}
