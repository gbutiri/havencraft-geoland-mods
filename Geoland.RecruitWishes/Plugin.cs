// SPDX-License-Identifier: MIT
// Part of the Geoland library: unofficial, fan-made mods for HavenCraft.
// HavenCraft is the work of Dionic Software; this project is not affiliated with them.
//
// Plugin: the entry point of Geoland.RecruitWishes, and its settings.
//
// What the plugin does: in HavenCraft a traveler joins the village when the player
// brings what the traveler asks for. The game has a fixed list of joining prices and uses
// each once, in order. When the list is used up, every traveler from then on asks for the
// last one, so in a grown village it is always the same request. This plugin changes only
// that repeat: from then on each traveler has a wish of their own, one kind of good the
// village makes, in an amount a person could carry.
//
// How it relates to the game: BepInEx, the mod loader, creates this class when the game
// starts. It reads the settings, checks that the parts of the game the plugin relies on
// are still there (GameAccess), and applies the two patches on the game's traveler module
// (WishedPricePatch and WishPaidPatch).
using BepInEx;
using BepInEx.Configuration;
using Geoland.Shared;

namespace Geoland.RecruitWishes;

[BepInPlugin(Id, Name, Version)]
public sealed class Plugin : BaseUnityPlugin
{
    /// <summary>Names the plugin to the mod loader, and names its settings file.</summary>
    public const string Id = "geoland.havencraft.recruitwishes";
    public const string Name = "Geoland.RecruitWishes";
    public const string Version = "0.2.1";

    private const string SettingsSection = "Wishes";

    // The goods a traveler may wish for, each with the smallest and largest amount.
    //
    // What is in it: things a working village makes. Food and farm goods, workshop
    // products, and furniture and decoration. Nothing raw such as stone or wood, which
    // the game's own early prices already cover, and nothing rare enough to stall a
    // village that does not happen to make it.
    //
    // How the amounts were chosen: by how the good is made and carried. Food and farm
    // goods come in bulk, so they run up to 100. Workshop products take more work each,
    // so they run to a few dozen. Furniture and decoration are built one at a time, so a
    // wish is for a handful.
    private const string DefaultPool =
        "honey:20-100, berry:20-100, bread:10-60, flour:20-60, " +
        "candle:10-50, soap:10-40, wax:20-60, cloth:10-40, fiber:20-60, " +
        "sewingKit:5-15, glass:10-40, gears:5-20, copperIngot:5-20, book:3-10, " +
        "candleStand:2-6, chandelier:2-5, torch:5-15, streetlamp:2-5, " +
        "painting:1-3, bannerStanding:2-5, bannerWall:2-6, carpetSmall:2-5, carpetLarge:1-3, " +
        "chairBasic:3-8, tableBasic:2-5, plantVaseWood:3-8";

    internal static ConfigEntry<bool> Enabled { get; private set; }
    internal static ConfigEntry<string> Pool { get; private set; }

    private void Awake()
    {
        GameHook.Log = Logger;

        Enabled = Config.Bind(SettingsSection, "Enabled", true,
            "Give each traveler a wish of their own once the game's fixed list of joining prices is used up.");
        Pool = Config.Bind(SettingsSection, "Pool", DefaultPool,
            "What travelers may wish for: item:min-max, separated by commas. Item names are the game's internal names. Unknown names are skipped and listed in the log.");

        GameAccess.Resolve();
        GameHook.CreateMod = () => new RecruitWishesMod();
        GameHook.ApplyPatches(Id, Name + " " + Version);
    }
}
