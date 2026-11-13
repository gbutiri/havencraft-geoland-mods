// SPDX-License-Identifier: MIT
// Part of the Geoland library: unofficial, fan-made mods for HavenCraft.
// HavenCraft is the work of Dionic Software; this project is not affiliated with them.
//
// GameAccess: the one place where this plugin reaches into private parts of the game.
//
// How it relates to the game: the player action module is where the game turns the "use
// item" and "interact" buttons into actions. Three of its private methods handle a click,
// a button held down and a button released, and it privately holds the input module,
// which knows whether the keyboard and mouse currently belong to the world, a text field
// or a menu. Planting needs all four. They are found by name once, at start-up. If a game
// update renames or removes one, the log says so and planting stays switched off; the
// game then behaves exactly as it does without the plugin.
//
// A second, separate group makes a new plant start empty. The game creates an object
// that grows wild already full, and one the player builds empty, with its refill timer
// running. A planted bush is the game's wild object, so it would appear full, and the
// one item it cost would come straight back with more. The harvestable part of an object
// keeps its fullness and its timer private; these reaches set them the way the game
// itself does for a built object. If a game update removes one of them, the log says so
// and planting still works: the plants simply appear full.
using System;
using System.Reflection;
using Geoland.Shared;

namespace Geoland.Planting;

internal static class GameAccess
{
    private const string NeededFor = "planting";
    private const string NeededForStartingEmpty = "a new plant starting empty (plants will appear full)";

    private static FieldInfo inputModule;
    private static FieldInfo harvestDescription;
    private static FieldInfo harvestSimulation;
    private static FieldInfo harvestRefillTick;
    private static MethodInfo harvestSetIsFull;

    /// <summary>Handles one click of "use item" or "interact".</summary>
    internal static MethodInfo ClickHandler { get; private set; }

    /// <summary>Handles the same buttons being held down.</summary>
    internal static MethodInfo ButtonHeldHandler { get; private set; }

    /// <summary>Handles the same buttons being released.</summary>
    internal static MethodInfo ButtonReleasedHandler { get; private set; }

    /// <summary>False when the game no longer has what this plugin reaches for.</summary>
    internal static bool Available
    {
        get { return inputModule != null && ClickHandler != null && ButtonHeldHandler != null && ButtonReleasedHandler != null; }
    }

    /// <summary>Looks everything up. Called once, when the plugin starts.</summary>
    internal static void Resolve()
    {
        inputModule = GameMembers.Field(typeof(PlayerActionModule), "inputModule", typeof(InputModule), NeededFor);
        ClickHandler = GameMembers.Method(typeof(PlayerActionModule), "HandleConfirmAction", NeededFor);
        ButtonHeldHandler = GameMembers.Method(typeof(PlayerActionModule), "HandleConfirmPressed", NeededFor);
        ButtonReleasedHandler = GameMembers.Method(typeof(PlayerActionModule), "HandleConfirmReleased", NeededFor);

        Type harvest = typeof(ResourceHarvestWOComponent);
        harvestDescription = GameMembers.Field(harvest, "desc", typeof(ResourceHarvestWOCDescription), NeededForStartingEmpty);
        harvestSimulation = GameMembers.Field(harvest, "simulationModule", typeof(SimulationModule), NeededForStartingEmpty);
        harvestRefillTick = GameMembers.Field(harvest, "refillTick", typeof(int), NeededForStartingEmpty);
        harvestSetIsFull = GameMembers.Method(harvest, "SetIsFull", NeededForStartingEmpty);
    }

    /// <summary>False when a new plant cannot be made to start empty. Planting still works.</summary>
    internal static bool CanStartEmpty
    {
        get { return harvestDescription != null && harvestSimulation != null && harvestRefillTick != null && harvestSetIsFull != null; }
    }

    /// <summary>
    /// Empties a harvestable object that was just created and starts its refill timer:
    /// the same three steps the game takes for an object the player builds.
    /// </summary>
    internal static void StartEmpty(ResourceHarvestWOComponent harvest)
    {
        var description = (ResourceHarvestWOCDescription)harvestDescription.GetValue(harvest);
        var simulation = (SimulationModule)harvestSimulation.GetValue(harvest);

        const bool full = false;
        const bool evenIfUnchanged = false;
        harvestSetIsFull.Invoke(harvest, new object[] { full, evenIfUnchanged });
        harvestRefillTick.SetValue(harvest, simulation.GetTick() + description.regenerateTicks);
        simulation.AddTickUpdate(harvest, description.regenerateTicks);
    }

    /// <summary>The game's input module, as held by the player action module.</summary>
    internal static InputModule InputOf(PlayerActionModule playerActions)
    {
        return (InputModule)inputModule.GetValue(playerActions);
    }
}
