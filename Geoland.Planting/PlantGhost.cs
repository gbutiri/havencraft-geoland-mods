// SPDX-License-Identifier: MIT
// Part of the Geoland library: unofficial, fan-made mods for HavenCraft.
// HavenCraft is the work of Dionic Software; this project is not affiliated with them.
//
// PlantGhost: the see-through preview of the plant at the spot being aimed at.
//
// How it relates to the game: the game shows a ghost of a building piece before it is
// placed. This class asks the game for the same kind of ghost for a plant, and marks it
// with the game's own outlines: the building outline where the plant may go, and the
// demolishing outline where it may not. One ghost is made per kind of plant the first
// time it is needed and kept for the rest of the world's session; it is only shown,
// moved and hidden after that.
using System.Collections.Generic;
using UnityEngine;

namespace Geoland.Planting;

internal sealed class PlantGhost
{
    // The Unity layer the game puts its own construction ghosts on. It keeps a ghost off
    // the map and out of the way of clicks.
    private const int GhostLayer = 9;

    private const string GameObjectNamePrefix = "geolandPlantingGhost_";

    private readonly WorldObjectModule worldObjects;
    private readonly Dictionary<string, WorldObjectGhost> ghostByObjectName = new Dictionary<string, WorldObjectGhost>();

    private WorldObjectGhost current;
    private string currentObjectName;
    private bool visible;
    private bool outlinedAsGood;

    internal PlantGhost(WorldObjectModule worldObjects)
    {
        this.worldObjects = worldObjects;
    }

    /// <summary>Shows the ghost of this plant at the spot, outlined as a good spot or a refused one.</summary>
    internal void Show(Plantable plantable, GridTransform spot, bool spotIsGood)
    {
        if (currentObjectName != plantable.ObjectName)
        {
            Hide();
            current = GhostFor(plantable.ObjectName);
            currentObjectName = plantable.ObjectName;
        }

        WorldObjectRepresentation representation = current.GetRepresentation();
        representation.SetTransform(WorldObjectUtil.GridToCenterTransform(spot, current.description, ""));

        bool outlineIsStale = outlinedAsGood != spotIsGood;
        if (!visible)
        {
            representation.GetBaseGameObject().SetActive(true);
            visible = true;
            outlineIsStale = true;   // a ghost that was hidden has lost its outline
        }
        if (outlineIsStale)
        {
            representation.SetOutline(spotIsGood ? OutlineGameID.ConstructSelection : OutlineGameID.DeconstructSelection);
            outlinedAsGood = spotIsGood;
        }
    }

    /// <summary>Hides the ghost, if it is showing.</summary>
    internal void Hide()
    {
        if (!visible) return;
        current.GetRepresentation().GetBaseGameObject().SetActive(false);
        visible = false;
    }

    // The ghost for this kind of plant, made on first use.
    private WorldObjectGhost GhostFor(string objectName)
    {
        if (ghostByObjectName.TryGetValue(objectName, out WorldObjectGhost ghost)) return ghost;

        ghost = worldObjects.SpawnWorldObjectGhost(objectName, "");
        GameObject root = ghost.GetRepresentation().GetBaseGameObject();
        root.name = GameObjectNamePrefix + objectName;
        foreach (Transform part in root.GetComponentsInChildren<Transform>(true))
        {
            part.gameObject.layer = GhostLayer;
        }
        root.SetActive(false);
        ghostByObjectName[objectName] = ghost;
        return ghost;
    }
}
