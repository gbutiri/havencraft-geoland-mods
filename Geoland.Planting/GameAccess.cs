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
using System.Reflection;
using Geoland.Shared;

namespace Geoland.Planting;

internal static class GameAccess
{
    private const string NeededFor = "planting";

    private static FieldInfo inputModule;

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
    }

    /// <summary>The game's input module, as held by the player action module.</summary>
    internal static InputModule InputOf(PlayerActionModule playerActions)
    {
        return (InputModule)inputModule.GetValue(playerActions);
    }
}
