// SPDX-License-Identifier: MIT
// Part of the Geoland library: unofficial, fan-made mods for HavenCraft.
// HavenCraft is the work of Dionic Software; this project is not affiliated with them.
//
// GameAccess: the one place where this plugin reaches into private parts of the game.
//
// How it relates to the game: the reaches fall into two groups, and the plugin can lose
// one without losing the other.
//  - Keeping levels. The plot module's constructor and the start of loading a save are
//    patched so that the levels are stored in, and read from, the save file. Without
//    these the plugin switches itself off completely: levels that cannot be saved must
//    not be made.
//  - Painting levels. The plot painter is a tool whose class is not public. The plugin
//    reads where its brush is, replaces its aim upstairs and takes over its "apply the
//    brush" step. Without these, levels already in a save are still honoured; they just
//    cannot be changed until the plugin is updated.
// Everything is found by name once, at start-up, and the log says what is missing.
using System;
using System.Collections;
using System.Reflection;
using Geoland.Shared;
using UnityEngine;

namespace Geoland.PlotLevels;

internal static class GameAccess
{
    private const string NeededForKeeping = "keeping plot levels in the save file";
    private const string NeededForPainting = "painting plot levels";

    private const string PainterTypeName = "PlotPaintPlayerAction";

    private static FieldInfo inputModule;
    private static FieldInfo itemActions;
    private static FieldInfo painterHasValidBrush;
    private static FieldInfo painterBrushSize;
    private static FieldInfo painterBrushCorner;
    private static FieldInfo painterBrushPoint;
    private static MethodInfo painterIsErasing;

    /// <summary>Creates the plot module, once for each world.</summary>
    internal static ConstructorInfo PlotModuleConstructor { get; private set; }

    /// <summary>Starts loading a save file. Its argument is the file's path.</summary>
    internal static MethodInfo LoadStarted { get; private set; }

    /// <summary>The painter's "work out where the brush is" step, run every frame the painter is in use.</summary>
    internal static MethodInfo PainterUpdateBrush { get; private set; }

    /// <summary>The painter's "apply the brush" step, run on a click and as a stroke is dragged.</summary>
    internal static MethodInfo PainterApplyBrush { get; private set; }

    /// <summary>False when levels could not be stored in the save file. The whole plugin is then off.</summary>
    internal static bool CanKeepLevels { get; private set; }

    /// <summary>False when the plot painter cannot be used on upstairs floors.</summary>
    internal static bool CanPaint { get; private set; }

    /// <summary>Looks everything up. Called once, when the plugin starts.</summary>
    internal static void Resolve()
    {
        PlotModuleConstructor = GameMembers.Constructor(typeof(PlotModule), NeededForKeeping);
        Type loadingManager = GameMembers.HiddenType("LoadingManager", NeededForKeeping);
        if (loadingManager != null) LoadStarted = GameMembers.Method(loadingManager, "ExecuteLoad", NeededForKeeping);
        CanKeepLevels = PlotModuleConstructor != null && LoadStarted != null;

        inputModule = GameMembers.Field(typeof(PlayerActionModule), "inputModule", typeof(InputModule), NeededForPainting);
        itemActions = GameMembers.Field(typeof(PlayerActionModule), "allItemActions", typeof(IEnumerable), NeededForPainting);

        Type painter = GameMembers.HiddenType(PainterTypeName, NeededForPainting);
        if (painter != null)
        {
            painterHasValidBrush = GameMembers.Field(painter, "validBrushPos", typeof(bool), NeededForPainting);
            painterBrushSize = GameMembers.Field(painter, "brushSize", typeof(int), NeededForPainting);
            painterBrushCorner = GameMembers.Field(painter, "brushPosInt", typeof(Vector3Int), NeededForPainting);
            painterBrushPoint = GameMembers.Field(painter, "brushPos", typeof(Vector3), NeededForPainting);
            painterIsErasing = GameMembers.Method(painter, "IsInEraseMode", NeededForPainting);
            PainterApplyBrush = GameMembers.Method(painter, "Paint", NeededForPainting);
        }
        Type painterUtil = GameMembers.HiddenType("PlotPaintUtil", NeededForPainting);
        if (painterUtil != null) PainterUpdateBrush = GameMembers.Method(painterUtil, "UpdateBrushPosition", NeededForPainting);

        CanPaint = CanKeepLevels
            && inputModule != null && itemActions != null
            && painterHasValidBrush != null && painterBrushSize != null && painterBrushCorner != null
            && painterBrushPoint != null && painterIsErasing != null
            && PainterApplyBrush != null && PainterUpdateBrush != null;
    }

    /// <summary>The game's input module, as held by the player action module.</summary>
    internal static InputModule InputOf(PlayerActionModule playerActions)
    {
        return (InputModule)inputModule.GetValue(playerActions);
    }

    /// <summary>The plot painter tool of this world, or null if the game did not create one.</summary>
    internal static object FindPainter(PlayerActionModule playerActions)
    {
        foreach (object action in (IEnumerable)itemActions.GetValue(playerActions))
        {
            if (action != null && action.GetType().Name == PainterTypeName) return action;
        }
        return null;
    }

    /// <summary>True when the painter is aimed at something within its reach.</summary>
    internal static bool PainterHasValidBrush(object painter)
    {
        return (bool)painterHasValidBrush.GetValue(painter);
    }

    /// <summary>The side of the painter's square brush, in cells.</summary>
    internal static int PainterBrushSize(object painter)
    {
        return (int)painterBrushSize.GetValue(painter);
    }

    /// <summary>The cell at the brush's lowest corner.</summary>
    internal static Vector3Int PainterBrushCorner(object painter)
    {
        return (Vector3Int)painterBrushCorner.GetValue(painter);
    }

    /// <summary>The exact point the painter is aimed at.</summary>
    internal static Vector3 PainterBrushPoint(object painter)
    {
        return (Vector3)painterBrushPoint.GetValue(painter);
    }

    /// <summary>True while the player holds the painter's erase button.</summary>
    internal static bool PainterIsErasing(object painter)
    {
        return (bool)painterIsErasing.Invoke(painter, null);
    }
}
