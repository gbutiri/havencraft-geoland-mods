// SPDX-License-Identifier: MIT
// Part of the Geoland library: unofficial, fan-made mods for HavenCraft.
// HavenCraft is the work of Dionic Software; this project is not affiliated with them.
//
// RockChoice: which of the game's rocks a mine gets, from the settings.
//
// How it relates to the game: the game's data defines five rocks that the "Mine Rocks"
// order works on, named rockA to rockE. rockA and rockB cover two by two cells; rockC,
// rockD and rockE cover one. rockC is the medium rock; rockD and rockE are the small
// ones, with the least health and the least stone. The plugin names these objects and
// asks the game to place one; it brings no rock of its own.
//
// The setting is either one of the five names, and every rock is that one, or "random".
// With "random" each new rock is chosen afresh: medium or small with an even chance, and
// when small, either of the two small ones. The two-cell rocks are left out of the random
// choice because they reach past the stake's middle cell. Every name is checked against
// the running game's own list of objects when a world has loaded.
using System;
using System.Collections.Generic;
using Geoland.Shared;

namespace Geoland.Mines;

internal static class RockChoice
{
    /// <summary>The setting unless the player changes it.</summary>
    internal const string Default = Random;

    /// <summary>The setting that asks for a different rock each time.</summary>
    internal const string Random = "random";

    // The tracking key the game files rocks under on a plot, and that its "Mine Rocks"
    // order looks for.
    internal const string TrackingKey = PlotWOTrackingKeys.rock;

    private const string MediumRock = "rockC";

    private static readonly string[] rockNames = { "rockA", "rockB", "rockC", "rockD", "rockE" };

    // What "random" draws from. The medium rock is listed twice so that medium and small
    // come up equally often.
    private static readonly string[] randomDraw = { MediumRock, MediumRock, "rockD", "rockE" };

    /// <summary>
    /// The rocks a mine may get, from the settings: one name, or the list "random" draws
    /// from. A name the setting does not know is replaced by "random". Returns null, with
    /// a line in the log, if this version of the game has none of the rocks.
    /// </summary>
    internal static string[] FromSettings(WorldObjectModule worldObjects)
    {
        string wanted = (Plugin.Rock.Value ?? "").Trim();
        string named = Array.Find(rockNames, name => string.Equals(name, wanted, StringComparison.OrdinalIgnoreCase));
        if (named == null && !string.Equals(wanted, Random, StringComparison.OrdinalIgnoreCase))
        {
            GameHook.Log.LogWarning("The Rock setting is '" + wanted + "', which is neither " + Random + " nor one of "
                + string.Join(", ", rockNames) + ". Using " + Random + ".");
        }

        if (named != null)
        {
            if (IsMinableRock(worldObjects, named)) return new[] { named };
            GameHook.Log.LogWarning("This version of the game has no minable rock named " + named + ". Using " + Random + ".");
        }

        var usable = new List<string>();
        foreach (string name in randomDraw)
        {
            if (IsMinableRock(worldObjects, name)) usable.Add(name);
        }
        if (usable.Count > 0) return usable.ToArray();

        GameHook.Log.LogError("The game has changed: it has none of the rocks " + string.Join(", ", randomDraw)
            + ". Mines place no rocks. Look for a newer version of the plugin.");
        return null;
    }

    /// <summary>The rocks as one phrase for the log.</summary>
    internal static string Describe(string[] rocks)
    {
        return rocks.Length == 1 ? rocks[0] : "a random one of " + string.Join(", ", new HashSet<string>(rocks));
    }

    // True when the running game defines this object and files it under the key its
    // "Mine Rocks" order looks for.
    private static bool IsMinableRock(WorldObjectModule worldObjects, string name)
    {
        WorldObjectDescription description = worldObjects.TryGetDescription(name);
        return description != null && description.plotTrackingKeys.Contains(TrackingKey);
    }
}
