// SPDX-License-Identifier: MIT
// Part of the Geoland library: unofficial, fan-made mods for HavenCraft.
// HavenCraft is the work of Dionic Software; this project is not affiliated with them.
//
// PainterPatches: the two places where the game's plot painter is changed.
//
// How it relates to the game: every frame the painter is in use it does two things.
// It works out where its brush is, and, on a click or while a stroke is dragged, it
// applies the brush to the game's flat plot grid.
//  - After the first step, the brush is put on the upstairs floor being aimed at, if
//    there is one (PainterAim).
//  - The second step is taken over for a stroke that began upstairs: instead of
//    changing the ground grid, the stroke paints or erases an upper level
//    (PlotLevelsMod).
// A stroke that began on the ground floor or the terrain runs exactly as the game made
// it.
//
// A stroke stays on the kind of floor it began on. The game chooses a stroke's plot
// once, at the click, from its ground grid. If the takeover were decided frame by frame,
// a stroke begun upstairs and dragged onto a wall or past the floor's edge would be
// handed back to the game half way, and the game would paint or erase the ground with a
// plot chosen for another purpose. So a stroke that began upstairs does nothing while
// the brush is off the upstairs floors, and a stroke that began on the ground does
// nothing while the brush is on one.
using System;
using System.Reflection;
using Geoland.Shared;
using HarmonyLib;
using UnityEngine;

namespace Geoland.PlotLevels;

internal static class PainterPatches
{
    private enum StrokeOn
    {
        NoStroke,
        Ground,
        Upstairs,
    }

    // The stroke in progress, and the last frame in which the painter was in use.
    private static StrokeOn stroke;
    private static int lastPainterFrame = -1;

    [HarmonyPatch]
    private static class BrushPositionWorkedOut
    {
        private static bool Prepare()
        {
            return GameAccess.CanPaint;
        }

        private static MethodBase TargetMethod()
        {
            return GameAccess.PainterUpdateBrush;
        }

        // The parameter names are the game's; that is how the patch is handed them.
        private static void Postfix(ref int brushSize, ref Vector3 brushPos, ref Vector3Int brushPosInt, ref int lastBrushPosUpdateFrame,
            ref bool validBrushPos, GameObject brushDecalGameObject, RaycastCollisionModule raycastCollisionModule)
        {
            PainterAim.PutBrushOnFloor(brushSize, ref brushPos, ref brushPosInt, ref lastBrushPosUpdateFrame,
                ref validBrushPos, brushDecalGameObject, raycastCollisionModule);

            // The stroke is over once neither the paint nor the erase button is held.
            lastPainterFrame = Time.frameCount;
            PlotLevelsMod mod = PlotLevelsMod.Current;
            if (stroke != StrokeOn.NoStroke && (mod == null || !mod.StrokeButtonHeld()))
            {
                stroke = StrokeOn.NoStroke;
                mod?.StrokeEnded();
            }
        }
    }

    [HarmonyPatch]
    private static class BrushApplied
    {
        private static bool Prepare()
        {
            return GameAccess.CanPaint;
        }

        private static MethodBase TargetMethod()
        {
            return GameAccess.PainterApplyBrush;
        }

        // Returning false keeps the game's own painting from running.
        private static bool Prefix(object __instance)
        {
            PlotLevelsMod mod = PlotLevelsMod.Current;
            if (mod == null) return true;

            bool upstairs = PainterAim.IsUpstairs;
            try
            {
                // A new stroke: none was being followed, or the painter was put away in
                // between, or a button went down this frame. A button pressed while the
                // other is still held is not a new stroke: the game keeps the plot it
                // chose at the first click, so the stroke keeps its kind as well.
                bool painterWasAway = Time.frameCount > lastPainterFrame + 1;
                bool pressedAlone = mod.StrokeStartsThisFrame() && !mod.BothStrokeButtonsHeld();
                if (stroke == StrokeOn.NoStroke || painterWasAway || pressedAlone)
                {
                    stroke = upstairs ? StrokeOn.Upstairs : StrokeOn.Ground;
                }
                if (stroke == StrokeOn.Ground) return !upstairs;
                if (upstairs) mod.PainterStroke(erase: GameAccess.PainterIsErasing(__instance));
            }
            catch (Exception e)
            {
                GameHook.Log.LogError("Painting an upper level failed: " + e);
            }

            // Whatever happened, a stroke that is not a ground stroke must never change
            // the ground grid.
            return false;
        }
    }
}
