// SPDX-License-Identifier: MIT
// Part of the Geoland library: unofficial, fan-made mods for HavenCraft.
// HavenCraft is the work of Dionic Software; this project is not affiliated with them.
//
// Plantable: which hotbar item plants which plant.
//
// How it relates to the game: items and world objects are known to the game by internal
// names. Each entry pairs the name of an item with the name of the wild plant that yields
// it. The plants are the game's own harvestable objects, so once planted they behave like
// any the game placed itself: they can be harvested, they grow back, and they are saved
// with the world.
//
// The table is in code and not in the settings file on purpose. An entry only works if
// the game has an object that yields the item, and each pair here was checked against
// the game's own object definitions.
using System.Collections.Generic;

namespace Geoland.Planting;

internal sealed class Plantable
{
    private static readonly Dictionary<string, Plantable> byItemName = new Dictionary<string, Plantable>
    {
        // A berry bush: one berry per harvest.
        { "berry", new Plantable("berry", "berryBush02", "Berry bush", "berries") },

        // Wild cotton. Unlike the cotton crop on a field, it is a plant in its own right
        // and does not depend on a farm field.
        { "cotton", new Plantable("cotton", "cottonPlantB", "Wild cotton", "cotton") },

        // The small plain bush, one cell in size. Harvested, it gives fiber and a little wood.
        { "fiber", new Plantable("fiber", "bush01", "Fiber bush", "fiber") },
    };

    /// <summary>What to tell the player to select when nothing plantable is selected.</summary>
    internal const string ItemsForPlayer = "berries, cotton or fiber";

    /// <summary>The item that must be selected in the hotbar. Planting uses up one.</summary>
    internal string ItemName { get; }

    /// <summary>The world object that gets planted.</summary>
    internal string ObjectName { get; }

    /// <summary>The plant's name in notifications, such as "Berry bush".</summary>
    internal string Label { get; }

    /// <summary>The item as it is counted in notifications, such as "berries".</summary>
    internal string ItemPlural { get; }

    private Plantable(string itemName, string objectName, string label, string itemPlural)
    {
        ItemName = itemName;
        ObjectName = objectName;
        Label = label;
        ItemPlural = itemPlural;
    }

    /// <summary>What the named item plants, or null if it plants nothing.</summary>
    internal static Plantable ForItem(string itemName)
    {
        byItemName.TryGetValue(itemName, out Plantable plantable);
        return plantable;
    }
}
