// SPDX-License-Identifier: MIT
// Part of the Geoland library: unofficial, fan-made mods for HavenCraft.
// HavenCraft is the work of Dionic Software; this project is not affiliated with them.
//
// GameMembers: looks up the private parts of the game that a plugin has to reach by name.
//
// How it relates to the game: some of what these plugins need is private in the game's
// code, so it can only be found by name while the game runs. A name can change with any
// game update. Each plugin therefore looks its names up once, at start-up, through this
// class. When one is gone the log says exactly which, and the plugin switches the feature
// that needed it off instead of failing later in the middle of play.
using System;
using System.Reflection;
using HarmonyLib;

namespace Geoland.Shared;

internal static class GameMembers
{
    private const BindingFlags AnyDeclared =
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    /// <summary>
    /// Finds a field of a game class by name, searching the class and its base classes.
    /// </summary>
    /// <param name="owner">The game class that has the field.</param>
    /// <param name="name">The field's name in the game's code.</param>
    /// <param name="expectedType">What the field must hold for the plugin's code to use it.</param>
    /// <param name="neededFor">What the plugin uses it for, in a few words, for the log.</param>
    /// <returns>The field, or null (with one line in the log) if the game no longer has it.</returns>
    internal static FieldInfo Field(Type owner, string name, Type expectedType, string neededFor)
    {
        for (Type type = owner; type != null; type = type.BaseType)
        {
            FieldInfo field = type.GetField(name, AnyDeclared);
            if (field == null) continue;
            if (expectedType.IsAssignableFrom(field.FieldType)) return field;

            ReportChange(owner.Name + "." + name + " now holds " + field.FieldType.Name + ", not " + expectedType.Name, neededFor);
            return null;
        }
        ReportChange(owner.Name + "." + name + " is gone", neededFor);
        return null;
    }

    /// <summary>
    /// Finds a method of a game class by name, searching the class and its base classes.
    /// Only for methods that have no overloads.
    /// </summary>
    /// <param name="owner">The game class that has the method.</param>
    /// <param name="name">The method's name in the game's code.</param>
    /// <param name="neededFor">What the plugin uses it for, in a few words, for the log.</param>
    /// <returns>The method, or null (with one line in the log) if the game no longer has it.</returns>
    internal static MethodInfo Method(Type owner, string name, string neededFor)
    {
        try
        {
            for (Type type = owner; type != null; type = type.BaseType)
            {
                MethodInfo method = type.GetMethod(name, AnyDeclared);
                if (method != null) return method;
            }
            ReportChange(owner.Name + "." + name + "() is gone", neededFor);
        }
        catch (AmbiguousMatchException)
        {
            ReportChange(owner.Name + "." + name + "() now comes in several forms", neededFor);
        }
        return null;
    }

    /// <summary>Finds the one constructor of a game class, whatever its visibility.</summary>
    /// <param name="owner">The game class.</param>
    /// <param name="neededFor">What the plugin uses it for, in a few words, for the log.</param>
    /// <returns>The constructor, or null (with one line in the log) if the class no longer has exactly one.</returns>
    internal static ConstructorInfo Constructor(Type owner, string neededFor)
    {
        ConstructorInfo[] constructors = owner.GetConstructors(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        if (constructors.Length == 1) return constructors[0];

        ReportChange(owner.Name + " now has " + constructors.Length + " constructors, not one", neededFor);
        return null;
    }

    /// <summary>Finds a game class that is not public, by name.</summary>
    /// <param name="name">
    /// The class's name in the game's code. A class inside another is written
    /// "Outer+Inner".
    /// </param>
    /// <param name="neededFor">What the plugin uses it for, in a few words, for the log.</param>
    /// <returns>The class, or null (with one line in the log) if the game no longer has it.</returns>
    internal static Type HiddenType(string name, string neededFor)
    {
        Type type = AccessTools.TypeByName(name);
        if (type == null) ReportChange("the class " + name + " is gone", neededFor);
        return type;
    }

    private static void ReportChange(string what, string neededFor)
    {
        GameHook.Log.LogError("The game has changed: " + what + ". It was needed for " + neededFor
            + ", which is now switched off. Look for a newer version of the plugin.");
    }
}
