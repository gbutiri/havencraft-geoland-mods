// SPDX-License-Identifier: MIT
// Part of the Geoland library: unofficial, fan-made mods for HavenCraft.
// HavenCraft is the work of Dionic Software; this project is not affiliated with them.
//
// LevelSavePart: stores the upper levels inside the save file, and reads them back.
//
// How it relates to the game: the game builds a save from parts. Each part (the game's
// ISavable interface) writes one named file into the save's zip, and on loading the
// game hands each part its file. This class is one more part, so the levels travel
// with the save itself and need no file of their own on disk.
//
// Writing goes through the game. Reading does not: the game logs an error when a part's
// file is missing from a save, and every save made without the plugin has none. So the
// part tells the game it has no file to load, and opens the save's zip itself. A save
// without the file simply has no levels.
//
// A save that holds the file loads normally in a game without the plugin: the game
// only opens the files its own parts ask for.
using System;
using System.IO;
using System.IO.Compression;
using Geoland.Shared;
using Newtonsoft.Json;

namespace Geoland.PlotLevels;

internal sealed class LevelSavePart : ISavable
{
    private const string FileName = "geolandPlotLevels.json";

    // The file's name before the plugin joined the Geoland library. It is still read,
    // so saves from then keep their levels. The next save writes the current name.
    private const string FileNameBeforeRename = "havenPlotLevels.json";

    private static LevelSavePart registered;
    private static SaveModule registeredWith;

    private LevelSaveData prepared;

    /// <summary>The save file the game is loading, noted when the load starts (SaveRegistrationPatches).</summary>
    internal static string SaveBeingLoaded { get; set; }

    /// <summary>Adds the part to a new world's save system.</summary>
    internal static void Register(SaveModule saveModule)
    {
        registered = new LevelSavePart();
        registeredWith = saveModule;
        saveModule.AddSavable(registered);
    }

    /// <summary>Takes the part out again when the world ends, as the game requires.</summary>
    internal static void Unregister()
    {
        try
        {
            if (registered != null && registeredWith != null) registeredWith.RemoveSavable(registered);
        }
        finally
        {
            registered = null;
            registeredWith = null;
        }
    }

    public string GetSaveFileName()
    {
        return FileName;
    }

    public bool DoesWriteToSave()
    {
        return true;
    }

    public bool ShouldExistInVersion(string saveVersion)
    {
        return true;
    }

    // False sends the game to LoadWithoutFile below instead of looking for the file.
    public bool DoesLoadFromFile()
    {
        return false;
    }

    public void PrepareSaveMainThread()
    {
        prepared = LevelMap.ToSaveData(GameHook.Module<PlotModule>());
    }

    public void PrepareSaveParallel()
    {
    }

    public void WritePreparedDataToSave(Stream stream, JsonSerializer jsonSerializer)
    {
        using (var writer = new StreamWriter(stream))
        {
            writer.Write(JsonConvert.SerializeObject(prepared ?? new LevelSaveData()));
        }
        prepared = null;
    }

    public void ReadFromSave(Stream stream, JsonSerializer jsonSerializer, string saveVersion)
    {
        throw new NotSupportedException("Not used: this part reads its own file in LoadWithoutFile.");
    }

    public void LoadWithoutFile(string saveVersion)
    {
        LevelMap.Clear();
        string path = SaveBeingLoaded;
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;
        try
        {
            // The game has the save open as well, so it is opened for shared reading.
            using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var zip = new ZipArchive(file, ZipArchiveMode.Read))
            {
                ZipArchiveEntry entry = zip.GetEntry(FileName) ?? zip.GetEntry(FileNameBeforeRename);
                if (entry == null) return;

                using (var reader = new StreamReader(entry.Open()))
                {
                    LevelMap.FromSaveData(JsonConvert.DeserializeObject<LevelSaveData>(reader.ReadToEnd()));
                }
            }
            GameHook.Log.LogInfo("loaded upper levels for " + LevelMap.CellCount + " cells from the save");
        }
        catch (Exception e)
        {
            LevelMap.Clear();
            GameHook.Log.LogError("Could not read the plot levels from the save; continuing without levels: " + e);
        }
    }

    public void Destroy()
    {
    }
}
