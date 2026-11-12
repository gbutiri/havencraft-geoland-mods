// SPDX-License-Identifier: MIT
// Part of the Geoland library: unofficial, fan-made mods for HavenCraft.
// HavenCraft is the work of Dionic Software; this project is not affiliated with them.
//
// WorkOrderTitlePatches: gives the harvest work orders their titles, such as
// "Collect Honey".
//
// How it relates to the game: the game shows a work order under the text its language
// table (Loca) has for "recipe_" plus the order's internal name. If the table has none,
// it falls back to the name of the item the order yields, so the order would read as
// "Honey", a thing and not a job. The table is read
// through four functions: two that ask whether a text exists and two that fetch it.
// These patches run before them and answer for this plugin's work orders only. Every
// other question is passed on to the game's table untouched.
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace Geoland.Harvest;

internal static class WorkOrderTitlePatches
{
    // The title of the work order the code asks for, or null if it is not one of ours.
    private static string TitleFor(string code)
    {
        HarvestOrder order = HarvestCatalog.OrderFor(HarvestNames.ItemOfTitleCode(code));
        return order == null ? null : order.Title;
    }

    [HarmonyPatch]
    private static class TitleExists
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(Loca), nameof(Loca.Has));
            yield return AccessTools.Method(typeof(Loca), nameof(Loca.HasUnderscoreFit));
        }

        // Returning false keeps the game's own lookup from running.
        private static bool Prefix(string code, ref bool __result)
        {
            if (TitleFor(code) == null) return true;
            __result = true;
            return false;
        }
    }

    [HarmonyPatch]
    private static class TitleText
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(Loca), nameof(Loca.Get), new[] { typeof(string) });
            yield return AccessTools.Method(typeof(Loca), nameof(Loca.GetUnderscoreFit));
        }

        // Returning false keeps the game's own lookup from running.
        private static bool Prefix(string code, ref string __result)
        {
            string title = TitleFor(code);
            if (title == null) return true;
            __result = title;
            return false;
        }
    }
}
