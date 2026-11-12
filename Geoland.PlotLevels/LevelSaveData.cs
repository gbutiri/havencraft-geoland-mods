// SPDX-License-Identifier: MIT
// Part of the Geoland library: unofficial, fan-made mods for HavenCraft.
// HavenCraft is the work of Dionic Software; this project is not affiliated with them.
//
// LevelSaveData: the shape of the plugin's file inside a save.
//
// How it relates to the game: a HavenCraft save is a zip of named files, most of them
// JSON. This is the content of the one file the plugin adds. The field names are
// written to the file as they are spelled here, so they must not be renamed.
using System.Collections.Generic;

namespace Geoland.PlotLevels;

internal sealed class LevelSaveData
{
    /// <summary>How many numbers describe one level: x, z, the height it starts at, the plot.</summary>
    internal const int NumbersPerLevel = 4;

    /// <summary>The version of this file's layout.</summary>
    public int version = 1;

    /// <summary>The width of the world in cells, which the cell positions were worked out with.</summary>
    public int worldSizeX;

    /// <summary>One entry per level: [x, z, the height it starts at, the plot].</summary>
    public List<int[]> levels = new List<int[]>();
}
