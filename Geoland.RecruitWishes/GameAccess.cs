// SPDX-License-Identifier: MIT
// Part of the Geoland library: unofficial, fan-made mods for HavenCraft.
// HavenCraft is the work of Dionic Software; this project is not affiliated with them.
//
// GameAccess: the one place where this plugin reaches into a private part of the game.
//
// How it relates to the game: the traveler module keeps its list of joining prices in a
// private field. The plugin has to read that list to tell when it is used up and which
// price is the repeated last one. The field is found by name once, at start-up. If a game
// update renames or removes it, the log says so and the plugin makes no wishes; the game
// then behaves exactly as it does without the plugin.
using System.Collections.Generic;
using System.Reflection;
using Geoland.Shared;

namespace Geoland.RecruitWishes;

internal static class GameAccess
{
    private const string JoiningPricesField = "travelerConvincePrices";

    private static FieldInfo joiningPrices;

    /// <summary>False when the game no longer has what this plugin reaches for.</summary>
    internal static bool Available { get { return joiningPrices != null; } }

    /// <summary>Looks the field up. Called once, when the plugin starts.</summary>
    internal static void Resolve()
    {
        joiningPrices = GameMembers.Field(typeof(TravelerModule), JoiningPricesField,
            typeof(List<List<ItemAmount>>), "traveler wishes");
    }

    /// <summary>
    /// The game's fixed list of joining prices, in the order it uses them. The game tells
    /// prices apart by which list object they are, so these lists must never be copied.
    /// </summary>
    internal static List<List<ItemAmount>> JoiningPrices(TravelerModule travelers)
    {
        return (List<List<ItemAmount>>)joiningPrices.GetValue(travelers);
    }
}
