// SPDX-License-Identifier: MIT
// Part of the Geoland library: unofficial, fan-made mods for HavenCraft.
// HavenCraft is the work of Dionic Software; this project is not affiliated with them.
//
// PainterBrush: where the game's plot painter is aimed right now, and how big its
// brush is.
//
// How it relates to the game: the plot painter keeps a square brush of 1 to 5 cells a
// side (its own key changes the size), the point it is aimed at, and the cell at the
// brush's corner. The plugin does not have a brush of its own. It reads the painter's,
// so what is painted upstairs is exactly the square the painter shows.
using System.Collections.Generic;
using UnityEngine;

namespace Geoland.PlotLevels;

internal struct PainterBrush
{
    // The painter's own limits, applied again in case a game update changes them.
    private const int SmallestSize = 1;
    private const int LargestSize = 5;

    /// <summary>False when the painter is not aimed at anything within reach, or the brush is wholly outside the world.</summary>
    internal bool IsValid;

    /// <summary>The side of the square, in cells.</summary>
    internal int Size;

    /// <summary>The cell at the square's lowest corner.</summary>
    internal int CornerX, CornerZ;

    /// <summary>The height level of the surface aimed at.</summary>
    internal int Height;

    /// <summary>The exact point aimed at.</summary>
    internal Vector3 Point;

    /// <summary>Reads the brush from the painter.</summary>
    internal static PainterBrush Read(object painter, PlotModule plots)
    {
        var brush = new PainterBrush();
        if (!GameAccess.PainterHasValidBrush(painter)) return brush;

        Vector3Int corner = GameAccess.PainterBrushCorner(painter);
        brush.Size = Mathf.Clamp(GameAccess.PainterBrushSize(painter), SmallestSize, LargestSize);
        brush.CornerX = corner.x;
        brush.CornerZ = corner.z;
        brush.Point = GameAccess.PainterBrushPoint(painter);
        brush.Height = LevelMap.HeightOf(brush.Point);

        // Valid if at least one of its cells is inside the world.
        brush.IsValid = brush.CornerX + brush.Size > 0 && brush.CornerZ + brush.Size > 0
            && brush.CornerX < plots.GetWorldSizeX() && brush.CornerZ < plots.GetWorldSizeZ();
        return brush;
    }

    /// <summary>The brush's cells that are inside the world, as (x, z).</summary>
    internal List<Vector2Int> Cells(PlotModule plots)
    {
        var cells = new List<Vector2Int>();
        if (!IsValid) return cells;

        int worldSizeX = plots.GetWorldSizeX();
        int worldSizeZ = plots.GetWorldSizeZ();
        for (int dz = 0; dz < Size; dz++)
        {
            for (int dx = 0; dx < Size; dx++)
            {
                int x = CornerX + dx;
                int z = CornerZ + dz;
                if (x >= 0 && z >= 0 && x < worldSizeX && z < worldSizeZ) cells.Add(new Vector2Int(x, z));
            }
        }
        return cells;
    }

    /// <summary>True when both describe the same square on the same floor.</summary>
    internal bool SameSquareAs(PainterBrush other)
    {
        return IsValid == other.IsValid && Size == other.Size && Height == other.Height
            && CornerX == other.CornerX && CornerZ == other.CornerZ;
    }

    /// <summary>The aimed point, for the log.</summary>
    internal string Describe()
    {
        return $"({Point.x:0.0}, {Point.y:0.00}, {Point.z:0.0})";
    }
}
