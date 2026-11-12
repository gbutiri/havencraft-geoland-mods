// SPDX-License-Identifier: MIT
// Part of the Geoland library: unofficial, fan-made mods for HavenCraft.
// HavenCraft is the work of Dionic Software; this project is not affiliated with them.
//
// GameHook: how a Geoland plugin starts up and reaches the running game.
//
// The files in this folder are compiled into every plugin instead of being shipped as a
// separate library, so each plugin is a single file that works on its own.
//
// How it relates to the game: HavenCraft keeps a world's systems in modules held by a
// WorldOrchestrator. The game's official mod interface is planned for later and does not
// yet cover what these mods need, so this class finds the orchestrator itself, and the
// plugins add themselves to the game's own list of mods so that they run on the game's
// schedule (see RegisterModPatch). It also applies a plugin's patches one class
// at a time, so that a patch which no longer fits the game is reported and skipped
// without taking the plugin's other patches down with it.
using System;
using System.Reflection;
using BepInEx.Logging;
using Dionic.ModAPI_1;
using HarmonyLib;

namespace Geoland.Shared;

internal static class GameHook
{
    /// <summary>The game version these plugins were written and tested against.</summary>
    internal const string BuiltForGameVersion = "Alpha 6.0.5";

    /// <summary>The plugin's log. Set by the plugin before anything else is called.</summary>
    internal static ManualLogSource Log;

    /// <summary>
    /// Set by the plugin: creates its mod object. The game is given a new one for each
    /// world it loads (see <see cref="RegisterModPatch"/>).
    /// </summary>
    internal static Func<IMod> CreateMod;

    /// <summary>
    /// The plugin's name and version, as given to <see cref="ApplyPatches"/>. It is the
    /// name the plugin goes by in the game's list of mods (see <see cref="AnnounceModPatch"/>).
    /// </summary>
    internal static string PluginLabel { get; private set; }

    private static FieldInfo gameOrchestratorInstance;
    private static FieldInfo gameOrchestratorWorld;

    /// <summary>
    /// Applies every Harmony patch class in the plugin, each on its own, and reports the
    /// result in one log line. Call once, from the plugin's Awake.
    /// </summary>
    /// <param name="harmonyId">The plugin's ID, which names its patches to Harmony.</param>
    /// <param name="label">The plugin's name and version, for the log and for the game's list of mods.</param>
    internal static void ApplyPatches(string harmonyId, string label)
    {
        PluginLabel = label;
        WarnIfGameVersionDiffers(label);
        ResolveWorldAccess();

        var harmony = new Harmony(harmonyId);
        int applied = 0, failed = 0;
        foreach (Type type in typeof(GameHook).Assembly.GetTypes())
        {
            if (type.GetCustomAttributes(typeof(HarmonyPatch), false).Length == 0) continue;
            try
            {
                harmony.CreateClassProcessor(type).Patch();
                applied++;
            }
            catch (Exception e)
            {
                failed++;
                Log.LogError("Patch " + type.Name + " failed: " + e);
            }
        }
        Log.LogInfo(label + " loaded, " + applied + " patches applied, " + failed + " failed");
    }

    /// <summary>The world that is currently loaded.</summary>
    internal static WorldOrchestrator World()
    {
        if (gameOrchestratorInstance == null || gameOrchestratorWorld == null)
        {
            throw new InvalidOperationException(
                "The game's world cannot be reached. See the earlier 'game has changed' line in this log.");
        }
        object gameOrchestrator = gameOrchestratorInstance.GetValue(null);
        return (WorldOrchestrator)gameOrchestratorWorld.GetValue(gameOrchestrator);
    }

    /// <summary>One of the loaded world's modules, by type.</summary>
    internal static T Module<T>() where T : IWorldModule
    {
        return World().GetModule<T>();
    }

    private static void ResolveWorldAccess()
    {
        const string neededFor = "reaching the game's modules";
        gameOrchestratorInstance = GameMembers.Field(typeof(GameOrchestrator), "instance", typeof(object), neededFor);
        gameOrchestratorWorld = GameMembers.Field(typeof(GameOrchestrator), "worldOrchestrator", typeof(WorldOrchestrator), neededFor);
    }

    private static void WarnIfGameVersionDiffers(string label)
    {
        // The game's own name for its version, the one it writes into saves and its log.
        string running = Versions.GetVersion();
        if (running == BuiltForGameVersion)
        {
            Log.LogInfo("HavenCraft " + running + ", the version " + label + " was built for");
            return;
        }
        Log.LogWarning(label + " was built for HavenCraft " + BuiltForGameVersion + " and this is " + running
            + ". Parts of it may not work. Look for a newer version of the plugin.");
    }
}
