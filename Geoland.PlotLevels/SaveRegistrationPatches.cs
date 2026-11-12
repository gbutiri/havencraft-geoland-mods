// SPDX-License-Identifier: MIT
// Part of the Geoland library: unofficial, fan-made mods for HavenCraft.
// HavenCraft is the work of Dionic Software; this project is not affiliated with them.
//
// SaveRegistrationPatches: puts the plugin's save part in place for each world, at the
// right moment, and takes it out again.
//
// How it relates to the game: the order matters. When a save is loaded, the game goes
// through its save parts in the order they were added. The plot module's part loads
// the plots, and at that moment every object is filed under the plot it stands in. The
// levels must be in force by then, or beds upstairs would be filed under the shop below
// and the villagers living there would lose their home. So the plugin's part is added
// just before the plot module adds its own: in a patch that runs before the plot
// module's constructor.
using System;
using System.Reflection;
using Geoland.Shared;
using HarmonyLib;

namespace Geoland.PlotLevels;

internal static class SaveRegistrationPatches
{
    // A new world: start with no levels, and add the save part ahead of the game's.
    [HarmonyPatch]
    private static class WorldStarted
    {
        private static bool Prepare()
        {
            return GameAccess.CanKeepLevels;
        }

        private static MethodBase TargetMethod()
        {
            return GameAccess.PlotModuleConstructor;
        }

        private static void Prefix(SaveModule saveModule)
        {
            try
            {
                LevelMap.Clear();
                LevelSavePart.Register(saveModule);
            }
            catch (Exception e)
            {
                GameHook.Log.LogError("Could not add the plot levels to the save system; levels will not be saved: " + e);
            }
        }

        private static void Postfix(PlotModule __instance)
        {
            __instance.onPlotRemoved += LevelMap.PlotRemoved;
        }
    }

    // The world ends: take the save part out and forget the levels.
    [HarmonyPatch(typeof(PlotModule), nameof(PlotModule.Destroy))]
    private static class WorldEnded
    {
        private static bool Prepare()
        {
            return GameAccess.CanKeepLevels;
        }

        private static void Prefix()
        {
            try
            {
                LevelSavePart.Unregister();
            }
            catch (Exception e)
            {
                GameHook.Log.LogError("Could not take the plot levels out of the save system: " + e);
            }
            LevelMap.Clear();
        }
    }

    // A save is about to be loaded: note which file, so the save part can read its own
    // file from it (see LevelSavePart for why it reads the file itself).
    [HarmonyPatch]
    private static class LoadStarted
    {
        private static bool Prepare()
        {
            return GameAccess.CanKeepLevels;
        }

        private static MethodBase TargetMethod()
        {
            return GameAccess.LoadStarted;
        }

        private static void Prefix(string slotPath)
        {
            LevelSavePart.SaveBeingLoaded = slotPath;
        }
    }
}
