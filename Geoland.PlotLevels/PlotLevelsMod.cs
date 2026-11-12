// SPDX-License-Identifier: MIT
// Part of the Geoland library: unofficial, fan-made mods for HavenCraft.
// HavenCraft is the work of Dionic Software; this project is not affiliated with them.
//
// PlotLevelsMod: ties the parts together for the loaded world: the pick key, the
// painter's strokes on upstairs floors, and the overlay.
//
// How it relates to the game: the game calls this class when a world has loaded and
// again every frame (see Shared\RegisterModPatch.cs). The painter's strokes reach it
// from PainterPatches. It follows the game's own painter in how a stroke chooses its
// plot. On the ground, a stroke that starts with the brush on a plot extends that plot.
// Upstairs, a stroke that starts with the brush on a section already painted on that
// floor extends that section. On bare floor there is no section to extend, and unlike
// on the ground a new plot cannot simply be created up there, because a plot needs
// ground cells to exist. So the floor is given to the plot picked beforehand with the
// pick key.
//
// The ground floor is kept strictly apart. Anything less than a set number of blocks
// above the terrain belongs to the game's flat grid and its painter, and no upper level
// is ever made there.
using System;
using Dionic.ModAPI_1;
using Geoland.Shared;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Geoland.PlotLevels;

internal sealed class PlotLevelsMod : IMod
{
    // The game's internal names for the plot painter item and for the "use item" button.
    private const string PlotPainterItem = "plotPainter";
    private const string UseItemButton = "character/useItem";

    // The hint about picking a plot first is shown at most this often, in seconds. A
    // stroke calls for it on every frame the brush moves.
    private const float HintEverySeconds = 4f;

    /// <summary>The mod of the world that is loaded now, for the painter patches. Null until a world has loaded.</summary>
    internal static PlotLevelsMod Current { get; private set; }

    private readonly LevelOverlay overlay = new LevelOverlay();

    private Game game;
    private PlotModule plots;
    private PlayerCharacterModule players;
    private PlayerInventoryModule inventories;
    private HeightAndBiomeModule terrain;
    private InputModule input;
    private object painter;

    private ushort pickedPlot;
    private ushort strokePlot;
    private float nextHintTime;
    private bool overlayFailed;

    public void OnGameLoaded(Game game)
    {
        if (!GameAccess.CanKeepLevels) return;
        try
        {
            this.game = game;
            plots = GameHook.Module<PlotModule>();
            players = GameHook.Module<PlayerCharacterModule>();
            inventories = GameHook.Module<PlayerInventoryModule>();
            terrain = GameHook.Module<HeightAndBiomeModule>();
            if (GameAccess.CanPaint)
            {
                PlayerActionModule playerActions = GameHook.Module<PlayerActionModule>();
                input = GameAccess.InputOf(playerActions);
                painter = GameAccess.FindPainter(playerActions);
            }
            Current = this;

            int withoutPlot = LevelMap.DropLevelsOfMissingPlots(plots);
            int onGroundFloor = LevelMap.DropGroundFloorLevels(plots, LowestUpperHeight);
            GameHook.Log.LogInfo("World loaded: " + LevelMap.CellCount + " cells have upper levels"
                + (withoutPlot > 0 ? "; removed " + withoutPlot + " levels whose plot no longer exists" : "")
                + (onGroundFloor > 0 ? "; removed " + onGroundFloor + " levels on the ground floor, left by an early version" : "")
                + (painter == null ? "; the plot painter was not found, so levels cannot be painted" : ""));

            // The main menu loads a world too, with no player in it. Say nothing there.
            if (players.TryGetMainPlayer() == null) return;
            Notify("Geoland Plot Levels " + Plugin.Version + " active. With the plot painter out: " + Plugin.PickKey.Value
                + " picks the plot you aim at, then click an upstairs floor to give it that floor.");
            if (onGroundFloor > 0)
            {
                Notify("Plot Levels: removed " + onGroundFloor + " old ground-floor levels. The ground floor follows the normal plots again.");
            }
        }
        catch (Exception e)
        {
            GameHook.Log.LogError("Could not set up plot levels for this world: " + e);
        }
    }

    public void UpdateFrame(Game game)
    {
        // Everything done per frame belongs to the plot painter: with another item in
        // hand there is nothing to pick and nothing to draw.
        if (Current != this || painter == null || players.TryGetMainPlayer() == null) return;
        if (!PlotPainterSelected()) return;

        PainterBrush brush;
        try
        {
            brush = PainterBrush.Read(painter, plots);
        }
        catch (Exception e)
        {
            GameHook.Log.LogError("Could not read the plot painter's brush: " + e);
            return;
        }

        if (PickKeyPressed())
        {
            try
            {
                Pick(brush);
            }
            catch (Exception e)
            {
                GameHook.Log.LogError("Picking a plot failed: " + e);
                Notify("Plot Levels: that failed, see the BepInEx log.");
            }
        }
        DrawOverlay(brush);
    }

    /// <summary>
    /// True when the point is on a floor above the ground floor: far enough above the
    /// terrain that an upper level may start there.
    /// </summary>
    internal bool IsAboveGroundFloor(Vector3 point)
    {
        if (terrain == null) return false;
        return LevelMap.HeightOf(point) >= LowestUpperHeight(point.x, point.z);
    }

    /// <summary>
    /// Paints or erases an upper level under the painter's brush. Called in place of the
    /// painter's own "apply the brush" step while it is aimed upstairs: on the click,
    /// and again whenever the brush moves with the button held. It therefore only tells
    /// the player something when cells actually changed.
    /// </summary>
    internal void PainterStroke(bool erase)
    {
        PainterBrush brush = PainterBrush.Read(painter, plots);
        if (!brush.IsValid) return;

        if (erase)
        {
            int handedBack = LevelMap.Erase(plots, brush.Cells(plots), brush.Height);
            if (handedBack > 0)
            {
                GameHook.Log.LogInfo($"Painter erase at {brush.Describe()}: height {brush.Height}, brush {brush.Size}, cells changed {handedBack}");
                Notify(handedBack + " cells at height " + brush.Height + " went back to the ground plot.");
            }
            return;
        }

        // The plot is chosen when the button goes down and kept for the whole stroke,
        // as the game's painter does it.
        bool strokeStarts = input != null && input.WasPressedThisFrame(UseItemButton);
        if (strokeStarts || strokePlot == 0 || plots.TryGetPlotInfo(strokePlot) == null)
        {
            strokePlot = PlotForStroke(brush);
        }
        if (strokePlot == 0)
        {
            if (Time.unscaledTime >= nextHintTime)
            {
                nextHintTime = Time.unscaledTime + HintEverySeconds;
                Notify("To start a section on this floor, first aim at the plot it should belong to (its stairs, say) and press "
                    + Plugin.PickKey.Value + ".");
            }
            return;
        }

        int given = LevelMap.Paint(plots, brush.Cells(plots), brush.Height, strokePlot);
        if (given > 0)
        {
            GameHook.Log.LogInfo($"Painter paint at {brush.Describe()}: height {brush.Height}, brush {brush.Size}, plot {strokePlot}, cells changed {given}");
            Notify(given + " cells, from height " + brush.Height + " up, now belong to " + Describe(strokePlot) + ".");
        }
    }

    // The lowest height an upper level may start at above this spot.
    private int LowestUpperHeight(float x, float z)
    {
        float ground = terrain.SampleHeight(new Vector2(x, z));
        return Mathf.FloorToInt(ground) + Plugin.MinHeightAboveGround.Value;
    }

    // The same for a cell, taken at the cell's centre.
    private int LowestUpperHeight(int cellX, int cellZ)
    {
        return LowestUpperHeight(cellX + 0.5f, cellZ + 0.5f);
    }

    // The plot a stroke starting here paints: the upstairs section under most of the
    // brush if it touches one, otherwise the picked plot (0 if none is picked).
    // Extending a section also makes its plot the picked one.
    private ushort PlotForStroke(PainterBrush brush)
    {
        ushort section = LevelMap.MainPlotStartingAt(plots, brush.Cells(plots), brush.Height);
        if (section != 0 && plots.TryGetPlotInfo(section) != null)
        {
            pickedPlot = section;
            return section;
        }
        ForgetPickedPlotIfDeleted();
        return pickedPlot;
    }

    private void Pick(PainterBrush brush)
    {
        if (!brush.IsValid)
        {
            Notify("Aim the plot painter at a plot to pick it.");
            return;
        }
        ushort aimedAt = plots.GetPlot(brush.Point);
        GameHook.Log.LogInfo($"Pick at {brush.Describe()}: height level {brush.Height}, plot {aimedAt}, ground plot {LevelMap.GroundPlot(plots, brush.Point)}");
        if (aimedAt == PlotModule.wildernessPlotID || plots.TryGetPlotInfo(aimedAt) == null)
        {
            Notify("No plot there to pick (height " + brush.Height + ").");
            return;
        }
        pickedPlot = aimedAt;
        Notify("Picked " + Describe(aimedAt) + ". Now click an upstairs floor with the plot painter to give it that floor.");
    }

    private void DrawOverlay(PainterBrush brush)
    {
        if (overlayFailed) return;
        try
        {
            ForgetPickedPlotIfDeleted();
            overlay.Draw(plots, brush, pickedPlot);
        }
        catch (Exception e)
        {
            // Painting still works without the overlay, so only the overlay is switched off.
            overlayFailed = true;
            GameHook.Log.LogError("Overlay switched off after an error: " + e);
        }
    }

    private void ForgetPickedPlotIfDeleted()
    {
        if (pickedPlot != 0 && plots.TryGetPlotInfo(pickedPlot) == null) pickedPlot = 0;
    }

    // The pick key counts only while the keyboard belongs to the game world: not while
    // typing in chat or another text field, and not while a menu is open.
    private bool PickKeyPressed()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard[Plugin.PickKey.Value].wasPressedThisFrame) return false;
        if (input != null && (input.IsTextFieldActive() || input.modalManager.IsGameModal())) return false;
        return true;
    }

    private bool PlotPainterSelected()
    {
        try
        {
            ItemStack stack = inventories?.TryGetMainPlayerInventory()?.GetSelectedDashboardItem();
            return stack != null && stack.item != null && stack.item.itemName == PlotPainterItem;
        }
        catch (Exception)
        {
            // Asked every frame, including moments when the inventory is being rebuilt.
            // Not selected is the safe answer; the next frame asks again.
            return false;
        }
    }

    // A plot as the player knows it: its name if it has one, its kind, and its number.
    private string Describe(ushort plotId)
    {
        PlotInfo info = plots.TryGetPlotInfo(plotId);
        if (info == null || plotId == PlotModule.wildernessPlotID) return "no plot";

        string kind = info.TryGetConfig()?.GetPlotConfigType();
        string kindName = kind != null && PlotConfigNames.HasNameInGame(kind) ? PlotConfigNames.GetNameInGame(kind) : (kind ?? "plot");
        return string.IsNullOrEmpty(info.name)
            ? kindName + " #" + plotId
            : "\"" + info.name + "\" (" + kindName + ") #" + plotId;
    }

    private void Notify(string text)
    {
        game?.notifications.Show(text);
    }
}
