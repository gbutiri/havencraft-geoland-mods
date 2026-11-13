// SPDX-License-Identifier: MIT
// Part of the Geoland library: unofficial, fan-made mods for HavenCraft.
// HavenCraft is the work of Dionic Software; this project is not affiliated with them.
//
// ChoppableAdapterPatches: teaches the game's adapter for choppable objects to harvest.
//
// How it relates to the game: bushes and wild cotton can be chopped down, so the game
// gives each of them the adapter it uses for trees and rocks. The work planner allows
// one adapter per object, so the plugin cannot add its own beside it. Instead, the five
// questions the planner asks that adapter are patched. For a harvest task on a kind of
// object the plugin manages, the answer comes from HarvestWork. For anything else, and
// that includes every tree and rock, the game's own answer stands.
//
// The adapter's class is not public, so the patches receive it as a plain object and
// reach the world object through the interface it implements.
using System;
using System.Collections.Generic;
using System.Reflection;
using Geoland.Shared;
using HarmonyLib;

namespace Geoland.Harvest;

internal static class ChoppableAdapterPatches
{
    // Tells the game to show the villager's own work time as the progress.
    private const float NoProgressOfItsOwn = -1f;

    private static bool failureLogged;

    // These patches answer the work planner, which asks about every tree and rock. An
    // error must not reach it, and must not fill the log: the first one is written down,
    // and each patch then gives the answer that leaves the game's own behaviour standing.
    private static void LogFailure(string question, Exception e)
    {
        if (failureLogged) return;
        failureLogged = true;
        GameHook.Log.LogError("Answering the work planner (" + question + ") failed; shown once: " + e);
    }

    private static WorldObject ObjectOf(object adapter)
    {
        return ((IVillagerTaskWO)adapter).GetWorldObject();
    }

    // The harvest part of the adapter's object, or null if the object is not a
    // choppable kind this plugin manages.
    private static ResourceHarvestWOComponent ManagedHarvest(object adapter)
    {
        WorldObject worldObject = ObjectOf(adapter);
        if (!worldObject.IsValid()) return null;

        HarvestableType type = HarvestCatalog.TypeOf(worldObject);
        if (type == null || !type.IsChoppable) return null;
        return worldObject.TryGetComponent<ResourceHarvestWOComponent>();
    }

    // "Can you do this task?" The game's adapter says yes only to chopping.
    [HarmonyPatch]
    private static class CanDoTask
    {
        private static bool Prepare() { return GameAccess.Available; }

        private static MethodBase TargetMethod() { return GameAccess.ChoppableCanDoTask; }

        private static void Postfix(object __instance, string taskName, ref bool __result)
        {
            if (HarvestNames.ItemOfTask(taskName) == null) return;
            try
            {
                ResourceHarvestWOComponent harvest = ManagedHarvest(__instance);
                __result = harvest != null && HarvestWork.CanDo(harvest, taskName);
            }
            catch (Exception e)
            {
                __result = false;
                LogFailure("can you do this task", e);
            }
        }
    }

    // "What would you yield?" No chopping work order tracks the kinds of object this
    // plugin manages (HarvestCatalog skips any that are tracked already), so for the
    // planner their yield is always their harvest.
    [HarmonyPatch]
    private static class Yield
    {
        private static bool Prepare() { return GameAccess.Available; }

        private static MethodBase TargetMethod() { return GameAccess.ChoppableYield; }

        private static bool Prefix(object __instance, List<ItemAmount> outAmounts)
        {
            try
            {
                ResourceHarvestWOComponent harvest = ManagedHarvest(__instance);
                if (harvest == null) return true;
                HarvestWork.AddYield(harvest, outAmounts);
                return false;
            }
            catch (Exception e)
            {
                LogFailure("what would you yield", e);
                return true;
            }
        }
    }

    // "Do some of the work", every frame. The game's adapter would damage the object as
    // if it were being chopped. For a harvest task nothing is damaged; the answer only
    // says whether the item is still there (true stops the work early).
    [HarmonyPatch]
    private static class Step
    {
        private static bool Prepare() { return GameAccess.Available; }

        private static MethodBase TargetMethod() { return GameAccess.ChoppableStep; }

        private static bool Prefix(object __instance, string taskName, ref bool __result)
        {
            string item = HarvestNames.ItemOfTask(taskName);
            if (item == null) return true;

            // Whatever happens, a harvest task must never reach the game's chopping.
            try
            {
                bool nothingLeftToTake = !HarvestWork.IsAvailable(ManagedHarvest(__instance), item);
                __result = nothingLeftToTake;
                if (!nothingLeftToTake) HarvestWork.WorkedOn(ObjectOf(__instance));
            }
            catch (Exception e)
            {
                __result = true;
                LogFailure("do some of the work", e);
            }
            return false;
        }
    }

    // "How far along is the work?" The game's adapter reports how far the object has
    // been chopped down. While a villager is harvesting it, the villager's own work
    // time is shown.
    [HarmonyPatch]
    private static class Progress
    {
        private static bool Prepare() { return GameAccess.Available; }

        private static MethodBase TargetMethod() { return GameAccess.ChoppableProgress; }

        private static void Postfix(object __instance, ref float __result)
        {
            try
            {
                if (HarvestWork.ShowsHarvestProgress(ObjectOf(__instance))) __result = NoProgressOfItsOwn;
            }
            catch (Exception e)
            {
                LogFailure("how far along is the work", e);
            }
        }
    }

    // "The work is done." For chopping, the game's adapter hands nothing over at this
    // point. A harvest hands its item to the villager now.
    [HarmonyPatch]
    private static class Finish
    {
        private static bool Prepare() { return GameAccess.Available; }

        private static MethodBase TargetMethod() { return GameAccess.ChoppableFinish; }

        // The parameter names are the game's; that is how the patch is handed them.
        private static bool Prefix(object __instance, string taskName, IVillagerTaskWorkHandler taskWorkHandler,
            VillagerNPCComponent villager, ref bool __result)
        {
            if (HarvestNames.ItemOfTask(taskName) == null) return true;
            try
            {
                __result = HarvestWork.Finish(ManagedHarvest(__instance), taskName, handOver: taskWorkHandler, worker: villager);
            }
            catch (Exception e)
            {
                __result = false;
                LogFailure("the work is done", e);
            }
            return false;
        }
    }
}
