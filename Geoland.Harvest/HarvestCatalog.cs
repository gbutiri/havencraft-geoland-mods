// SPDX-License-Identifier: MIT
// Part of the Geoland library: unofficial, fan-made mods for HavenCraft.
// HavenCraft is the work of Dionic Software; this project is not affiliated with them.
//
// HarvestCatalog: finds the kinds of harvestable object the game has, and works out the
// work orders that cover them.
//
// How it relates to the game: an object's definition marks it as harvestable by giving
// it a "resourceHarvest" part that lists the items it yields. Nothing is hard-coded
// here about bushes or hives: the catalog reads the game's own definition files, so a
// harvestable object added by a game update is found too.
//
// The catalog has to exist before the game reads its recipes, because the work orders
// are added to them, and the game reads its recipes before its object definitions. The
// definition files are therefore read here, once, at that earlier moment
// (GameDataPatch asks for it).
using System;
using System.Collections.Generic;
using System.Linq;
using Geoland.Shared;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Geoland.Harvest;

internal static class HarvestCatalog
{
    // Objects the player finds in the wild and cannot make: the wild bee nest and the
    // giant berry bush. They are left to the player. Berry bushes can be planted and
    // hives can be built, and those are what a farm is for.
    private static readonly HashSet<string> leftToThePlayer = new HashSet<string> { "beeNest", "berryBush_giant" };

    private static readonly Dictionary<string, HarvestableType> typeByObjectName = new Dictionary<string, HarvestableType>();
    private static readonly Dictionary<string, HarvestOrder> orderByItem = new Dictionary<string, HarvestOrder>();
    private static bool scanned;

    /// <summary>Every kind of object villagers may harvest.</summary>
    internal static IEnumerable<HarvestableType> Types { get { return typeByObjectName.Values; } }

    /// <summary>Every work order, one per item.</summary>
    internal static IEnumerable<HarvestOrder> Orders { get { return orderByItem.Values; } }

    internal static int OrderCount { get { return orderByItem.Count; } }

    /// <summary>What kind of harvestable this object is, or null if villagers do not harvest it.</summary>
    internal static HarvestableType TypeOf(WorldObject worldObject)
    {
        typeByObjectName.TryGetValue(worldObject.description.worldObjectName, out HarvestableType type);
        return type;
    }

    /// <summary>The work order that collects this item, or null if there is none.</summary>
    internal static HarvestOrder OrderFor(string item)
    {
        if (item == null) return null;
        orderByItem.TryGetValue(item, out HarvestOrder order);
        return order;
    }

    /// <summary>
    /// Reads the game's object definitions and fills the catalog. Does the work the
    /// first time it is called and nothing after that: the definitions do not change
    /// while the game runs.
    /// </summary>
    internal static void EnsureScanned()
    {
        if (scanned) return;
        scanned = true;
        if (!GameAccess.Available) return;
        try
        {
            IEnumerable<TextAsset> files = GameAccess.ObjectDefinitionFiles();
            if (files == null) return;

            foreach (TextAsset file in files)
            {
                JObject definitions;
                try
                {
                    definitions = JObject.Parse(file.text);
                }
                catch (Exception)
                {
                    // Not a file this plugin can read. The game reads it its own way.
                    continue;
                }
                foreach (JProperty definition in definitions.Properties())
                {
                    Consider(definition.Name, definition.Value as JObject);
                }
            }

            foreach (HarvestOrder order in Orders)
            {
                GameHook.Log.LogInfo("work order \"" + order.Title + "\" covers: "
                    + string.Join(", ", order.Types.Select(type => type.ObjectName)));
            }
            if (orderByItem.Count == 0)
            {
                GameHook.Log.LogInfo("no harvestable kinds of object found (or all switched off)");
            }
        }
        catch (Exception e)
        {
            GameHook.Log.LogError("Could not read the game's object definitions: " + e);
        }
    }

    // Adds one kind of object to the catalog if it is harvestable and not ruled out.
    private static void Consider(string objectName, JObject definition)
    {
        if (definition == null || typeByObjectName.ContainsKey(objectName)) return;

        JObject parts = definition["components"] as JObject;
        JObject harvestPart = parts == null ? null : parts["resourceHarvest"] as JObject;
        JArray yields = harvestPart == null ? null : harvestPart["itemsOnHarvest"] as JArray;
        if (yields == null || yields.Count == 0) return;

        List<string> items = yields
            .Select(yield => (string)yield["item"])
            .Where(item => !string.IsNullOrEmpty(item))
            .Distinct()
            .OrderBy(item => item, StringComparer.Ordinal)
            .ToList();
        if (items.Count == 0) return;

        if (leftToThePlayer.Contains(objectName))
        {
            GameHook.Log.LogInfo("leaving " + objectName + " to the player: it is found in the wild, not made");
            return;
        }

        // An object with a "resourceNode" part can be chopped down, and carries the
        // game's adapter for that. If a plot already tracks it, one of the game's own
        // chopping work orders may be using that adapter, and it is left alone.
        bool isChoppable = parts["resourceNode"] != null;
        bool isAlreadyTracked = definition["plotTrackingKeys"] is JArray existingKeys && existingKeys.Count > 0;
        if (isChoppable && isAlreadyTracked)
        {
            GameHook.Log.LogInfo("skipping " + objectName + ": villagers may already chop it");
            return;
        }

        bool switchedOn = Plugin.Settings.Bind(Plugin.SettingsSection, objectName, true,
            "Let farm workers harvest " + objectName + " (yields " + string.Join(", ", items) + ").").Value;
        if (!switchedOn)
        {
            GameHook.Log.LogInfo(objectName + " is switched off in the settings file");
            return;
        }

        float xSize = (float?)definition["xSize"] ?? 1f;
        float zSize = (float?)definition["zSize"] ?? 1f;
        var type = new HarvestableType(objectName, items, isChoppable, Mathf.Max(xSize, zSize) * 0.5f);
        typeByObjectName[objectName] = type;

        foreach (string item in items)
        {
            if (!orderByItem.TryGetValue(item, out HarvestOrder order))
            {
                order = new HarvestOrder(item);
                orderByItem[item] = order;
            }
            order.Types.Add(type);
        }
    }
}
