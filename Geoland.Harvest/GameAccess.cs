// SPDX-License-Identifier: MIT
// Part of the Geoland library: unofficial, fan-made mods for HavenCraft.
// HavenCraft is the work of Dionic Software; this project is not affiliated with them.
//
// GameAccess: the one place where this plugin reaches into private parts of the game.
//
// How it relates to the game: three parts of the game are involved.
//  - The "harvest" part of a world object (ResourceHarvestWOComponent) knows whether the
//    object is full and when it refills. What it yields, the game's clock and the refill
//    time are private, as is the call that empties it.
//  - Objects that can be chopped down carry an adapter that tells the work planner how
//    to chop them. The adapter's class is not public. Its five methods are patched so
//    that it can answer for harvesting too (ChoppableAdapterPatches).
//  - The order book module takes the work orders read from a save file. One patch there
//    translates the names this plugin's work orders had in earlier versions
//    (SavedNamesPatch).
// Everything is found by name once, at start-up. If a game update renames or removes
// something, the log says so and the plugin adds no work orders; the game then behaves
// exactly as it does without the plugin.
using System;
using System.Collections.Generic;
using System.Reflection;
using Geoland.Shared;
using UnityEngine;

namespace Geoland.Harvest;

internal static class GameAccess
{
    private const string NeededFor = "villagers harvesting";
    private const string NeededForOldSaves = "keeping farm work orders from saves made with earlier versions";

    private static FieldInfo harvestDescription;
    private static FieldInfo harvestSimulation;
    private static FieldInfo harvestRefillTick;
    private static MethodInfo harvestSetIsFull;
    private static FieldInfo savedOrderBooks;

    /// <summary>Creates the harvest part of a world object.</summary>
    internal static ConstructorInfo HarvestConstructor { get; private set; }

    /// <summary>Sets whether a harvestable object is full. Every emptying and refilling goes through it.</summary>
    internal static MethodInfo HarvestSetIsFull { get { return harvestSetIsFull; } }

    /// <summary>Creates the villager work module, once for each world.</summary>
    internal static ConstructorInfo WorkModuleConstructor { get; private set; }

    /// <summary>The adapter's "can you do this task" method.</summary>
    internal static MethodInfo ChoppableCanDoTask { get; private set; }

    /// <summary>The adapter's "what would you yield" method.</summary>
    internal static MethodInfo ChoppableYield { get; private set; }

    /// <summary>The adapter's "do some of the work" method, called every frame of the work.</summary>
    internal static MethodInfo ChoppableStep { get; private set; }

    /// <summary>The adapter's "the work is done" method.</summary>
    internal static MethodInfo ChoppableFinish { get; private set; }

    /// <summary>The adapter's "how far along is the work" method.</summary>
    internal static MethodInfo ChoppableProgress { get; private set; }

    /// <summary>Takes the order books read from a save file.</summary>
    internal static MethodInfo OrderBooksLoadFromSave { get; private set; }

    /// <summary>False when the game no longer has what harvesting needs.</summary>
    internal static bool Available { get; private set; }

    /// <summary>False when work orders saved under their old names cannot be translated.</summary>
    internal static bool CanTranslateSavedNames
    {
        get { return OrderBooksLoadFromSave != null && savedOrderBooks != null; }
    }

    /// <summary>Looks everything up. Called once, when the plugin starts.</summary>
    internal static void Resolve()
    {
        Type harvest = typeof(ResourceHarvestWOComponent);
        harvestDescription = GameMembers.Field(harvest, "desc", typeof(ResourceHarvestWOCDescription), NeededFor);
        harvestSimulation = GameMembers.Field(harvest, "simulationModule", typeof(SimulationModule), NeededFor);
        harvestRefillTick = GameMembers.Field(harvest, "refillTick", typeof(int), NeededFor);
        harvestSetIsFull = GameMembers.Method(harvest, "SetIsFull", NeededFor);
        HarvestConstructor = GameMembers.Constructor(harvest, NeededFor);
        WorkModuleConstructor = GameMembers.Constructor(typeof(VillagerWorkModule), NeededFor);

        // The class of the adapter that choppable objects carry.
        Type adapter = GameMembers.HiddenType("ResourceNodeVillagerTaskWO", NeededFor);
        if (adapter != null)
        {
            ChoppableCanDoTask = GameMembers.Method(adapter, nameof(IVillagerTaskWO.CanDoTask), NeededFor);
            ChoppableYield = GameMembers.Method(adapter, nameof(IVillagerTaskWO.GetRequiredOutputSpaceAmounts), NeededFor);
            ChoppableStep = GameMembers.Method(adapter, nameof(IVillagerTaskWO.TryExecuteTaskStep), NeededFor);
            ChoppableFinish = GameMembers.Method(adapter, nameof(IVillagerTaskWO.TryExecuteTaskFinish), NeededFor);
            ChoppableProgress = GameMembers.Method(adapter, nameof(IVillagerTaskWO.GetWorkProgressOverride), NeededFor);
        }

        Available = harvestDescription != null && harvestSimulation != null && harvestRefillTick != null
            && harvestSetIsFull != null && HarvestConstructor != null && WorkModuleConstructor != null
            && ChoppableCanDoTask != null && ChoppableYield != null && ChoppableStep != null
            && ChoppableFinish != null && ChoppableProgress != null;

        OrderBooksLoadFromSave = GameMembers.Method(typeof(OrderBookModule), "LoadFromSave", NeededForOldSaves);
        Type savedData = GameMembers.HiddenType("OrderBookSaveManager+Serializable", NeededForOldSaves);
        if (savedData != null)
        {
            savedOrderBooks = GameMembers.Field(savedData, "orderBooks",
                typeof(List<OrderBookSaveManager.OrderBookSerializable>), NeededForOldSaves);
        }
    }

    /// <summary>What a harvestable object yields and how long it takes to refill.</summary>
    internal static ResourceHarvestWOCDescription DescriptionOf(ResourceHarvestWOComponent harvest)
    {
        return (ResourceHarvestWOCDescription)harvestDescription.GetValue(harvest);
    }

    /// <summary>The game's clock, as the harvestable object holds it.</summary>
    internal static SimulationModule SimulationOf(ResourceHarvestWOComponent harvest)
    {
        return (SimulationModule)harvestSimulation.GetValue(harvest);
    }

    /// <summary>
    /// Empties a harvestable object and starts its refill timer, the same three steps the
    /// game takes when the player harvests it, without dropping items on the ground.
    /// </summary>
    internal static void EmptyAndStartRefill(ResourceHarvestWOComponent harvest, SimulationModule simulation, int refillTicks)
    {
        const bool full = false;
        const bool evenIfUnchanged = false;
        harvestSetIsFull.Invoke(harvest, new object[] { full, evenIfUnchanged });
        harvestRefillTick.SetValue(harvest, simulation.GetTick() + refillTicks);
        simulation.AddTickUpdate(harvest, refillTicks);
    }

    /// <summary>The order books in the data read from a save file.</summary>
    internal static List<OrderBookSaveManager.OrderBookSerializable> SavedOrderBooks(object savedData)
    {
        return (List<OrderBookSaveManager.OrderBookSerializable>)savedOrderBooks.GetValue(savedData);
    }

    /// <summary>
    /// The game's object definition files, as text. They are the same files the game
    /// reads; the plugin reads them a moment earlier (see HarvestCatalog).
    /// </summary>
    /// <returns>The files, or null (with one line in the log) if they cannot be found.</returns>
    internal static IEnumerable<TextAsset> ObjectDefinitionFiles()
    {
        const string resourcePath = "WorldObjects/worldObjectResources";
        UnityEngine.Object resources = Resources.Load(resourcePath);
        if (resources == null)
        {
            GameHook.Log.LogError("The game has changed: its object definitions are no longer at '" + resourcePath
                + "'. They were needed for " + NeededFor + ", which is now switched off.");
            return null;
        }
        FieldInfo files = GameMembers.Field(resources.GetType(), "worldObjectDescriptions", typeof(IEnumerable<TextAsset>), NeededFor);
        return files == null ? null : (IEnumerable<TextAsset>)files.GetValue(resources);
    }
}
