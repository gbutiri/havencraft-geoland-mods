// SPDX-License-Identifier: MIT
// Part of the Geoland library: unofficial, fan-made mods for HavenCraft.
// HavenCraft is the work of Dionic Software; this project is not affiliated with them.
//
// TravelerWishes: the pool of goods travelers may wish for, and each traveler's wish.
//
// A wish is worked out from the traveler's own ID number and nothing else. The same
// traveler therefore always has the same wish: while the player gathers it, after leaving
// and coming back, and after saving and loading. Nothing is written to the save file.
//
// How it relates to the game: the game tells joining prices apart by which list object
// they are, not by what is in them. Each traveler's wish is therefore made once and the
// same list is handed out every time, and the plugin recognises a wish that comes back
// from the join panel the same way.
using System;
using System.Collections.Generic;
using Geoland.Shared;

namespace Geoland.RecruitWishes;

internal static class TravelerWishes
{
    // Spreads neighbouring traveler IDs across the whole range of seeds (Knuth's
    // multiplicative hash), so travelers who arrive together do not get similar wishes.
    private const uint IdScrambler = 2654435761u;

    // Amounts of goods that come in bulk are rounded to a multiple of five, so a wish
    // reads as "40 honey" and not "38 honey". Small amounts are left exact.
    private const int RoundingStep = 5;
    private const int SmallestMaxToRound = 20;

    /// <summary>One line of the pool: a good and the range its amount is drawn from.</summary>
    private struct PoolEntry
    {
        public Item Item;
        public int Min;
        public int Max;
    }

    private static readonly List<PoolEntry> pool = new List<PoolEntry>();
    private static readonly Dictionary<uint, List<ItemAmount>> wishByTravelerId = new Dictionary<uint, List<ItemAmount>>();
    private static readonly HashSet<List<ItemAmount>> allWishes = new HashSet<List<ItemAmount>>();

    /// <summary>True when wishes can be made: switched on, a usable pool, and the game as expected.</summary>
    internal static bool Ready
    {
        get { return Plugin.Enabled.Value && GameAccess.Available && pool.Count > 0; }
    }

    /// <summary>
    /// Reads the pool from the settings, keeping only the goods this version of the game
    /// has. Called for each loaded world; wishes made for the previous world are dropped.
    /// </summary>
    internal static void LoadPool(ItemModule items)
    {
        pool.Clear();
        wishByTravelerId.Clear();
        allWishes.Clear();

        var skipped = new List<string>();
        foreach (string part in Plugin.Pool.Value.Split(','))
        {
            string entry = part.Trim();
            if (entry.Length == 0) continue;

            if (!TryParseEntry(entry, out string itemName, out int min, out int max))
            {
                skipped.Add(entry + " (not item:min-max)");
                continue;
            }
            Item item = items.TryGetItem(itemName);
            if (item == null)
            {
                skipped.Add(entry + " (no such item)");
                continue;
            }
            pool.Add(new PoolEntry { Item = item, Min = min, Max = max });
        }

        GameHook.Log.LogInfo("wish pool: " + pool.Count + " goods"
            + (skipped.Count > 0 ? "; skipped: " + string.Join("; ", skipped) : ""));
    }

    /// <summary>True if this price is a wish this plugin handed out.</summary>
    internal static bool IsWish(List<ItemAmount> price)
    {
        return price != null && allWishes.Contains(price);
    }

    /// <summary>The wish of this traveler: always the same list for the same traveler.</summary>
    internal static List<ItemAmount> For(NPC traveler)
    {
        uint id = (uint)traveler.GetID();
        if (wishByTravelerId.TryGetValue(id, out List<ItemAmount> wish)) return wish;

        var random = new Random(unchecked((int)(id * IdScrambler)));
        PoolEntry entry = pool[random.Next(pool.Count)];
        int amount = random.Next(entry.Min, entry.Max + 1);
        if (entry.Max >= SmallestMaxToRound)
        {
            int nearestStep = (amount + RoundingStep / 2) / RoundingStep * RoundingStep;
            amount = Math.Max(entry.Min, nearestStep);
        }
        amount = Math.Min(amount, entry.Max);

        wish = new List<ItemAmount> { new ItemAmount(entry.Item, amount) };
        wishByTravelerId[id] = wish;
        allWishes.Add(wish);
        GameHook.Log.LogInfo("traveler " + id + " wishes for " + amount + " x " + entry.Item.itemName);
        return wish;
    }

    /// <summary>Drops the wish of a traveler who has joined the village.</summary>
    internal static void Forget(NPC traveler)
    {
        uint id = (uint)traveler.GetID();
        if (!wishByTravelerId.TryGetValue(id, out List<ItemAmount> wish)) return;

        allWishes.Remove(wish);
        wishByTravelerId.Remove(id);
    }

    // Reads one pool entry, "item:min-max". The range must be at least 1 and not reversed.
    private static bool TryParseEntry(string entry, out string itemName, out int min, out int max)
    {
        itemName = null;
        min = 0;
        max = 0;

        string[] nameAndRange = entry.Split(':');
        if (nameAndRange.Length != 2) return false;
        string[] range = nameAndRange[1].Split('-');
        if (range.Length != 2) return false;
        if (!int.TryParse(range[0].Trim(), out min) || !int.TryParse(range[1].Trim(), out max)) return false;
        if (min < 1 || max < min) return false;

        itemName = nameAndRange[0].Trim();
        return true;
    }
}
