// SPDX-License-Identifier: MIT
// Part of the Geoland library: unofficial, fan-made mods for HavenCraft.
// HavenCraft is the work of Dionic Software; this project is not affiliated with them.
//
// PlantingMod: planting mode itself. Reads the toggle key and the selected hotbar item,
// keeps the ghost preview up to date, and plants when the click arrives.
//
// How it relates to the game: the game calls this class when a world has loaded and
// again every frame (see Shared\RegisterModPatch.cs). The click reaches it from
// PlantClickPatch. Planting uses the game's own calls throughout: the player's inventory
// to pay for the plant, and the world object module to place it, which is what the game's
// own wild-plant spawner uses. A planted object is therefore an ordinary one.
using System;
using Dionic.ModAPI_1;
using Geoland.Shared;
using UnityEngine.InputSystem;

namespace Geoland.Planting;

internal sealed class PlantingMod : IMod
{
    /// <summary>The mod of the world that is loaded now, for the click patches. Null until a world has loaded.</summary>
    internal static PlantingMod Current { get; private set; }

    private Game game;
    private WorldObjectModule worldObjects;
    private PlayerCharacterModule players;
    private PlayerInventoryModule inventories;
    private InputModule input;
    private SpotFinder spots;
    private PlantGhost ghost;

    private bool plantingMode;
    private bool ghostFailed;

    public void OnGameLoaded(Game game)
    {
        if (!GameAccess.Available) return;
        try
        {
            this.game = game;
            plantingMode = false;
            worldObjects = GameHook.Module<WorldObjectModule>();
            players = GameHook.Module<PlayerCharacterModule>();
            inventories = GameHook.Module<PlayerInventoryModule>();
            input = GameAccess.InputOf(GameHook.Module<PlayerActionModule>());
            spots = new SpotFinder(worldObjects, GameHook.Module<HeightAndBiomeModule>(), players);
            ghost = new PlantGhost(worldObjects);
            Current = this;
            GameHook.Log.LogInfo("Planting is ready for this world");

            // The main menu loads a world too, with no player in it. Say nothing there.
            if (players.TryGetMainPlayer() != null)
            {
                game.notifications.Show("Geoland Planting " + Plugin.Version + " active. "
                    + Plugin.ToggleKey.Value + " toggles planting mode.");
            }
        }
        catch (Exception e)
        {
            GameHook.Log.LogError("Could not set up planting for this world: " + e);
        }
    }

    public void UpdateFrame(Game game)
    {
        if (Current != this || players.TryGetMainPlayer() == null) return;

        if (ToggleKeyPressed())
        {
            plantingMode = !plantingMode;
            GameHook.Log.LogInfo("Planting mode " + (plantingMode ? "on" : "off"));
            if (!plantingMode) Notify("Planting OFF.");
            else if (SelectedPlantable() != null) Notify("Planting ON. Click to plant where you aim.");
            else Notify("Planting ON. Select " + Plantable.ItemsForPlayer + " in your hotbar, then click to plant.");
        }

        if (ghostFailed) return;
        try
        {
            UpdateGhost();
        }
        catch (Exception e)
        {
            // Planting still works without the preview, so only the ghost is switched off.
            ghostFailed = true;
            GameHook.Log.LogError("Ghost preview switched off after an error: " + e);
        }
    }

    /// <summary>Shows a notification to the player.</summary>
    internal void Notify(string text)
    {
        game?.notifications.Show(text);
    }

    /// <summary>
    /// True when this click on the game world is a planting click and the game should not
    /// act on it: planting mode is on, it is the "use item" button, and a plantable item
    /// is selected.
    /// </summary>
    internal bool ShouldTakeClick(PlayerActionConfirmType confirmType)
    {
        return plantingMode && confirmType == PlayerActionConfirmType.UseItem && SelectedPlantable() != null;
    }

    /// <summary>True when the click that just happened landed on the hotbar or other interface, not on the world.</summary>
    internal bool ClickWasOnInterface()
    {
        return input != null && input.WasUIInteractedWithInThisFrame();
    }

    /// <summary>
    /// Plants what the selected item plants at the spot being aimed at, for one of the item.
    /// </summary>
    /// <returns>What to tell the player: that it was planted, or why it was not.</returns>
    internal string TryPlant()
    {
        Plantable plantable = SelectedPlantable();
        if (plantable == null)
        {
            return "Select " + Plantable.ItemsForPlayer + " in your hotbar to plant.";
        }
        PlayerInventory inventory = inventories.TryGetMainPlayerInventory();
        ItemStack selected = inventory.GetSelectedDashboardItem();
        ItemContainer container = inventory.TryGetItemContainer();
        if (container == null)
        {
            return "Planting: inventory not available.";
        }

        // The preview may be working from a remembered answer; a real planting looks again.
        spots.ForgetLastScan();
        if (!spots.TryFind(plantable, out GridTransform spot, out string problem) || problem != null)
        {
            return problem;
        }

        // The spot is good: pay one of the item, then plant.
        if (!container.TryRemoveItemFully(selected.item, 1))
        {
            return "No " + plantable.ItemPlural + " left to plant.";
        }
        worldObjects.SpawnWorldObject(plantable.ObjectName, spot);
        spots.TurnNextPlant();
        spots.ForgetLastScan();   // the new plant now stands on this cell

        int left = container.GetItemAmountInContainer(plantable.ItemName);
        GameHook.Log.LogInfo($"Planted {plantable.ObjectName} at grid ({spot.pos.x}, {spot.pos.y}, {spot.pos.z}), {plantable.ItemPlural} left {left}");
        return $"{plantable.Label} planted. {Capitalised(plantable.ItemPlural)} left: {left}.";
    }

    // The toggle key counts only while the keyboard belongs to the game world: not while
    // typing in chat or another text field, and not while a menu is open. The game does
    // not filter raw key presses for us, so its input module is asked.
    private bool ToggleKeyPressed()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard[Plugin.ToggleKey.Value].wasPressedThisFrame) return false;
        if (input != null && (input.IsTextFieldActive() || input.modalManager.IsGameModal())) return false;
        return true;
    }

    private void UpdateGhost()
    {
        Plantable plantable = plantingMode ? SelectedPlantable() : null;
        if (plantable == null || !spots.TryFind(plantable, out GridTransform spot, out string problem))
        {
            ghost.Hide();
            return;
        }
        ghost.Show(plantable, spot, spotIsGood: problem == null);
    }

    // What the selected hotbar item plants, or null if it plants nothing.
    private Plantable SelectedPlantable()
    {
        try
        {
            ItemStack stack = inventories?.TryGetMainPlayerInventory()?.GetSelectedDashboardItem();
            if (stack == null || stack.item == null || stack.amount < 1) return null;
            return Plantable.ForItem(stack.item.itemName);
        }
        catch (Exception)
        {
            // Asked every frame, including moments when the inventory is being rebuilt.
            // Nothing selected is the safe answer; the next frame asks again.
            return null;
        }
    }

    private static string Capitalised(string word)
    {
        return char.ToUpper(word[0]) + word.Substring(1);
    }
}
