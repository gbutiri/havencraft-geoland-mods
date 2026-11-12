// SPDX-License-Identifier: MIT
// Part of the Geoland library: unofficial, fan-made mods for HavenCraft.
// HavenCraft is the work of Dionic Software; this project is not affiliated with them.
//
// LevelOverlay: shows the upper levels while the plot painter is out.
//
// How it relates to the game: the game colours plots by projecting its flat grid
// straight down through every floor, so it cannot show that an upstairs floor belongs
// to a different plot. This class draws that itself:
//  - an outlined tile on every cell that has an upper level, lying on the floor where
//    the level starts, in the colour the game gives the plot it belongs to. An
//    upstairs area therefore matches the ground cells of its plot, and the outline,
//    which the game's flat colouring never has, marks it as an upper level;
//  - a paler preview of the painter's brush, at the height it would paint.
// The tiles are plain geometry handed to Unity to draw each frame. Nothing is added to
// the world, the save or the game's plot data.
//
// It depends on one thing outside the game's own code: Unity's built-in shader
// "Hidden/Internal-Colored", which draws plain vertex colours without lighting. If a
// future version of Unity drops it, the overlay switches itself off and says so in the
// log; painting still works.
using System.Collections.Generic;
using Geoland.Shared;
using UnityEngine;
using UnityEngine.Rendering;

namespace Geoland.PlotLevels;

internal sealed class LevelOverlay
{
    private const string ShaderName = "Hidden/Internal-Colored";

    // Where a tile lies: this far above the floor's grid height, to sit on the floor's
    // surface, and this far in from the cell's edges, so that cells read as a grid.
    private const float TileLift = 0.09f;
    private const float TileInset = 0.06f;

    // A tile is a see-through fill with a stronger outline of this width around it,
    // drawn a hair above the fill so the two do not flicker.
    private const float OutlineWidth = 0.09f;
    private const float OutlineLift = 0.005f;
    private const float OutlineStrength = 2.2f;

    // How see-through the tiles are (0 is invisible, 1 is solid).
    private const float LevelTileAlpha = 0.4f;
    private const float BrushTileAlpha = 0.3f;

    // The brush preview lies just above the level tiles, in the picked plot's colour
    // mixed halfway to white, or in plain white when no plot is picked.
    private const float BrushLiftAboveTiles = 0.02f;
    private const float BrushWhiteMix = 0.5f;

    // The level tiles are spread over the whole island in one mesh. Bounds that cover
    // the world keep Unity from ever deciding the mesh is out of view.
    private static readonly Bounds wholeWorld = new Bounds(new Vector3(512f, 0f, 512f), new Vector3(2048f, 512f, 2048f));

    private Material material;
    private bool materialFailed;

    private Mesh levelMesh;
    private int levelMeshVersion = -1;

    private Mesh brushMesh;
    private PainterBrush brushMeshBrush;
    private ushort brushMeshPlot;
    private bool brushMeshBuilt;

    /// <summary>
    /// Draws the level tiles and the brush preview for this frame. Call every frame the
    /// overlay should be visible. The meshes are rebuilt only when what they show changed.
    /// </summary>
    internal void Draw(PlotModule plots, PainterBrush brush, ushort pickedPlot)
    {
        if (!EnsureMaterial()) return;

        if (levelMeshVersion != LevelMap.Version)
        {
            levelMesh = BuildLevelMesh(levelMesh, plots.GetWorldSizeX());
            levelMeshVersion = LevelMap.Version;
        }
        if (!brushMeshBuilt || !brush.SameSquareAs(brushMeshBrush) || pickedPlot != brushMeshPlot)
        {
            brushMesh = BuildBrushMesh(brushMesh, plots, brush, pickedPlot);
            brushMeshBrush = brush;
            brushMeshPlot = pickedPlot;
            brushMeshBuilt = true;
        }

        Graphics.DrawMesh(levelMesh, Matrix4x4.identity, material, 0);
        Graphics.DrawMesh(brushMesh, Matrix4x4.identity, material, 0);
    }

    private static Mesh BuildLevelMesh(Mesh mesh, int worldSizeX)
    {
        var geometry = new TileGeometry();
        foreach (KeyValuePair<int, List<PlotLevel>> cell in LevelMap.Cells)
        {
            int x = cell.Key % worldSizeX;
            int z = cell.Key / worldSizeX;
            foreach (PlotLevel level in cell.Value)
            {
                // A floor handed back to the ground plot has nothing to show.
                if (level.Plot == PlotLevel.BackToGround) continue;

                Color color = PlotUtil.GetDefaultPlotColorById(level.Plot);
                color.a = LevelTileAlpha;
                geometry.AddTile(x, z, level.FromHeight + TileLift, color);
            }
        }
        return geometry.WriteTo(mesh);
    }

    private static Mesh BuildBrushMesh(Mesh mesh, PlotModule plots, PainterBrush brush, ushort pickedPlot)
    {
        var geometry = new TileGeometry();
        Color color = pickedPlot != 0
            ? Color.Lerp(PlotUtil.GetDefaultPlotColorById(pickedPlot), Color.white, BrushWhiteMix)
            : Color.white;
        color.a = BrushTileAlpha;
        foreach (Vector2Int cell in brush.Cells(plots))
        {
            geometry.AddTile(cell.x, cell.y, brush.Height + TileLift + BrushLiftAboveTiles, color);
        }
        return geometry.WriteTo(mesh);
    }

    private bool EnsureMaterial()
    {
        if (material != null) return true;
        if (materialFailed) return false;

        Shader shader = Shader.Find(ShaderName);
        if (shader == null)
        {
            materialFailed = true;
            GameHook.Log.LogError("Overlay switched off: Unity's '" + ShaderName + "' shader, which it is drawn with, was not found");
            return false;
        }

        // See-through, visible from both sides, and hidden behind walls and floors like
        // any other object, without itself hiding anything.
        material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        material.SetInt("_Cull", (int)CullMode.Off);
        material.SetInt("_ZWrite", 0);
        material.SetInt("_ZTest", (int)CompareFunction.LessEqual);
        return true;
    }

    // The corner points, colours and triangles of a set of tiles, gathered before they
    // are written into a mesh.
    private sealed class TileGeometry
    {
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<Color> colors = new List<Color>();
        private readonly List<int> triangles = new List<int>();

        // One tile on the cell (x, z) at height y: a fill, and four strips around its
        // edge in a stronger shade of the same colour.
        internal void AddTile(int x, int z, float y, Color fill)
        {
            float x0 = x + TileInset, x1 = x + 1f - TileInset;
            float z0 = z + TileInset, z1 = z + 1f - TileInset;
            AddQuad(x0, z0, x1, z1, y, fill);

            Color outline = fill;
            outline.a = Mathf.Min(1f, fill.a * OutlineStrength);
            float outlineY = y + OutlineLift;
            AddQuad(x0, z0, x1, z0 + OutlineWidth, outlineY, outline);
            AddQuad(x0, z1 - OutlineWidth, x1, z1, outlineY, outline);
            AddQuad(x0, z0 + OutlineWidth, x0 + OutlineWidth, z1 - OutlineWidth, outlineY, outline);
            AddQuad(x1 - OutlineWidth, z0 + OutlineWidth, x1, z1 - OutlineWidth, outlineY, outline);
        }

        internal Mesh WriteTo(Mesh mesh)
        {
            // 32-bit indices: a large village can have more tile corners than 16 bits count.
            if (mesh == null) mesh = new Mesh { hideFlags = HideFlags.HideAndDontSave, indexFormat = IndexFormat.UInt32 };
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.bounds = wholeWorld;
            return mesh;
        }

        // A flat rectangle at height y, as two triangles.
        private void AddQuad(float x0, float z0, float x1, float z1, float y, Color color)
        {
            int first = vertices.Count;
            vertices.Add(new Vector3(x0, y, z0));
            vertices.Add(new Vector3(x0, y, z1));
            vertices.Add(new Vector3(x1, y, z1));
            vertices.Add(new Vector3(x1, y, z0));
            for (int i = 0; i < 4; i++) colors.Add(color);

            triangles.Add(first);
            triangles.Add(first + 1);
            triangles.Add(first + 2);
            triangles.Add(first);
            triangles.Add(first + 2);
            triangles.Add(first + 3);
        }
    }
}
