// SPDX-License-Identifier: MIT
// Part of the Geoland library: unofficial, fan-made mods for HavenCraft.
// HavenCraft is the work of Dionic Software; this project is not affiliated with them.
//
// PlotLevel: one upper level of one cell: "from this height up, this plot".
//
// How it relates to the game: heights are whole grid levels, the unit the game stores
// object positions in, and plots are known by the game's own plot numbers.
namespace Geoland.PlotLevels;

internal struct PlotLevel
{
    /// <summary>
    /// In place of a plot number: "from here up, whatever the ground says again". It is
    /// how a floor above an upper level is handed back to the ground plot.
    /// </summary>
    internal const ushort BackToGround = ushort.MaxValue;

    /// <summary>The level starts at this height and runs up to the next level of the cell, if any.</summary>
    internal int FromHeight;

    /// <summary>The plot the level belongs to, or <see cref="BackToGround"/>.</summary>
    internal ushort Plot;

    internal PlotLevel(int fromHeight, ushort plot)
    {
        FromHeight = fromHeight;
        Plot = plot;
    }
}
