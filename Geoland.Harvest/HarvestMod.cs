// SPDX-License-Identifier: MIT
// Part of the Geoland library: unofficial, fan-made mods for HavenCraft.
// HavenCraft is the work of Dionic Software; this project is not affiliated with them.
//
// HarvestMod: what the plugin does when a world has loaded, and every frame after.
//
// How it relates to the game: the game calls every registered mod when a world has
// finished loading and again every frame (see Shared\RegisterModPatch.cs). The plugin
// needs both. The first tells it that the work planner is ready to be told about
// changes. The second is its clock for noticing items that have grown back.
using System;
using Dionic.ModAPI_1;
using Geoland.Shared;

namespace Geoland.Harvest;

internal sealed class HarvestMod : IMod
{
    public void OnGameLoaded(Game game)
    {
        HarvestWork.WorldLoaded();
        try
        {
            // The main menu loads a world too, with no player in it. Say nothing there.
            if (GameHook.Module<PlayerCharacterModule>().TryGetMainPlayer() != null)
            {
                game.notifications.Show("Geoland Harvest " + Plugin.Version + " active. Farm plots have "
                    + HarvestCatalog.OrderCount + " harvest work orders.");
            }
        }
        catch (Exception e)
        {
            GameHook.Log.LogError("Could not show the Harvest notice: " + e);
        }
    }

    public void UpdateFrame(Game game)
    {
        HarvestWork.LookForRegrownItems();
    }
}
