// SPDX-License-Identifier: MIT
// Part of the Geoland library: unofficial, fan-made mods for HavenCraft.
// HavenCraft is the work of Dionic Software; this project is not affiliated with them.
//
// PainterAim: finds the floor the player is looking at, and puts the plot painter's
// brush on it.
//
// How it relates to the game: the plot painter aims along a ray from the centre of the
// screen, but accepts only the terrain as a target. Floors and other building pieces
// are passed over. Upstairs, its brush therefore lands on the ground underneath the
// building, at ground height, and a click paints the ground plot through the floor.
//
// This class looks along the same ray and also accepts the top of a building piece.
// When that is a floor above the ground floor, it replaces the brush position the
// painter worked out with the spot on that floor, using the painter's own arithmetic
// for turning an aimed point into a brush corner. The painter's brush marker then sits
// on the floor, and everything that reads the brush sees the upstairs spot. Aimed at
// the ground floor or the terrain, nothing is replaced.
using System;
using Geoland.Shared;
using UnityEngine;

namespace Geoland.PlotLevels;

internal static class PainterAim
{
    // Unity layers, as the game uses them: the terrain, and the boxes that building
    // pieces are clicked by.
    private const int TerrainLayer = 0;
    private const int BuildingPieceLayer = 3;

    // How far the painter reaches, as in the game.
    private const float MaxReach = 12f;

    // A surface counts as a floor when it faces up at least this much. Walls and the
    // sides of things do not.
    private const float FloorNormalMinY = 0.5f;

    private static Vector3Int lastCorner;
    private static bool wasUpstairs;
    private static int lastMoveFrame;
    private static bool failed;

    /// <summary>
    /// True when, this frame, the painter is aimed at a floor above the ground floor and
    /// its brush has been put there.
    /// </summary>
    internal static bool IsUpstairs { get; private set; }

    /// <summary>
    /// Runs right after the painter has worked out its brush position. The arguments are
    /// the painter's own values; changing them changes the painter.
    /// </summary>
    internal static void PutBrushOnFloor(int brushSize, ref Vector3 brushPos, ref Vector3Int brushPosInt,
        ref int lastBrushPosUpdateFrame, ref bool validBrushPos, GameObject brushMarker, RaycastCollisionModule raycasts)
    {
        IsUpstairs = false;
        PlotLevelsMod mod = PlotLevelsMod.Current;
        if (failed || mod == null) return;
        try
        {
            if (!TryFindFloor(raycasts, out Vector3 point) || !mod.IsAboveGroundFloor(point))
            {
                wasUpstairs = false;
                return;
            }

            // The painter's own arithmetic: the brush is centred on the aimed point.
            var half = new Vector3(brushSize / 2f, brushSize / 2f, brushSize / 2f);
            Vector3 lowestCorner = point - half;
            var corner = new Vector3Int(Mathf.RoundToInt(lowestCorner.x), Mathf.RoundToInt(lowestCorner.y), Mathf.RoundToInt(lowestCorner.z));

            // The painter applies a dragged stroke only in frames where the brush moved
            // to a new cell, which it recognises by this frame number.
            if (corner != lastCorner || !wasUpstairs)
            {
                lastCorner = corner;
                lastMoveFrame = Time.frameCount;
            }
            wasUpstairs = true;

            brushPos = point;
            brushPosInt = corner;
            lastBrushPosUpdateFrame = lastMoveFrame;
            brushMarker.transform.position = (Vector3)corner + half;
            validBrushPos = true;
            IsUpstairs = true;
        }
        catch (Exception e)
        {
            // The painter still works on the ground without this, so only upstairs
            // aiming is switched off.
            failed = true;
            IsUpstairs = false;
            GameHook.Log.LogError("Upstairs aiming switched off after an error: " + e);
        }
    }

    // The first solid thing under the crosshair, if it is within reach and is the top
    // of something. Anything else in the way, such as a villager, is looked past.
    private static bool TryFindFloor(RaycastCollisionModule raycasts, out Vector3 point)
    {
        point = default;
        foreach (RaycastHit hit in raycasts.screenCenterRaycaster.GetRaycastHitsOrdered())
        {
            int layer = hit.collider.gameObject.layer;
            if (layer != TerrainLayer && layer != BuildingPieceLayer) continue;
            if (hit.distance > MaxReach) return false;
            if (hit.normal.y < FloorNormalMinY) return false;

            point = hit.point;
            return true;
        }
        return false;
    }
}
