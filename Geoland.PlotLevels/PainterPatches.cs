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
//  - The second step is taken over while the brush is upstairs: instead of changing
//    the ground grid, the stroke paints or erases an upper level (PlotLevelsMod).
// Aimed at the ground floor or the terrain, both steps run exactly as the game made
// them.
using System;
using System.Reflection;
using Geoland.Shared;
using HarmonyLib;
using UnityEngine;

namespace Geoland.PlotLevels;

internal static class PainterPatches
{
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
            if (!PainterAim.IsUpstairs || mod == null) return true;
            try
            {
                mod.PainterStroke(erase: GameAccess.PainterIsErasing(__instance));
            }
            catch (Exception e)
            {
                GameHook.Log.LogError("Painting an upper level failed: " + e);
            }

            // Whatever happened, an upstairs click must never change the ground grid.
            return false;
        }
    }
}
