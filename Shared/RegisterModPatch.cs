// SPDX-License-Identifier: MIT
// Part of the Geoland library: unofficial, fan-made mods for HavenCraft.
// HavenCraft is the work of Dionic Software; this project is not affiliated with them.
//
// RegisterModPatch: adds the plugin to the game's own list of mods.
//
// How it relates to the game: HavenCraft creates a mod loader module with every world.
// That module calls each mod in its list once when the world has loaded and again every
// frame, but in this version of the game nothing ever puts a mod in the list. This patch
// puts the plugin's mod there just before the "world loaded" call goes out, so the plugin
// runs on the game's own schedule and is told about every world, including a reload.
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
