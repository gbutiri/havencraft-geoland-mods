// SPDX-License-Identifier: MIT
// Part of the Geoland library: unofficial, fan-made mods for HavenCraft.
// HavenCraft is the work of Dionic Software; this project is not affiliated with them.
//
// RecruitWishesMod: what the plugin does each time a world has loaded.
//
// How it relates to the game: the game calls every registered mod when a world has
// finished loading (see Shared\RegisterModPatch.cs). Item names can only be checked
// against a loaded world's item module, so this is where the pool of wishes is read from
// the settings. The wishes themselves are made later, when the game asks what each
// waiting traveler wants (WishedPricePatch).
using System;
using Dionic.ModAPI_1;
using Geoland.Shared;

namespace Geoland.RecruitWishes;

internal sealed class RecruitWishesMod : IMod
{
    public void OnGameLoaded(Game game)
    {
        try
        {
            TravelerWishes.LoadPool(GameHook.Module<ItemModule>());

            // The main menu loads a world too, with no player in it. Say nothing there.
            bool playerIsInWorld = GameHook.Module<PlayerCharacterModule>().TryGetMainPlayer() != null;
            if (playerIsInWorld && TravelerWishes.Ready)
            {
                game.notifications.Show("Geoland Recruit Wishes " + Plugin.Version
                    + " active. Once the usual joining prices are used up, each traveler has a wish of their own.");
            }
        }
        catch (Exception e)
        {
            GameHook.Log.LogError("Could not set up traveler wishes for this world: " + e);
        }
    }

    public void UpdateFrame(Game game)
    {
        // Nothing to do every frame: the plugin only answers when the game asks.
    }
}
