// SPDX-License-Identifier: MIT
// Part of the Geoland library: unofficial, fan-made mods for HavenCraft.
// HavenCraft is the work of Dionic Software; this project is not affiliated with them.
//
// SwallowClickPatch: keeps the game from acting on the held and released halves of a
// planting click.
//
// How it relates to the game: besides the click itself, the player action module tells
// the selected item's action when the button goes down and when it comes up, through two
// more methods. An item used by holding the button would otherwise still start while
// PlantClickPatch has taken the click. This patch runs before both methods and skips
// them for a planting click. Any other press or release is passed on untouched.
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace Geoland.Planting;

[HarmonyPatch]
internal static class SwallowClickPatch
{
    private static bool Prepare()
    {
        return GameAccess.Available;
    }

    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return GameAccess.ButtonHeldHandler;
        yield return GameAccess.ButtonReleasedHandler;
    }

    // Returning false stops the game's own handling of the press or release.
    private static bool Prefix(PlayerActionConfirmType confirmType)
    {
        PlantingMod mod = PlantingMod.Current;
        return mod == null || !mod.ShouldTakeClick(confirmType);
    }
}
