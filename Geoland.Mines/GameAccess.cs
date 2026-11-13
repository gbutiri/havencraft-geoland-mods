// SPDX-License-Identifier: MIT
// Part of the Geoland library: unofficial, fan-made mods for HavenCraft.
// HavenCraft is the work of Dionic Software; this project is not affiliated with them.
//
// GameAccess: the one place where this plugin reaches into a private part of the game.
//
// How it relates to the game: a Tree Stake is a field. When a villager sows a field, the
// game places the plant at the field's own position moved by an offset from the field's
// definition, which for the Tree Stake is its middle cell. The field keeps that
// definition in a private field. The plugin reads the offset from there, so a rock goes
// exactly where the game would put a sapling, whatever a later game version makes of the
// stake's shape. The field is found by name once, at start-up. If a game update renames
// or removes it, the log says so and the plugin places no rocks; the game then behaves
// exactly as it does without the plugin.
using System.Reflection;
using Geoland.Shared;
using Unity.Mathematics;

namespace Geoland.Mines;

internal static class GameAccess
{
    private const string FieldDefinitionField = "desc";

    private static FieldInfo fieldDefinition;

    /// <summary>False when the game no longer has what this plugin reaches for.</summary>
    internal static bool Available { get { return fieldDefinition != null; } }

    /// <summary>Looks the field up. Called once, when the plugin starts.</summary>
    internal static void Resolve()
    {
        fieldDefinition = GameMembers.Field(typeof(FieldWOComponent), FieldDefinitionField,
            typeof(FieldWOCDescription), "placing a rock in the middle of a Tree Stake");
    }

    /// <summary>
    /// How far from a field's own position the game places what grows in it, in cells.
    /// </summary>
    internal static int3 PlantOffsetOf(FieldWOComponent field)
    {
        return ((FieldWOCDescription)fieldDefinition.GetValue(field)).plantOffset;
    }
}
