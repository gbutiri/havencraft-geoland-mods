// SPDX-License-Identifier: MIT
// Part of the Geoland library: unofficial, fan-made mods for HavenCraft.
// HavenCraft is the work of Dionic Software; this project is not affiliated with them.
//
// Plugin: the entry point of Geoland.Planting, and its settings.
//
// What the plugin does: lets the player plant things. A key (G by default) switches
// planting mode on and off. While it is on and a plantable item is selected in the
// hotbar, a ghost of the plant previews the spot being aimed at, and a click plants it
// there for one of the item. Plantable.cs lists which item plants what.
//
// How it relates to the game: BepInEx, the mod loader, creates this class when the game
// starts. It reads the settings, checks that the parts of the game the plugin relies on
// are still there (GameAccess), and applies the patches that let planting mode take the
// "use item" click (PlantClickPatch and SwallowClickPatch). Everything else runs from
// PlantingMod, on the game's own "world loaded" and "every frame" calls.
using BepInEx;
using BepInEx.Configuration;
using Geoland.Shared;
using UnityEngine.InputSystem;

namespace Geoland.Planting;

[BepInPlugin(Id, Name, Version)]
public sealed class Plugin : BaseUnityPlugin
{
    /// <summary>Names the plugin to the mod loader, and names its settings file.</summary>
    public const string Id = "geoland.havencraft.planting";
    public const string Name = "Geoland.Planting";
    public const string Version = "0.8.2";

    private const string SettingsSection = "Planting";

    internal static ConfigEntry<Key> ToggleKey { get; private set; }

    private void Awake()
    {
        GameHook.Log = Logger;

        ToggleKey = Config.Bind(SettingsSection, "ToggleKey", Key.G,
            "Key that switches planting mode on and off. G is not used by the game's default bindings.");

        GameAccess.Resolve();
        GameHook.CreateMod = () => new PlantingMod();
        GameHook.ApplyPatches(Id, Name + " " + Version);
    }
}
