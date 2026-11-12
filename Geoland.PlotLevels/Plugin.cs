// SPDX-License-Identifier: MIT
// Part of the Geoland library: unofficial, fan-made mods for HavenCraft.
// HavenCraft is the work of Dionic Software; this project is not affiliated with them.
//
// Plugin: the entry point of Geoland.PlotLevels, and its settings.
//
// What the plugin does: plots on more than one level. In HavenCraft every ground cell
// belongs to one plot, and so does every floor above it. This plugin lets an upstairs
// floor belong to a different plot than the ground below, so that the bedrooms of a
// Home can sit above a shop. It works through the game's own plot painter:
//   K                 pick the plot you are aiming at (the home's stairs, say)
//   click             on an upstairs floor: give it, from that height up, to that plot
//   the erase button  on an upstairs floor: hand that floor back to the ground plot
// While the painter is out, a coloured tile marks every upstairs cell given to a plot.
// Aimed at the ground floor or the terrain, the painter does what it always did.
//
// How it relates to the game: BepInEx, the mod loader, creates this class when the game
// starts. It reads the settings, checks that the parts of the game the plugin relies on
// are still there (GameAccess), and applies the patches. The files, in reading order:
//   LevelMap          the upper levels themselves, and how they change
//   PlotLookupPatch   the one place where the game is given a different answer
//   LevelSavePart     keeping the levels inside the save file
//   PainterPatches    making the plot painter aim at, and paint, upstairs floors
//   PlotLevelsMod     the K key, the painter's strokes and the overlay, tied together
//   LevelOverlay      drawing the tiles
//
// Limits: the plot an upstairs floor is given to must already exist, so it needs at
// least one ground cell. Single-player only. A save loaded without this plugin ignores
// the levels, and upstairs objects fall back to the plot on the ground.
using BepInEx;
using BepInEx.Configuration;
using Geoland.Shared;
using UnityEngine.InputSystem;

namespace Geoland.PlotLevels;

[BepInPlugin(Id, Name, Version)]
public sealed class Plugin : BaseUnityPlugin
{
    /// <summary>Names the plugin to the mod loader, and names its settings file.</summary>
    public const string Id = "geoland.havencraft.plotlevels";
    public const string Name = "Geoland.PlotLevels";
    public const string Version = "0.5.1";

    /// <summary>The default for <see cref="MinHeightAboveGround"/>, in blocks.</summary>
    internal const int DefaultMinHeightAboveGround = 2;

    internal static ConfigEntry<Key> PickKey { get; private set; }
    internal static ConfigEntry<int> MinHeightAboveGround { get; private set; }

    private void Awake()
    {
        GameHook.Log = Logger;

        PickKey = Config.Bind("Keys", "PickPlot", Key.K,
            "With the plot painter selected: pick the plot you are aiming at. An upstairs floor you then click is given to it.");
        MinHeightAboveGround = Config.Bind("Levels", "MinHeightAboveGround", DefaultMinHeightAboveGround,
            "An upper level must start at least this many blocks above the terrain. Lower floors count as the ground floor, which belongs to the game's own plot painter.");

        GameAccess.Resolve();
        GameHook.CreateMod = () => new PlotLevelsMod();
        GameHook.ApplyPatches(Id, Name + " " + Version);
    }
}
