// SPDX-License-Identifier: MIT
// Part of the Geoland library: unofficial, fan-made mods for HavenCraft.
// HavenCraft is the work of Dionic Software; this project is not affiliated with them.
//
// HarvestTaskAdapter: lets the game's work planner send a villager to a harvestable
// object that has no adapter of its own, such as a beehive.
//
// How it relates to the game: the work planner knows nothing about particular objects.
// It asks an object's adapter (the game's IVillagerTaskWO interface) whether there is
// work to do, what the work yields, how it is going, and what to do when it is
// finished. Trees, rocks and crops have adapters; a beehive has none, which is why no
// villager ever worked one. This is that adapter. Every answer comes from HarvestWork.
using System.Collections.Generic;
using UnityEngine;

namespace Geoland.Harvest;

internal sealed class HarvestTaskAdapter : IVillagerTaskWO
{
    // Tells the game to show the villager's own work time as the progress.
    private const float NoProgressOfItsOwn = -1f;

    private readonly ResourceHarvestWOComponent harvest;

    internal HarvestTaskAdapter(ResourceHarvestWOComponent harvest)
    {
        this.harvest = harvest;
    }

    public bool CanDoTask(string taskName)
    {
        return HarvestWork.CanDo(harvest, taskName);
    }

    public void GetRequiredOutputSpaceAmounts(List<ItemAmount> outAmounts)
    {
        HarvestWork.AddYield(harvest, outAmounts);
    }

    // Called every frame of the work. Returning true stops the work early, which is
    // right when someone else has taken the item in the meantime.
    public bool TryExecuteTaskStep(string taskName, float workSpeed, IVillagerTaskWorkHandler taskWorkHandler, WorkOrder workOrder, VillagerNPCComponent villager)
    {
        return !HarvestWork.IsAvailable(harvest, HarvestNames.ItemOfTask(taskName));
    }

    public bool TryExecuteTaskFinish(string taskName, IVillagerTaskWorkHandler taskWorkHandler, WorkOrder workOrder, VillagerNPCComponent villager)
    {
        return HarvestWork.Finish(harvest, taskName, handOver: taskWorkHandler, worker: villager);
    }

    public float GetWorkProgressOverride()
    {
        return NoProgressOfItsOwn;
    }

    public Vector3 GetPosition()
    {
        return harvest.GetWorldObject().GetCenterTransform().pos;
    }

    public bool IsValid()
    {
        return harvest.GetWorldObject().IsValid();
    }

    public WorldObject GetWorldObject()
    {
        return harvest.GetWorldObject();
    }
}
