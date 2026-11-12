// SPDX-License-Identifier: MIT
// Part of the Geoland library: unofficial, fan-made mods for HavenCraft.
// HavenCraft is the work of Dionic Software; this project is not affiliated with them.
//
// LevelMap: the upper levels of the loaded world, and every way they change.
//
// How it relates to the game: the game keeps one flat grid with a plot number for each
// ground cell, and no height. That grid is left exactly as it is; here it means "ground
// level". This class adds, per cell, a short list of upper levels on top of it. A
// position belongs to the highest level at or below it, and to the ground plot when
// there is none.
//
// Objects such as beds, chests and workplaces are filed under the plot they stand in,
// and the game re-files them when it raises its "plot shapes changed" event. Whenever a
// level changes, this class raises that same event for the plots that gained or lost
// space, so that everything re-files itself the way it does after ordinary painting.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Geoland.PlotLevels;

internal static class LevelMap
{
    // Keeps something standing exactly on a floor from being counted as below it.
    private const float OnTheFloorMargin = 0.05f;

    // Cell index (z * world width + x) to that cell's upper levels, lowest first.
    private static readonly Dictionary<int, List<PlotLevel>> levelsByCell = new Dictionary<int, List<PlotLevel>>();

    /// <summary>How many cells have at least one upper level.</summary>
    internal static int CellCount { get { return levelsByCell.Count; } }

    /// <summary>Every cell that has upper levels, by cell index, for the overlay and for saving.</summary>
    internal static IEnumerable<KeyValuePair<int, List<PlotLevel>>> Cells { get { return levelsByCell; } }

    /// <summary>Goes up whenever a level changes, so that the overlay knows to redraw.</summary>
    internal static int Version { get; private set; }

    /// <summary>The height level a position is on.</summary>
    internal static int HeightOf(Vector3 position)
    {
        return Mathf.FloorToInt(position.y + OnTheFloorMargin);
    }

    // ---- looking up ---------------------------------------------------------------

    /// <summary>The plot an upper level gives this position to, if one does.</summary>
    /// <returns>False when the ground plot applies: no level reaches the position, or the one that does hands it back.</returns>
    internal static bool TryLookup(PlotModule plots, Vector3 position, out ushort plot)
    {
        plot = 0;
        if (levelsByCell.Count == 0) return false;

        plots.ClampWorldPosAndToXZ(position, out int x, out int z);
        if (!levelsByCell.TryGetValue(CellIndex(plots, x, z), out List<PlotLevel> levels)) return false;

        int height = HeightOf(position);
        for (int i = levels.Count - 1; i >= 0; i--)
        {
            if (levels[i].FromHeight > height) continue;
            if (levels[i].Plot == PlotLevel.BackToGround) return false;
            plot = levels[i].Plot;
            return true;
        }
        return false;
    }

    /// <summary>The plot of the game's own grid under this position, ignoring levels.</summary>
    internal static ushort GroundPlot(PlotModule plots, Vector3 position)
    {
        plots.ClampWorldPosAndToXZ(position, out int x, out int z);
        return plots.GetPlotData()[CellIndex(plots, x, z)];
    }

    /// <summary>
    /// The plot that most of the brush already belongs to through a level that starts on
    /// exactly this floor, or 0 if the brush touches no such level. The game's painter
    /// has the same rule on the ground: the plot under most of the brush is the one a
    /// stroke extends.
    /// </summary>
    internal static ushort MainPlotStartingAt(PlotModule plots, IEnumerable<Vector2Int> brush, int height)
    {
        var cellCountByPlot = new Dictionary<ushort, int>();
        foreach (Vector2Int cell in brush)
        {
            if (!levelsByCell.TryGetValue(CellIndex(plots, cell.x, cell.y), out List<PlotLevel> levels)) continue;
            foreach (PlotLevel level in levels)
            {
                if (level.FromHeight != height || level.Plot == PlotLevel.BackToGround) continue;
                cellCountByPlot.TryGetValue(level.Plot, out int count);
                cellCountByPlot[level.Plot] = count + 1;
            }
        }

        // On a tie the lower plot number wins, so the answer does not depend on chance.
        ushort best = 0;
        int bestCount = 0;
        foreach (KeyValuePair<ushort, int> entry in cellCountByPlot)
        {
            if (entry.Value > bestCount || (entry.Value == bestCount && entry.Key < best))
            {
                best = entry.Key;
                bestCount = entry.Value;
            }
        }
        return best;
    }

    // ---- changing -----------------------------------------------------------------

    /// <summary>Gives the cells, from this height up, to the plot.</summary>
    /// <returns>How many cells changed hands.</returns>
    internal static int Paint(PlotModule plots, IEnumerable<Vector2Int> brush, int height, ushort plot)
    {
        var plotsAffected = new HashSet<ushort> { plot };
        int changed = 0;
        foreach (Vector2Int cell in brush)
        {
            ushort before = plots.GetPlot(CentreOf(cell, height));
            if (before == plot) continue;
            plotsAffected.Add(before);

            int index = CellIndex(plots, cell.x, cell.y);
            if (!levelsByCell.TryGetValue(index, out List<PlotLevel> levels))
            {
                levels = new List<PlotLevel>();
                levelsByCell[index] = levels;
            }
            levels.RemoveAll(level => level.FromHeight == height);
            levels.Add(new PlotLevel(height, plot));
            SortLowestFirst(levels);
            changed++;
        }
        if (changed > 0) LevelsChanged(plots, plotsAffected);
        return changed;
    }

    /// <summary>
    /// Hands the floor at this height in the cells back to the ground plot. Floors below
    /// keep their levels: "the level in force at this height" may start further down,
    /// and erasing must act on the floor aimed at, not on the one the level started on.
    /// </summary>
    /// <returns>How many cells changed hands.</returns>
    internal static int Erase(PlotModule plots, IEnumerable<Vector2Int> brush, int height)
    {
        var plotsAffected = new HashSet<ushort>();
        int changed = 0;
        foreach (Vector2Int cell in brush)
        {
            int index = CellIndex(plots, cell.x, cell.y);
            if (!levelsByCell.TryGetValue(index, out List<PlotLevel> levels)) continue;

            ushort ground = plots.GetPlotData()[index];
            ushort before = plots.GetPlot(CentreOf(cell, height));
            bool hadLevelStartingHere = levels.RemoveAll(level => level.FromHeight == height) > 0;

            // If a level from a lower floor still reaches this one, hand this floor back explicitly.
            int below = levels.FindLastIndex(level => level.FromHeight < height);
            if (below >= 0 && levels[below].Plot != PlotLevel.BackToGround)
            {
                levels.Add(new PlotLevel(height, PlotLevel.BackToGround));
                SortLowestFirst(levels);
            }
            Tidy(index, levels);

            if (before == ground && !hadLevelStartingHere) continue;
            plotsAffected.Add(before);
            plotsAffected.Add(ground);
            if (before != ground) changed++;
        }
        if (changed > 0) LevelsChanged(plots, plotsAffected);
        return changed;
    }

    /// <summary>A plot was deleted: its levels go with it.</summary>
    internal static void PlotRemoved(PlotInfo removed)
    {
        Version++;
        foreach (int index in levelsByCell.Keys.ToList())
        {
            List<PlotLevel> levels = levelsByCell[index];
            levels.RemoveAll(level => level.Plot == removed.id);
            Tidy(index, levels);
        }
    }

    /// <summary>Removes levels that point at a plot the world no longer has.</summary>
    /// <returns>How many levels were removed.</returns>
    internal static int DropLevelsOfMissingPlots(PlotModule plots)
    {
        Version++;
        int dropped = 0;
        foreach (int index in levelsByCell.Keys.ToList())
        {
            List<PlotLevel> levels = levelsByCell[index];
            dropped += levels.RemoveAll(level => level.Plot != PlotLevel.BackToGround && plots.TryGetPlotInfo(level.Plot) == null);
            Tidy(index, levels);
        }
        return dropped;
    }

    /// <summary>
    /// Removes levels that start too low to be an upper floor. Versions of the plugin
    /// before 0.3.1 could make such levels on the ground floor, where they look like
    /// ordinary plot painting but are not. The ground floor belongs to the game's grid.
    /// </summary>
    /// <param name="plots">The world's plot module.</param>
    /// <param name="lowestUpperHeight">For a cell (x, z): the lowest height an upper level may start at.</param>
    /// <returns>How many levels were removed.</returns>
    internal static int DropGroundFloorLevels(PlotModule plots, Func<int, int, int> lowestUpperHeight)
    {
        int worldSizeX = plots.GetWorldSizeX();
        var plotsAffected = new HashSet<ushort>();
        int dropped = 0;
        foreach (int index in levelsByCell.Keys.ToList())
        {
            List<PlotLevel> levels = levelsByCell[index];
            int lowest = lowestUpperHeight(index % worldSizeX, index / worldSizeX);
            foreach (PlotLevel level in levels)
            {
                if (level.FromHeight < lowest && level.Plot != PlotLevel.BackToGround) plotsAffected.Add(level.Plot);
            }
            int removed = levels.RemoveAll(level => level.FromHeight < lowest);
            if (removed == 0) continue;

            dropped += removed;
            plotsAffected.Add(plots.GetPlotData()[index]);
            Tidy(index, levels);
        }
        if (dropped > 0) LevelsChanged(plots, plotsAffected);
        return dropped;
    }

    /// <summary>Forgets every level. Called when a world starts and when it ends.</summary>
    internal static void Clear()
    {
        Version++;
        levelsByCell.Clear();
    }

    // ---- to and from the save file ------------------------------------------------

    /// <summary>The levels as they are written to the save file.</summary>
    internal static LevelSaveData ToSaveData(PlotModule plots)
    {
        var data = new LevelSaveData { worldSizeX = plots.GetWorldSizeX() };
        foreach (KeyValuePair<int, List<PlotLevel>> cell in levelsByCell)
        {
            int x = cell.Key % data.worldSizeX;
            int z = cell.Key / data.worldSizeX;
            foreach (PlotLevel level in cell.Value)
            {
                data.levels.Add(new[] { x, z, level.FromHeight, level.Plot });
            }
        }
        return data;
    }

    /// <summary>Replaces every level with those read from a save file.</summary>
    internal static void FromSaveData(LevelSaveData data)
    {
        Clear();
        if (data == null || data.levels == null || data.worldSizeX <= 0) return;

        foreach (int[] entry in data.levels)
        {
            if (entry == null || entry.Length < LevelSaveData.NumbersPerLevel) continue;
            int index = entry[1] * data.worldSizeX + entry[0];
            if (!levelsByCell.TryGetValue(index, out List<PlotLevel> levels))
            {
                levels = new List<PlotLevel>();
                levelsByCell[index] = levels;
            }
            levels.Add(new PlotLevel(entry[2], (ushort)entry[3]));
        }
        foreach (List<PlotLevel> levels in levelsByCell.Values) SortLowestFirst(levels);
    }

    // ---- helpers --------------------------------------------------------------------

    private static int CellIndex(PlotModule plots, int x, int z)
    {
        return z * plots.GetWorldSizeX() + x;
    }

    // A point in the middle of the cell, half a block above the floor at this height.
    private static Vector3 CentreOf(Vector2Int cell, int height)
    {
        return new Vector3(cell.x + 0.5f, height + 0.5f, cell.y + 0.5f);
    }

    private static void SortLowestFirst(List<PlotLevel> levels)
    {
        levels.Sort((a, b) => a.FromHeight.CompareTo(b.FromHeight));
    }

    // A hand-back with no level underneath it does nothing, and neither does a cell
    // with no levels left. Both are removed.
    private static void Tidy(int index, List<PlotLevel> levels)
    {
        while (levels.Count > 0 && levels[0].Plot == PlotLevel.BackToGround) levels.RemoveAt(0);
        if (levels.Count == 0) levelsByCell.Remove(index);
    }

    // Marks the map as changed and raises the game's "plot shapes changed" event for
    // the plots that gained or lost space.
    private static void LevelsChanged(PlotModule plots, IEnumerable<ushort> plotsAffected)
    {
        Version++;
        List<PlotInfo> infos = plotsAffected.Distinct()
            .Select(id => plots.TryGetPlotInfo(id))
            .Where(info => info != null)
            .ToList();
        if (infos.Count > 0) plots.InvokeOnPlotsShapeChanged(infos);
    }
}
