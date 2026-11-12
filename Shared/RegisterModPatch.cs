// SPDX-License-Identifier: MIT
// Part of the Geoland library: unofficial, fan-made mods for HavenCraft.
// HavenCraft is the work of Dionic Software; this project is not affiliated with them.
//
// RegisterModPatch: adds the plugin to the game's own list of mods.
//
// How it relates to the game: HavenCraft has an official mod interface that is planned
// for later and does not yet cover what these mods need. The game creates a mod loader
// module with every world. That module calls each mod in its list once when the world
// has loaded and again every frame. This patch adds the plugin's mod to that list just
// before the "world loaded" call goes out, so the plugin runs on the game's own schedule
// and is told about every world, including a reload.
using System;
using Dionic.ModAPI_1;
using HarmonyLib;

namespace Geoland.Shared;

[HarmonyPatch(typeof(DionicModLoaderModule), nameof(DionicModLoaderModule.WorldInitAfterSaveLoad))]
internal static class RegisterModPatch
{
    private static void Prefix(DionicModLoaderModule __instance)
    {
        if (GameHook.CreateMod == null) return;
        try
        {
            IMod mod = GameHook.CreateMod();
            foreach (IMod existing in __instance.mods)
            {
                if (existing.GetType() == mod.GetType()) return;
            }
            __instance.mods.Add(mod);
            GameHook.Log.LogInfo(mod.GetType().Name + " registered with the game's mod loader");
        }
        catch (Exception e)
        {
            GameHook.Log.LogError("Could not register with the game's mod loader: " + e);
        }
    }
}
