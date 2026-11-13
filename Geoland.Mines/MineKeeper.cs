// SPDX-License-Identifier: MIT
// Part of the Geoland library: unofficial, fan-made mods for HavenCraft.
// HavenCraft is the work of Dionic Software; this project is not affiliated with them.
//
// MineKeeper: finds the mines of the loaded world and keeps a rock standing in each.
//
// A mine is a Tree Stake that stands on a Miner Zone. For each one this class looks at
// what the stake holds. If it holds anything, a rock or a sapling or a tree, the stake
// is left alone. If it holds nothing, the wait starts; when the wait is over a rock is
// placed in the stake and handed to it. When that rock has been mined out the stake
// holds nothing again, and the wait starts again.
//
// How it relates to the game (read in the game's code and data; of it, a stake on a Miner
// Zone getting its rock after the wait, and the next rock after that one was broken,
// have been seen in play; villagers mining the rock has not):
//  - The plot module's tracker files every Tree Stake under the plot it stands on, and
//    hands out that list per plot. So the stakes are found by asking for each Miner
//    Zone's list; the world's objects are never searched. In a multiplayer game the
//    tracker gives a guest no lists, so the plugin only acts for the host.
//  - A Tree Stake is a field. A field remembers the one object that grows in it, and
//    answers with nothing once that object no longer exists. The game uses this for a
//    sapling and then for the tree it becomes. The plugin hands the stake a rock the
//    same way, through the same public call, so "what the stake holds" is the game's
//    own answer and the plugin keeps no list of rocks.
//  - That link is written to the save by the game itself. After loading, a stake with a
//    rock in it still answers with its rock. Only the wait is not saved: a stake found
//    empty after loading waits the whole time again.
//  - The rock is placed with the world object module's own spawn call, where the game
//    would place the stake's sapling. A rock placed that way is an ordinary rock: the
//    game files it under the Miner Zone and offers it to that plot's "Mine Rocks" order
//    without being asked.
//  - A Miner Zone has no "Plant Trees at Stakes" order, so no villager sows a mine. The
//    player can still sow any empty stake by hand. A stake holding a sapling or a tree
//    is one that holds something, so the plugin waits until it is gone.
//  - The stake's own definition blocks its middle cell for walking. So for the default
//    rock, one cell in size, nobody should be standing where it appears. A larger rock
//    reaches past that cell, and this was not checked for it.
using System;
using System.Collections.Generic;
using Geoland.Shared;
using Unity.Mathematics;

namespace Geoland.Mines;

internal sealed class MineKeeper
{
    /// <summary>The wait unless the settings say otherwise: a berry bush's refill time in the game's data.</summary>
    internal const int DefaultWaitTicks = 21000;

    /// <summary>The shortest wait the plugin accepts: one second of play.</summary>
    internal const int ShortestWaitTicks = 100;

    // The Tree Stake's internal name. The game files stakes on a plot under the same word.
    private const string StakeName = "fieldTree";

    /// <summary>What the plugin remembers about one mine while the world is loaded.</summary>
    private sealed class Mine
    {
        public bool HeldSomething;
        public bool HeldRock;
        public bool Failed;
        public long RockDueTick;
    }

    private readonly PlotModule plots;
    private readonly WorldObjectModule worldObjects;
    private readonly SimulationModule simulation;
    private readonly string[] rocks;
    private readonly System.Random dice = new System.Random();
    private readonly int waitTicks;

    private readonly Dictionary<uint, Mine> mineByStakeId = new Dictionary<uint, Mine>();

    // Reused on every look, so that looking costs no memory.
    private readonly List<(WorldObject stake, ushort plotId)> stakesOnMinerZones = new List<(WorldObject, ushort)>();
    private readonly HashSet<uint> stakeIdsSeen = new HashSet<uint>();
    private readonly List<uint> stakeIdsToForget = new List<uint>();

    internal MineKeeper(PlotModule plots, WorldObjectModule worldObjects, SimulationModule simulation, string[] rocks, int waitTicks)
    {
        this.plots = plots;
        this.worldObjects = worldObjects;
        this.simulation = simulation;
        this.rocks = rocks;
        this.waitTicks = waitTicks;
    }

    /// <summary>True when the loaded world has at least one Miner Zone.</summary>
    internal bool AnyMinerZone()
    {
        foreach (PlotInfo plot in plots.GetAllPlotInfos())
        {
            if (IsMinerZone(plot)) return true;
        }
        return false;
    }

    /// <summary>
    /// Looks at every mine once: notes what each holds, starts or continues its wait, and
    /// places a rock where the wait is over. Called about once a second.
    /// </summary>
    internal void LookAtMines()
    {
        // The stakes are collected first and acted on afterwards, so that placing a rock
        // never changes a list of the game's while it is being read.
        stakesOnMinerZones.Clear();
        foreach (PlotInfo plot in plots.GetAllPlotInfos())
        {
            if (!IsMinerZone(plot)) continue;
            foreach (WorldObject stake in plots.tracker.GetWorldObjects(plot.id, StakeName))
            {
                if (stake.IsValid() && stake.description.worldObjectName == StakeName)
                {
                    stakesOnMinerZones.Add((stake, plot.id));
                }
            }
        }

        stakeIdsSeen.Clear();
        if (stakesOnMinerZones.Count > 0)
        {
            long now = simulation.GetTick();
            foreach ((WorldObject stake, ushort plotId) in stakesOnMinerZones)
            {
                FieldWOComponent field = stake.TryGetComponent<FieldWOComponent>();
                if (field == null) continue;

                uint stakeId = (uint)stake.GetID();
                stakeIdsSeen.Add(stakeId);
                try
                {
                    LookAtMine(stake, stakeId, field, plotId, now);
                }
                catch (Exception e)
                {
                    // One mine that cannot be kept must not stop the others. It is left
                    // alone for the rest of this world's session, and the log says why.
                    if (mineByStakeId.TryGetValue(stakeId, out Mine failed)) failed.Failed = true;
                    GameHook.Log.LogError("The mine with stake " + stakeId + " on plot " + plotId + " is left alone after an error: " + e);
                }
            }
        }
        ForgetMinesNotSeen();
    }

    private void LookAtMine(WorldObject stake, uint stakeId, FieldWOComponent field, ushort plotId, long now)
    {
        bool firstLook = !mineByStakeId.TryGetValue(stakeId, out Mine mine);
        if (firstLook)
        {
            mine = new Mine();
            mineByStakeId[stakeId] = mine;
        }
        if (mine.Failed) return;

        WorldObject held = field.GetPlantObject();
        if (held.IsValid())
        {
            mine.HeldSomething = true;
            mine.HeldRock = IsRock(held);
            if (firstLook)
            {
                GameHook.Log.LogInfo(Describe(stake, plotId) + " holds " + held.description.worldObjectName + " already");
            }
            return;
        }

        if (mine.HeldSomething)
        {
            GameHook.Log.LogInfo(Describe(stake, plotId) + ": " + (mine.HeldRock ? "its rock is gone" : "what it held is gone")
                + "; the next rock comes in " + waitTicks + " ticks");
            mine.HeldSomething = false;
            mine.HeldRock = false;
            mine.RockDueTick = now + waitTicks;
            return;
        }

        if (firstLook)
        {
            GameHook.Log.LogInfo(Describe(stake, plotId) + " is empty; its first rock comes in " + waitTicks + " ticks");
            mine.RockDueTick = now + waitTicks;
            return;
        }

        if (now >= mine.RockDueTick) PlaceRock(stake, field, plotId, mine);
    }

    // Places a rock where the game would place the stake's sapling, and hands it to the
    // stake as the game hands it a sapling.
    private void PlaceRock(WorldObject stake, FieldWOComponent field, ushort plotId, Mine mine)
    {
        GridTransform spot = stake.GetTransform();
        spot.pos += GameAccess.PlantOffsetOf(field);

        // A rock may stand there that the stake does not know of. Then the stake is
        // handed that rock, and no second one is placed on top of it.
        WorldObject rock = RockStandingAt(plotId, spot.pos);
        bool rockWasThere = rock.IsValid();

        // With the "random" setting there are several rocks to draw from; with a named
        // rock there is one.
        string rockName = rocks[dice.Next(rocks.Length)];
        if (!rockWasThere) rock = worldObjects.SpawnWorldObject(rockName, spot);

        field.SetPlantObject(rock);
        mine.HeldSomething = true;
        mine.HeldRock = true;
        GameHook.Log.LogInfo(Describe(stake, plotId) + ": "
            + (rockWasThere ? "took the " + rock.description.worldObjectName + " already standing in it" : "placed " + rockName));
    }

    // The rock on this plot that stands on exactly this cell, if there is one.
    private WorldObject RockStandingAt(ushort plotId, int3 cell)
    {
        foreach (WorldObject rock in plots.tracker.GetWorldObjects(plotId, RockChoice.TrackingKey))
        {
            if (!rock.IsValid()) continue;
            int3 at = rock.GetTransform().pos;
            if (at.x == cell.x && at.y == cell.y && at.z == cell.z) return rock;
        }
        return default(WorldObject);
    }

    // A stake that was removed, or whose plot is no longer a Miner Zone, is no longer a
    // mine. What it holds stays as it is; only the plugin's note of it is dropped.
    private void ForgetMinesNotSeen()
    {
        if (mineByStakeId.Count == stakeIdsSeen.Count) return;

        stakeIdsToForget.Clear();
        foreach (uint stakeId in mineByStakeId.Keys)
        {
            if (!stakeIdsSeen.Contains(stakeId)) stakeIdsToForget.Add(stakeId);
        }
        foreach (uint stakeId in stakeIdsToForget) mineByStakeId.Remove(stakeId);
    }

    private static bool IsMinerZone(PlotInfo plot)
    {
        IPlotConfig config = plot.TryGetConfig();
        return config != null && config.GetPlotConfigType() == PlotConfigNames.MinerZone;
    }

    private static bool IsRock(WorldObject worldObject)
    {
        return worldObject.description.plotTrackingKeys.Contains(RockChoice.TrackingKey);
    }

    private static string Describe(WorldObject stake, ushort plotId)
    {
        int3 at = stake.GetTransform().pos;
        return "Mine at (" + at.x + ", " + at.y + ", " + at.z + ") on plot " + plotId;
    }
}
