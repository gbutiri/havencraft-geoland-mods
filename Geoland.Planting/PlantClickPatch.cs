// SPDX-License-Identifier: MIT
// Part of the Geoland library: unofficial, fan-made mods for HavenCraft.
// HavenCraft is the work of Dionic Software; this project is not affiliated with them.
//
// PlantClickPatch: turns a "use item" click into planting while planting mode is on.
//
// How it relates to the game: every "use item" and "interact" click on the world goes
// through one method of the player action module, which hands it to the selected item's
// action (eating a berry, for example). This patch runs before that method. When the
// click is a planting click it plants and then tells the game the click has been dealt
// with, so the selected item is not also used the normal way. Any other click is passed
// on untouched, as is a click on the hotbar or another part of the interface.
using System;
using System.Reflection;
using Geoland.Shared;
using HarmonyLib;

namespace Geoland.Planting;

[HarmonyPatch]
internal static class PlantClickPatch
{
    private static bool Prepare()
    {
        return GameAccess.Available;
    }

    private static MethodBase TargetMethod()
    {
        return GameAccess.ClickHandler;
    }

    // Returning false stops the game's own handling of the click.
    private static bool Prefix(PlayerActionConfirmType confirmType)
    {
        PlantingMod mod = PlantingMod.Current;
        if (mod == null || !mod.ShouldTakeClick(confirmType)) return true;
        try
        {
            if (mod.ClickWasOnInterface()) return true;
            mod.Notify(mod.TryPlant());
        }
        catch (Exception e)
        {
            GameHook.Log.LogError("Planting on click failed: " + e);
            mod.Notify("Planting failed, see the BepInEx log.");
        }
        return false;
    }
}
