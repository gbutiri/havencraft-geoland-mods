// SPDX-License-Identifier: MIT
// Part of the Geoland library: unofficial, fan-made mods for HavenCraft.
// HavenCraft is the work of Dionic Software; this project is not affiliated with them.
//
// Plugin: the entry point of Geoland.Harvest, and its settings.
//
// What the plugin does: villagers harvest. Berry bushes, beehives, the plain bushes and
// wild cotton can be picked by the player but not by villagers. This plugin adds work
// orders to the Farm plot, one per item: Gather Berries, Collect Honey, Collect Wax,
// Gather Fiber, Gather Bush Wood and Gather Wild Cotton. A farm worker then harvests the
// objects that stand inside the farm.
//
// How it relates to the game: BepInEx, the mod loader, creates this class when the game
// starts. It checks that the parts of the game the plugin relies on are still there
// (GameAccess) and applies the patches. The work begins when the game reads its data
// files: GameDataPatch adds the work orders to the game's recipes and makes each
// harvestable object known to the game's work planner. HarvestWork has the harvest
// itself. The settings, one switch per kind of object, are created by HarvestCatalog as
// it finds the kinds of object the game has.
using BepInEx;
using BepInEx.Configuration;
using Geoland.Shared;

namespace Geoland.Harvest;

[BepInPlugin(Id, Name, Version)]
public sealed class Plugin : BaseUnityPlugin
{
    /// <summary>Names the plugin to the mod loader, and names its settings file.</summary>
    public const string Id = "geoland.havencraft.harvest";
    public const string Name = "Geoland.Harvest";
    public const string Version = "0.8.1";

    /// <summary>The settings section that holds one on/off switch per kind of object.</summary>
    internal const string SettingsSection = "Harvest";

    /// <summary>The plugin's settings file.</summary>
    internal static ConfigFile Settings { get; private set; }

    private void Awake()
    {
        GameHook.Log = Logger;
        Settings = Config;

        GameAccess.Resolve();
        GameHook.CreateMod = () => new HarvestMod();
        GameHook.ApplyPatches(Id, Name + " " + Version);
    }
}
