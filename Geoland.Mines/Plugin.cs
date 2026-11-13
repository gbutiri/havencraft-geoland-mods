// SPDX-License-Identifier: MIT
// Part of the Geoland library: unofficial, fan-made mods for HavenCraft.
// HavenCraft is the work of Dionic Software; this project is not affiliated with them.
//
// Plugin: the entry point of Geoland.Mines, and its settings.
//
// What the plugin does: in HavenCraft a rock that has been mined out is gone for good,
// so a village's stone runs out near home. This plugin makes a mine out of two things
// the game already has. A Tree Stake that stands on a Miner Zone is a mine. The plugin
// keeps one of the game's own rocks standing in it, and puts a new one there some time
// after the last was mined out. The villagers of the Miner Zone mine it with the game's
// own "Mine Rocks" order. A Tree Stake on any other plot is left entirely alone.
//
// How it relates to the game: BepInEx, the mod loader, creates this class when the game
// starts. It reads the settings and checks that the one private part of the game the
// plugin relies on is still there (GameAccess). The plugin has no patches of its own.
// Everything else runs from MinesMod, on the game's own "world loaded" and "every frame"
// calls. None of this has been run in the game yet; it is read in the game's code.
using BepInEx;
using BepInEx.Configuration;
using Geoland.Shared;

namespace Geoland.Mines;

[BepInPlugin(Id, Name, Version)]
public sealed class Plugin : BaseUnityPlugin
{
    /// <summary>Names the plugin to the mod loader, and names its settings file.</summary>
    public const string Id = "geoland.havencraft.mines";
    public const string Name = "Geoland.Mines";
    public const string Version = "0.1.0";

    private const string SettingsSection = "Mines";

    internal static ConfigEntry<bool> Enabled { get; private set; }
    internal static ConfigEntry<string> Rock { get; private set; }
    internal static ConfigEntry<int> WaitTicks { get; private set; }

    private void Awake()
    {
        GameHook.Log = Logger;

        Enabled = Config.Bind(SettingsSection, "Enabled", true,
            "Keep a rock standing in every Tree Stake that is placed on a Miner Zone. Tree Stakes on other plots are never touched.");
        Rock = Config.Bind(SettingsSection, "Rock", RockChoice.Default,
            "Which of the game's rocks appears in a mine, by its internal name. "
            + "rockE and rockD are small and give 21 stone each. rockC is larger and gives 45. "
            + "rockA and rockB are two cells wide, give 65, and may not fit inside the stake. "
            + "Any other name is replaced by " + RockChoice.Default + ", with a line in the log.");
        WaitTicks = Config.Bind(SettingsSection, "WaitTicks", MineKeeper.DefaultWaitTicks,
            "How long an empty mine waits before its next rock appears, in game ticks. The game counts 100 ticks a second "
            + "while it is not paused. The default, 21000, is the time a berry bush takes to refill: three and a half minutes "
            + "of play, or a little under three and a half hours on the game's clock at its usual day length. "
            + "Values below " + MineKeeper.ShortestWaitTicks + " are treated as " + MineKeeper.ShortestWaitTicks + ". "
            + "The wait is not kept in the save: after loading, an empty mine waits the whole time again.");

        GameAccess.Resolve();
        GameHook.CreateMod = () => new MinesMod();
        GameHook.ApplyPatches(Id, Name + " " + Version);
    }
}
