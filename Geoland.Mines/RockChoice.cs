// SPDX-License-Identifier: MIT
// Part of the Geoland library: unofficial, fan-made mods for HavenCraft.
// HavenCraft is the work of Dionic Software; this project is not affiliated with them.
//
// RockChoice: which of the game's rocks a mine gets, from the settings.
//
// How it relates to the game: the game's data defines five rocks that the "Mine Rocks"
// order works on, named rockA to rockE. rockA and rockB cover two by two cells; rockC,
// rockD and rockE cover one. rockD and rockE have the least health and give the least
// stone, and of the two rockE has the smaller footprint for pathfinding, so it is the
// default. The plugin names these objects and asks the game to place one; it brings no
// rock of its own. The setting is checked against the five names here, and against the
// running game's own list of objects when a world has loaded.
using System;
using Geoland.Shared;

namespace Geoland.Mines;

internal static class RockChoice
{
    /// <summary>The rock a mine gets unless the settings say otherwise.</summary>
    internal const string Default = "rockE";

    // The tracking key the game files rocks under on a plot, and that its "Mine Rocks"
    // order looks for.
    internal const string TrackingKey = PlotWOTrackingKeys.rock;

    private static readonly string[] rockNames = { "rockA", "rockB", "rockC", "rockD", "rockE" };

    /// <summary>
    /// The rock named in the settings, or the default if the setting names something
    /// else. Returns null, with a line in the log, if this version of the game has no
    /// such rock at all.
    /// </summary>
    internal static string FromSettings(WorldObjectModule worldObjects)
    {
        string wanted = (Plugin.Rock.Value ?? "").Trim();
        string chosen = Array.Find(rockNames, name => string.Equals(name, wanted, StringComparison.OrdinalIgnoreCase));
        if (chosen == null)
        {
            chosen = Default;
            GameHook.Log.LogWarning("The Rock setting is '" + wanted + "', which is not one of "
                + string.Join(", ", rockNames) + ". Using " + Default + ".");
        }

        if (IsMinableRock(worldObjects, chosen)) return chosen;
        if (chosen != Default && IsMinableRock(worldObjects, Default))
        {
            GameHook.Log.LogWarning("This version of the game has no minable rock named " + chosen + ". Using " + Default + ".");
            return Default;
        }
        GameHook.Log.LogError("The game has changed: it has no minable rock named " + chosen
            + ". Mines place no rocks. Look for a newer version of the plugin.");
        return null;
    }

    // True when the running game defines this object and files it under the key its
    // "Mine Rocks" order looks for.
    private static bool IsMinableRock(WorldObjectModule worldObjects, string name)
    {
        WorldObjectDescription description = worldObjects.TryGetDescription(name);
        return description != null && description.plotTrackingKeys.Contains(TrackingKey);
    }
}
