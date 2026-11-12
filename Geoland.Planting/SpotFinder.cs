// SPDX-License-Identifier: MIT
// Part of the Geoland library: unofficial, fan-made mods for HavenCraft.
// HavenCraft is the work of Dionic Software; this project is not affiliated with them.
//
// SpotFinder: works out which grid cell the player is aiming at, and whether a plant may
// go there.
//
// How it relates to the game: the game's height module gives the terrain height at any
// point. The aim point is found by walking along the view ray in small steps and asking
// for the height under each one, until a step is below the terrain. The point is
// therefore always on the ground itself, never on a building or another object that
// happens to be in the line of sight. The height of the chosen cell and
// the plant's exact position on it come from the same two calls the game's own
// wild-plant spawner uses, so a planted object sits on the ground as a natural one does.
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Geoland.Planting;

internal sealed class SpotFinder
{
    // Below this terrain height the ground is beach or water. It is the same floor the
    // game's own wild-plant spawner uses.
    private const float MinTerrainHeight = 0.4f;

    // How far from the character a plant may be placed, in cells.
    private const float MaxReach = 12f;

    // The view ray is followed this far, in steps of this size.
    private const float RayLength = 80f;
    private const float RayStep = 0.2f;

    // The island's grid is 1024 cells each way. Points on the outermost cells are skipped
    // so that the terrain height is never asked for outside the world.
    private const float WorldEdgeMin = 1f;
    private const float WorldEdgeMax = 1022f;

    // A plant needs one free cell on every side. Objects more than this many cells above
    // or below the spot, such as on a cliff top or a floor overhead, do not count.
    private const int ClearCellsAround = 1;
    private const int ClearCellsUpAndDown = 3;

    // How long the answer of the "is something in the way" scan may be reused, in seconds.
    private const float ScanReuseSeconds = 0.5f;

    // The game's rotations are 0, 2, 4 and 6 for the four directions.
    private const int Directions = 4;
    private const int RotationPerDirection = 2;

    private readonly WorldObjectModule worldObjects;
    private readonly HeightAndBiomeModule heights;
    private readonly PlayerCharacterModule players;

    private int nextRotation;

    // The last "is something in the way" scan: which cell, when, and the answer.
    private bool hasScan;
    private int scanX, scanY, scanZ;
    private float scanTime;
    private bool scanBlocked;

    internal SpotFinder(WorldObjectModule worldObjects, HeightAndBiomeModule heights, PlayerCharacterModule players)
    {
        this.worldObjects = worldObjects;
        this.heights = heights;
        this.players = players;
        TurnNextPlant();
    }

    /// <summary>
    /// Picks a new random direction for the next plant to face, so a row of them does not
    /// look stamped. The ghost shows the direction before the click.
    /// </summary>
    internal void TurnNextPlant()
    {
        nextRotation = UnityEngine.Random.Range(0, Directions) * RotationPerDirection;
    }

    /// <summary>Makes the next "is something in the way" check look at the world again.</summary>
    internal void ForgetLastScan()
    {
        hasScan = false;
    }

    /// <summary>Works out the spot being aimed at for this plant.</summary>
    /// <param name="plantable">What is to be planted.</param>
    /// <param name="spot">The plant's place on the grid, when there is one.</param>
    /// <param name="problem">
    /// Why a plant may not go there, in words for the player, or null if it may.
    /// </param>
    /// <returns>
    /// False when there is no spot at all: the player is not aiming at ground, or the
    /// point is outside the world. True when there is a spot, good or not; the ghost is
    /// shown there either way and <paramref name="problem"/> says which it is.
    /// </returns>
    internal bool TryFind(Plantable plantable, out GridTransform spot, out string problem)
    {
        spot = default;
        if (!TryGetAimPoint(out Vector3 target))
        {
            problem = "Aim at the ground to plant.";
            return false;
        }

        // Centre of the grid cell, on the terrain.
        target.x = Mathf.Floor(target.x) + 0.5f;
        target.z = Mathf.Floor(target.z) + 0.5f;
        target.y = heights.SampleHeight(new Vector2(target.x, target.z));

        WorldObjectDescription description = worldObjects.GetDescriptionFromName(plantable.ObjectName);
        spot = WorldObjectUtil.GetWorldObjectTransformAtWorldPos(
            target, yIsCentered: false, description, "", nextRotation, applyYOffset: true);
        if (spot.pos.y < 0 || !worldObjects.IsWorldPositionInsideWorld(spot.pos))
        {
            problem = "Can't plant here: outside the world.";
            return false;
        }

        Vector3 player = players.GetMainPlayerPosition();
        if (new Vector2(target.x - player.x, target.z - player.z).magnitude > MaxReach)
        {
            problem = "Too far away to plant.";
            return true;
        }
        if (target.y < MinTerrainHeight)
        {
            problem = "Can't plant here: too close to the water.";
            return true;
        }
        problem = IsBlocked(spot) ? "Can't plant here: something is in the way." : null;
        return true;
    }

    // True when another object stands on the spot or on a cell next to it.
    //
    // Cost: this looks at every object in the world, and a grown island has tens of
    // thousands. That is too slow to do every frame for the ghost, so the answer is kept
    // for as long as the aim stays on the same cell, up to half a second. A real planting
    // always looks again (ForgetLastScan).
    private bool IsBlocked(GridTransform spot)
    {
        bool sameCell = hasScan && spot.pos.x == scanX && spot.pos.y == scanY && spot.pos.z == scanZ;
        if (sameCell && Time.unscaledTime - scanTime < ScanReuseSeconds)
        {
            return scanBlocked;
        }

        bool blocked = false;
        foreach (WorldObject other in worldObjects.GetWorldObjects())
        {
            GridTransform place = other.GetTransform();
            if (Math.Abs(place.pos.x - spot.pos.x) <= ClearCellsAround
                && Math.Abs(place.pos.z - spot.pos.z) <= ClearCellsAround
                && Math.Abs(place.pos.y - spot.pos.y) <= ClearCellsUpAndDown)
            {
                blocked = true;
                break;
            }
        }

        hasScan = true;
        scanX = spot.pos.x;
        scanY = spot.pos.y;
        scanZ = spot.pos.z;
        scanTime = Time.unscaledTime;
        scanBlocked = blocked;
        return blocked;
    }

    // Follows the view ray until it goes below the terrain. The ray starts at the mouse
    // pointer when the pointer is free, and at the centre of the screen (the crosshair)
    // when the game has locked it.
    private bool TryGetAimPoint(out Vector3 hit)
    {
        hit = default;
        Camera camera = Camera.main;
        if (camera == null) return false;

        Vector2 screenPoint = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        if (Cursor.lockState != CursorLockMode.Locked && Mouse.current != null)
        {
            screenPoint = Mouse.current.position.ReadValue();
        }

        Ray ray = camera.ScreenPointToRay(new Vector3(screenPoint.x, screenPoint.y, 0f));
        for (float distance = 0f; distance <= RayLength; distance += RayStep)
        {
            Vector3 point = ray.origin + ray.direction * distance;
            if (point.x < WorldEdgeMin || point.z < WorldEdgeMin || point.x > WorldEdgeMax || point.z > WorldEdgeMax) continue;
            if (point.y <= heights.SampleHeight(new Vector2(point.x, point.z)))
            {
                hit = point;
                return true;
            }
        }
        return false;
    }
}
