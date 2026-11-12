// SPDX-License-Identifier: MIT
// Part of the Geoland library: unofficial, fan-made mods for HavenCraft.
// HavenCraft is the work of Dionic Software; this project is not affiliated with them.
//
// HarvestableType: one kind of harvestable object, such as the berry bush or the beehive.
//
// How it relates to the game: every kind of world object has a definition in the game's
// data files. This class holds the few facts from a definition that the plugin needs.
using System.Collections.Generic;

namespace Geoland.Harvest;

internal sealed class HarvestableType
{
    /// <summary>The game's internal name for this kind of object.</summary>
    internal string ObjectName { get; }

    /// <summary>The internal names of the items one harvest yields, in alphabetical order.</summary>
    internal IReadOnlyList<string> Items { get; }

    /// <summary>
    /// True when the object can also be chopped down. Such objects already carry the
    /// game's adapter for chopping, and the game allows one adapter per object, so that
    /// adapter is taught to harvest (ChoppableAdapterPatches). The others get the
    /// plugin's own adapter (HarvestTaskAdapter).
    /// </summary>
    internal bool IsChoppable { get; }

    /// <summary>Half the longer side of the object's footprint, in cells.</summary>
    internal float HalfSize { get; }

    internal HarvestableType(string objectName, IReadOnlyList<string> items, bool isChoppable, float halfSize)
    {
        ObjectName = objectName;
        Items = items;
        IsChoppable = isChoppable;
        HalfSize = halfSize;
    }
}
