// SPDX-License-Identifier: MIT
// Part of the Geoland library: unofficial, fan-made mods for HavenCraft.
// HavenCraft is the work of Dionic Software; this project is not affiliated with them.
//
// HarvestWork: the harvest itself. Decides whether an item is on an object right now,
// hands it to the villager when the work is done, and lets it grow back.
//
// How it relates to the game: to the game a harvestable object is either full or empty.
// A harvest takes everything, and the object refills after a set time. This plugin
// collects one item at a time (see HarvestOrder for why), so it keeps its own note of
// which items have been taken from an object the game still calls full:
//  - Taking the wax leaves the honey in the hive. The wax comes back after the object's
//    normal refill time.
//  - When the last item has been taken, the object is emptied the game's own way and
//    refills the game's own way.
//  - Whenever the game itself empties or refills an object, as when the player harvests
//    it by hand, the note for it is dropped: the game's state is the truth.
//  - The notes are kept in memory only. After loading a save, a full object offers all
//    its items again.
//
// The game's work planner reaches this class through an object's adapter: the plugin's
// own (HarvestTaskAdapter) or the game's adapter for choppable objects
// (ChoppableAdapterPatches). Both ask the same questions, answered here.
using System;
using System.Collections.Generic;
using System.Linq;
using Geoland.Shared;
using UnityEngine;

namespace Geoland.Harvest;

internal static class HarvestWork
{
    // How often the plugin looks for items that have grown back, in seconds.
    private const float RegrowCheckSeconds = 1f;

    // For each object with something taken: the item, and the game tick at which it is back.
    private static readonly Dictionary<ResourceHarvestWOComponent, Dictionary<string, int>> regrowTickByItem =
        new Dictionary<ResourceHarvestWOComponent, Dictionary<string, int>>();

    private static readonly Dictionary<ResourceHarvestWOComponent, HarvestTaskAdapter> ownAdapters =
        new Dictionary<ResourceHarvestWOComponent, HarvestTaskAdapter>();

    private static VillagerWorkModule work;
    private static SimulationModule simulation;
    private static bool worldIsLoaded;
    private static float nextRegrowCheck;

    // The game asks an adapter "can you do this task?" and then "what would you yield?",
    // and the second question does not repeat the task. The answer depends on it: the
    // task for wax yields only wax. So the task and object of the first question are
    // remembered for the second. The memory is kept per thread, so that the two
    // questions pair up correctly even if the planner ever asks from more than one.
    [ThreadStatic] private static string lastTaskAsked;
    [ThreadStatic] private static ResourceHarvestWOComponent lastHarvestAsked;

    // The object a villager harvested this frame (see ShowsHarvestProgress).
    private static int workedOnFrame = -1;
    private static WorldObject workedOnObject;

    // ---- the world ---------------------------------------------------------------

    /// <summary>A new world is being created: forget everything about the last one.</summary>
    internal static void WorldStarted(VillagerWorkModule workModule)
    {
        work = workModule;
        simulation = null;
        worldIsLoaded = false;
        ownAdapters.Clear();
        regrowTickByItem.Clear();
    }

    /// <summary>
    /// The world has finished loading. Until now the work planner was not ready to be
    /// told about changes; from now on it is.
    /// </summary>
    internal static void WorldLoaded()
    {
        worldIsLoaded = true;
    }

    // ---- adapters for objects that have none ----------------------------------------

    /// <summary>
    /// Gives a new harvestable object the plugin's adapter, unless it is choppable and so
    /// already has the game's.
    /// </summary>
    internal static void ObjectCreated(ResourceHarvestWOComponent harvest)
    {
        if (work == null) return;
        WorldObject worldObject = harvest.GetWorldObject();
        HarvestableType type = HarvestCatalog.TypeOf(worldObject);
        if (type == null || type.IsChoppable) return;
        if (work.taskWorldObjectManager.TryGetVillagerTaskWO(worldObject) != null) return;

        var adapter = new HarvestTaskAdapter(harvest);
        work.taskWorldObjectManager.AddVillagerTaskWorldObject(worldObject, adapter);
        ownAdapters[harvest] = adapter;
    }

    /// <summary>Takes the plugin's adapter away from an object that is being destroyed.</summary>
    internal static void ObjectDestroyed(ResourceHarvestWOComponent harvest)
    {
        regrowTickByItem.Remove(harvest);
        if (work == null || !ownAdapters.TryGetValue(harvest, out HarvestTaskAdapter adapter)) return;

        ownAdapters.Remove(harvest);
        work.taskWorldObjectManager.RemoveVillagerTaskWorldObject(harvest.GetWorldObject(), adapter);
    }

    /// <summary>
    /// The game emptied or refilled an object. It offers everything again from here, and
    /// the work planner is told that there may be work, or no longer any.
    /// </summary>
    internal static void ObjectEmptiedOrRefilled(ResourceHarvestWOComponent harvest)
    {
        regrowTickByItem.Remove(harvest);
        if (!worldIsLoaded) return;

        WorldObject worldObject = harvest.GetWorldObject();
        if (HarvestCatalog.TypeOf(worldObject) != null) TellPlanner(worldObject);
    }

    // ---- what the work planner asks, through an adapter -----------------------------

    /// <summary>"Can you do this task?" True when the task's item is on the object right now.</summary>
    internal static bool CanDo(ResourceHarvestWOComponent harvest, string taskName)
    {
        lastTaskAsked = taskName;
        lastHarvestAsked = harvest;
        string item = HarvestNames.ItemOfTask(taskName);
        return item != null && IsAvailable(harvest, item);
    }

    /// <summary>
    /// "What would you yield?" Adds the yield of the task just asked about. If the
    /// question comes with no task before it, adds everything that is on the object.
    /// </summary>
    internal static void AddYield(ResourceHarvestWOComponent harvest, List<ItemAmount> outAmounts)
    {
        string onlyItem = lastHarvestAsked == harvest ? HarvestNames.ItemOfTask(lastTaskAsked) : null;
        foreach (ItemStack stack in GameAccess.DescriptionOf(harvest).itemsOnHarvest)
        {
            string item = stack.item.itemName;
            if (onlyItem != null && item != onlyItem) continue;
            if (onlyItem == null && !IsAvailable(harvest, item)) continue;
            outAmounts.Add(new ItemAmount(stack.item, stack.amount));
        }
    }

    /// <summary>True when the item is on the object right now.</summary>
    internal static bool IsAvailable(ResourceHarvestWOComponent harvest, string item)
    {
        if (harvest == null || item == null || !harvest.IsFull()) return false;
        if (!GameAccess.DescriptionOf(harvest).itemsOnHarvest.Any(stack => stack.item.itemName == item)) return false;

        if (!regrowTickByItem.TryGetValue(harvest, out Dictionary<string, int> taken)) return true;
        if (!taken.TryGetValue(item, out int regrowTick)) return true;
        if (Simulation(harvest).GetTick() < regrowTick) return false;

        // It has grown back.
        taken.Remove(item);
        if (taken.Count == 0) regrowTickByItem.Remove(harvest);
        return true;
    }

    /// <summary>
    /// "The work is done." Hands the task's item to the villager. If that leaves nothing
    /// on the object it is emptied the game's way; otherwise the item grows back on its
    /// own while the rest stays.
    /// </summary>
    /// <returns>False if the item was no longer there, so nothing was handed over.</returns>
    internal static bool Finish(ResourceHarvestWOComponent harvest, string taskName, IVillagerTaskWorkHandler villager, VillagerNPCComponent worker)
    {
        string item = HarvestNames.ItemOfTask(taskName);
        if (!IsAvailable(harvest, item)) return false;

        ResourceHarvestWOCDescription description = GameAccess.DescriptionOf(harvest);
        var yield = new List<ItemAmount>();
        foreach (ItemStack stack in description.itemsOnHarvest)
        {
            if (stack.item.itemName == item) yield.Add(new ItemAmount(stack.item, stack.amount));
        }
        if (!villager.TryAddWorkOutputItems(yield))
        {
            GameHook.Log.LogError("A villager could not carry all the " + item + " from "
                + harvest.GetWorldObject().description.worldObjectName);
        }

        SimulationModule clock = Simulation(harvest);
        if (!regrowTickByItem.TryGetValue(harvest, out Dictionary<string, int> taken))
        {
            taken = new Dictionary<string, int>();
            regrowTickByItem[harvest] = taken;
        }
        taken[item] = clock.GetTick() + description.regenerateTicks;
        TakeTheRestIfItFits(harvest, description, taken, worker, clock);

        bool anythingLeft = description.itemsOnHarvest.Any(stack => IsAvailable(harvest, stack.item.itemName));
        if (anythingLeft)
        {
            TellPlanner(harvest.GetWorldObject());
        }
        else
        {
            regrowTickByItem.Remove(harvest);
            GameAccess.EmptyAndStartRefill(harvest, clock, description.regenerateTicks);
        }
        return true;
    }

    // One visit, everything that fits. The work order's own item has just been handed
    // over the game's way. The object's other items are now put straight into the
    // worker's inventory, each only if all of it fits; whatever does not fit stays on
    // the object for a later visit.
    //
    // Why straight into the inventory: the game's hand-over accepts only the items the
    // work order's recipe lists, and silently discards anything else. And why not list
    // them in the recipe: the planner then insists on carrying room for all of them
    // before it sends anyone, which is how a full honey store once stopped the wax.
    // Done this way, each work order is still judged by its own item alone, and the
    // extra items ride along only when there is room. The worker stores them in the
    // plot's chests with the rest of what they carry.
    private static void TakeTheRestIfItFits(ResourceHarvestWOComponent harvest, ResourceHarvestWOCDescription description,
        Dictionary<string, int> taken, VillagerNPCComponent worker, SimulationModule clock)
    {
        if (!Plugin.TakeEverythingThatFits.Value || worker == null) return;
        try
        {
            ItemContainer inventory = worker.TryGetInventory();
            if (inventory == null) return;

            foreach (ItemStack stack in description.itemsOnHarvest)
            {
                string other = stack.item.itemName;
                if (taken.ContainsKey(other) && clock.GetTick() < taken[other]) continue;   // already taken
                if (inventory.GetSpaceForItem(stack.item) < stack.amount) continue;         // does not fit: leave it

                int notAdded = inventory.AddItemPartially(stack.item, stack.amount);
                if (notAdded > 0)
                {
                    // The room was gone after all. Take back what went in, so the object
                    // still holds the whole amount.
                    inventory.TryRemoveItemFully(stack.item, stack.amount - notAdded);
                    continue;
                }
                taken[other] = clock.GetTick() + description.regenerateTicks;
            }
        }
        catch (Exception e)
        {
            GameHook.Log.LogError("Could not take an object's other items in the same visit; they stay on it: " + e);
        }
    }

    // ---- hand-picking a half-collected object ---------------------------------------

    /// <summary>
    /// The items a villager has already taken from this object and that have not grown
    /// back, or null if nothing has been taken. Used when the player harvests by hand,
    /// so that the player gets only what is really left (HandHarvestPatch).
    /// </summary>
    internal static ICollection<string> ItemsAlreadyTaken(ResourceHarvestWOComponent harvest)
    {
        if (!regrowTickByItem.TryGetValue(harvest, out Dictionary<string, int> taken)) return null;

        int now = Simulation(harvest).GetTick();
        List<string> stillGone = taken.Where(entry => now < entry.Value).Select(entry => entry.Key).ToList();
        return stillGone.Count == 0 ? null : stillGone;
    }

    // ---- the progress bar of choppable objects --------------------------------------

    /// <summary>Notes that a villager harvested this choppable object in this frame.</summary>
    internal static void WorkedOn(WorldObject worldObject)
    {
        workedOnFrame = Time.frameCount;
        workedOnObject = worldObject;
    }

    /// <summary>
    /// True when the progress bar over this choppable object should follow the harvest.
    /// The game's adapter reports how far the object has been chopped down, which is not
    /// what a villager harvesting it is doing.
    /// </summary>
    internal static bool ShowsHarvestProgress(WorldObject worldObject)
    {
        return workedOnFrame == Time.frameCount && workedOnObject == worldObject;
    }

    // ---- items growing back ---------------------------------------------------------

    /// <summary>
    /// Called every frame. About once a second, tells the work planner about items that
    /// have grown back on their own, so that waiting villagers go and collect them.
    /// </summary>
    internal static void LookForRegrownItems()
    {
        if (!worldIsLoaded || regrowTickByItem.Count == 0 || Time.unscaledTime < nextRegrowCheck) return;
        nextRegrowCheck = Time.unscaledTime + RegrowCheckSeconds;
        try
        {
            foreach (ResourceHarvestWOComponent harvest in regrowTickByItem.Keys.ToList())
            {
                WorldObject worldObject = harvest.GetWorldObject();
                if (!worldObject.IsValid())
                {
                    regrowTickByItem.Remove(harvest);
                    continue;
                }

                int now = Simulation(harvest).GetTick();
                Dictionary<string, int> taken = regrowTickByItem[harvest];
                List<string> regrown = taken.Where(entry => now >= entry.Value).Select(entry => entry.Key).ToList();
                if (regrown.Count == 0) continue;

                foreach (string item in regrown) taken.Remove(item);
                if (taken.Count == 0) regrowTickByItem.Remove(harvest);
                TellPlanner(worldObject);
            }
        }
        catch (Exception e)
        {
            GameHook.Log.LogError("Looking for regrown items failed: " + e);
        }
    }

    // Tells the work planner that what this object offers has changed. Only once the
    // world has loaded: before that the planner is not ready to hear it.
    private static void TellPlanner(WorldObject worldObject)
    {
        if (!worldIsLoaded || work == null) return;
        if (work.taskWorldObjectManager.TryGetVillagerTaskWO(worldObject) == null) return;
        work.taskWorldObjectManager.NotifyTaskWorldObjectStateChanged(worldObject);
    }

    // The game's clock. Every harvestable object holds the same one, so it is taken
    // from the first object asked about and kept for the rest of the world's session.
    private static SimulationModule Simulation(ResourceHarvestWOComponent harvest)
    {
        if (simulation == null) simulation = GameAccess.SimulationOf(harvest);
        return simulation;
    }
}
