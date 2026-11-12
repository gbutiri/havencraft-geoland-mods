// SPDX-License-Identifier: MIT
// Part of the Geoland library: unofficial, fan-made mods for HavenCraft.
// HavenCraft is the work of Dionic Software; this project is not affiliated with them.
//
// HarvestOrder: one work order, which collects one item from every kind of object that
// yields it.
//
// How it relates to the game: there is one work order per item, not one per kind of
// object, because of how the game's work planner judges a work order. "Keep 50 in
// storage" counts only the order's first output, and a villager will not start a job
// without room to carry everything it yields. A single "honey and wax" order therefore
// stopped as soon as there was enough honey, and no wax arrived. With "Collect Honey"
// and "Collect Wax" as separate orders, each is judged by its own item.
using System.Collections.Generic;

namespace Geoland.Harvest;

internal sealed class HarvestOrder
{
    /// <summary>The internal name of the item the order collects.</summary>
    internal string Item { get; }

    /// <summary>The order's internal name, which the game writes into save files.</summary>
    internal string RecipeName { get; }

    /// <summary>
    /// The task the order sends a villager to do. The same text is the order's tracking
    /// key and the name of the standing spots on each object.
    /// </summary>
    internal string TaskName { get; }

    /// <summary>The order's title as the player sees it.</summary>
    internal string Title { get; }

    /// <summary>The kinds of object the order covers.</summary>
    internal List<HarvestableType> Types { get; } = new List<HarvestableType>();

    internal HarvestOrder(string item)
    {
        Item = item;
        RecipeName = HarvestNames.RecipeFor(item);
        TaskName = HarvestNames.TaskFor(item);
        Title = HarvestNames.TitleFor(item);
    }
}
