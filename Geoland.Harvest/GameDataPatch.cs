// SPDX-License-Identifier: MIT
// Part of the Geoland library: unofficial, fan-made mods for HavenCraft.
// HavenCraft is the work of Dionic Software; this project is not affiliated with them.
//
// GameDataPatch: adds the harvest work orders to the game's recipes, and makes each
// harvestable object known to the work planner, as the game reads its data files.
//
// How it relates to the game: recipes and object definitions are JSON text that the
// game parses when a world is created. This patch runs after the JSON parser and adds
// to two kinds of file before the game turns them into its own objects:
//  - To the recipes: one work order per item. Like the game's own "Cut Trees", each is
//    a recipe that sends a villager to an object of a certain kind inside the plot.
//  - To each harvestable object's definition, per item it yields: a tracking key, which
//    makes plots keep track of such objects for the work order, and the spots where a
//    villager stands to do the work. Trees and crops have both in the game's data;
//    bushes and hives have neither.
// Nothing is changed on disk. The additions exist only in the running game.
using System;
using System.Linq;
using System.Reflection;
using Geoland.Shared;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Geoland.Harvest;

[HarmonyPatch]
internal static class GameDataPatch
{
    // Two of the game's own recipes. A parsed file that has both is the recipes file.
    private const string KnownRecipeA = "wood_cutTrees";
    private const string KnownRecipeB = "barley_harvest";

    // How long one harvest takes, in the game's work time, and the animation the
    // villager plays, which is one the game already has.
    private const float WorkDuration = 4f;
    private const string WorkAnimation = "harvesting";

    // In a recipe's output, this amount means "whatever the object yields", as in the
    // game's own "Cut Trees".
    private const int WhateverTheObjectYields = -1;

    // A villager stands this far beyond the edge of the object, and never closer to its
    // centre than the minimum, in cells.
    private const float StandBeyondEdge = 0.3f;
    private const float MinStandDistance = 0.9f;

    // One standing spot on each side of the object, as (x, z) directions.
    private static readonly Vector2[] sides =
    {
        new Vector2(0f, -1f), new Vector2(1f, 0f), new Vector2(-1f, 0f), new Vector2(0f, 1f),
    };

    // Set while this patch is itself parsing JSON, so that it does not act on its own work.
    [ThreadStatic] private static bool isParsingOwnJson;

    private static bool Prepare()
    {
        return GameAccess.Available;
    }

    // The JSON library's parser, which the game uses for both kinds of file.
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method(typeof(JObject), nameof(JObject.Parse), new[] { typeof(string), typeof(JsonLoadSettings) });
    }

    private static void Postfix(JObject __result)
    {
        if (__result == null || isParsingOwnJson) return;
        try
        {
            isParsingOwnJson = true;
            if (__result[KnownRecipeA] is JObject && __result[KnownRecipeB] is JObject)
            {
                // The recipes are read before the object definitions, so the catalog
                // reads the definitions itself, now.
                HarvestCatalog.EnsureScanned();
                AddWorkOrders(__result);
                return;
            }
            foreach (HarvestableType type in HarvestCatalog.Types)
            {
                if (__result[type.ObjectName] is JObject definition) MakeWorkable(definition, type);
            }
        }
        catch (Exception e)
        {
            GameHook.Log.LogError("Could not add the harvest work orders to the game's data: " + e);
        }
        finally
        {
            isParsingOwnJson = false;
        }
    }

    private static void AddWorkOrders(JObject recipes)
    {
        foreach (HarvestOrder order in HarvestCatalog.Orders)
        {
            if (recipes[order.RecipeName] != null) continue;
            recipes[order.RecipeName] = new JObject
            {
                ["inputItems"] = new JArray(),
                ["outputItems"] = new JArray { new JObject { ["item"] = order.Item, ["amount"] = WhateverTheObjectYields } },
                ["workDuration"] = WorkDuration,
                ["villagerTask"] = new JObject
                {
                    ["worldObjectPlotTrackingKey"] = order.TaskName,
                    ["interactionName"] = order.TaskName,
                },
                ["animationName"] = WorkAnimation,
                ["unlockedByDefault"] = true,
            };
        }
    }

    // Gives an object definition, for each item it yields, a tracking key and four
    // standing spots, one on each side, facing the object.
    private static void MakeWorkable(JObject definition, HarvestableType type)
    {
        if (!(definition["components"] is JObject parts) || parts["resourceHarvest"] == null) return;

        JArray trackingKeys = definition["plotTrackingKeys"] as JArray;
        if (trackingKeys == null)
        {
            trackingKeys = new JArray();
            definition["plotTrackingKeys"] = trackingKeys;
        }
        JObject standingSpots = definition["npcInteractionTransforms"] as JObject;
        if (standingSpots == null)
        {
            standingSpots = new JObject();
            definition["npcInteractionTransforms"] = standingSpots;
        }

        float distance = Mathf.Max(type.HalfSize + StandBeyondEdge, MinStandDistance);
        foreach (string item in type.Items)
        {
            HarvestOrder order = HarvestCatalog.OrderFor(item);
            if (!trackingKeys.Any(key => (string)key == order.TaskName)) trackingKeys.Add(order.TaskName);

            var spots = new JArray();
            foreach (Vector2 side in sides)
            {
                spots.Add(new JObject
                {
                    ["pos"] = new JObject { ["x"] = side.x * distance, ["y"] = 0f, ["z"] = side.y * distance },
                    ["rotType"] = "faceCenter",
                    ["testAnimBoolName"] = WorkAnimation,
                });
            }
            standingSpots[order.TaskName] = spots;
        }
    }
}
