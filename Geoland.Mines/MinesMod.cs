// SPDX-License-Identifier: MIT
// Part of the Geoland library: unofficial, fan-made mods for HavenCraft.
// HavenCraft is the work of Dionic Software; this project is not affiliated with them.
//
// MinesMod: what the plugin does when a world has loaded, and its once-a-second look at
// the mines.
//
// How it relates to the game: the game calls this class when a world has finished
// loading and again every frame (see Shared\RegisterModPatch.cs), and gives each loaded
// world a new one. On "world loaded" the settings are checked against the running game
// and the player is told what a mine is, if the world has a Miner Zone to put one on.
// The every-frame call only compares the clock; about once a second it lets MineKeeper
// look at the mines. If that look ever fails, the error is written to the log
// once and the plugin stops for this world; the game goes on as it does without it.
using System;
using Dionic.ModAPI_1;
using Geoland.Shared;
using UnityEngine;

namespace Geoland.Mines;

internal sealed class MinesMod : IMod
{
    // How often the plugin looks at the mines, in seconds.
    private const float LookSeconds = 1f;

    private MineKeeper keeper;
    private PlayerCharacterModule players;
    private float nextLook;
    private bool stopped;

    public void OnGameLoaded(Game game)
    {
        // Nothing of the last world may be carried into this one.
        keeper = null;
        stopped = false;
        if (!Plugin.Enabled.Value || !GameAccess.Available) return;
        try
        {
            WorldObjectModule worldObjects = GameHook.Module<WorldObjectModule>();
            string rockName = RockChoice.FromSettings(worldObjects);
            if (rockName == null) return;

            int waitTicks = Plugin.WaitTicks.Value;
            if (waitTicks < MineKeeper.ShortestWaitTicks)
            {
                GameHook.Log.LogWarning("The WaitTicks setting is " + waitTicks + "; using " + MineKeeper.ShortestWaitTicks + ".");
                waitTicks = MineKeeper.ShortestWaitTicks;
            }

            players = GameHook.Module<PlayerCharacterModule>();
            keeper = new MineKeeper(GameHook.Module<PlotModule>(), worldObjects, GameHook.Module<SimulationModule>(),
                rockName, waitTicks);
            nextLook = Time.unscaledTime + LookSeconds;
            GameHook.Log.LogInfo("Mines are ready for this world: " + rockName + " after " + waitTicks + " ticks");

            // The main menu loads a world too, with no player in it. Say nothing there,
            // and nothing in a world that has no Miner Zone to put a mine on.
            if (players.TryGetMainPlayer() != null && keeper.AnyMinerZone())
            {
                game.notifications.Show("Geoland Mines " + Plugin.Version
                    + " active. A Tree Stake placed on a Miner Zone is a mine: a rock appears in it for your miners, "
                    + "and a new one some time after it is mined out.");
            }
        }
        catch (Exception e)
        {
            keeper = null;
            GameHook.Log.LogError("Could not set up mines for this world: " + e);
        }
    }

    public void UpdateFrame(Game game)
    {
        if (keeper == null || stopped || Time.unscaledTime < nextLook) return;
        nextLook = Time.unscaledTime + LookSeconds;
        try
        {
            if (players.TryGetMainPlayer() == null) return;
            keeper.LookAtMines();
        }
        catch (Exception e)
        {
            stopped = true;
            GameHook.Log.LogError("Mines switched off for this world after an error: " + e);
        }
    }
}
