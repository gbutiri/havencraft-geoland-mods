// SPDX-License-Identifier: MIT
// Part of the Geoland library: unofficial, fan-made mods for HavenCraft.
// HavenCraft is the work of Dionic Software; this project is not affiliated with them.
//
// HarvestableLifecyclePatches: follows each harvestable object from its creation to its
// destruction, and notices when the game empties or refills it.
//
// How it relates to the game: the "harvest" part of a world object is created with the
// object, whether the object comes from a save, is spawned by the game, or is placed by
// the player. Three moments matter to the plugin:
//  - Created: an object with no adapter gets the plugin's (a beehive, for example).
//  - Destroyed: the adapter is taken away again, as the game requires.
//  - Emptied or refilled: every such change goes through one method of the game. The
//    plugin drops its own notes about the object, and tells the work planner, which
//    does not otherwise learn that a hive has refilled.
using System;
using System.Reflection;
using Geoland.Shared;
using HarmonyLib;

namespace Geoland.Harvest;

internal static class HarvestableLifecyclePatches
{
    [HarmonyPatch]
    private static class Created
    {
        private static bool Prepare()
        {
            return GameAccess.Available;
        }

        private static MethodBase TargetMethod()
        {
            return GameAccess.HarvestConstructor;
        }

        private static void Postfix(ResourceHarvestWOComponent __instance)
        {
            try
            {
                HarvestWork.ObjectCreated(__instance);
            }
            catch (Exception e)
            {
                GameHook.Log.LogError("Could not make a harvestable object workable: " + e);
            }
        }
    }

    [HarmonyPatch(typeof(ResourceHarvestWOComponent), nameof(ResourceHarvestWOComponent.DestroyImpl))]
    private static class Destroyed
    {
        private static bool Prepare()
        {
            return GameAccess.Available;
        }

        private static void Prefix(ResourceHarvestWOComponent __instance)
        {
            try
            {
                HarvestWork.ObjectDestroyed(__instance);
            }
            catch (Exception e)
            {
                GameHook.Log.LogError("Could not release a harvestable object that is being destroyed: " + e);
            }
        }
    }

    [HarmonyPatch]
    private static class EmptiedOrRefilled
    {
        private static bool Prepare()
        {
            return GameAccess.Available;
        }

        private static MethodBase TargetMethod()
        {
            return GameAccess.HarvestSetIsFull;
        }

        private static void Postfix(ResourceHarvestWOComponent __instance)
        {
            try
            {
                HarvestWork.ObjectEmptiedOrRefilled(__instance);
            }
            catch (Exception e)
            {
                GameHook.Log.LogError("Could not tell the work planner that a harvestable object changed: " + e);
            }
        }
    }
}
