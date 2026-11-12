// SPDX-License-Identifier: MIT
// Part of the Geoland library: unofficial, fan-made mods for HavenCraft.
// HavenCraft is the work of Dionic Software; this project is not affiliated with them.
//
// WorldStartedPatch: tells the plugin that a new world is being created, and hands it
// that world's work module.
//
// How it relates to the game: the game builds a fresh set of modules for every world it
// loads, the villager work module among them, and only afterwards creates the world's
// objects. This patch runs when the work module has been built. That is early enough
// for the plugin to give each harvestable object its adapter as the object is created
// (HarvestableLifecyclePatches), which the "world loaded" call would be too late for.
using System.Reflection;
using HarmonyLib;

namespace Geoland.Harvest;

[HarmonyPatch]
internal static class WorldStartedPatch
{
    private static bool Prepare()
    {
        return GameAccess.Available;
    }

    private static MethodBase TargetMethod()
    {
        return GameAccess.WorkModuleConstructor;
    }

    private static void Postfix(VillagerWorkModule __instance)
    {
        HarvestWork.WorldStarted(__instance);
    }
}
