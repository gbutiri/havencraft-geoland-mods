// SPDX-License-Identifier: MIT
// Part of the Geoland library: unofficial, fan-made mods for HavenCraft.
// HavenCraft is the work of Dionic Software; this project is not affiliated with them.
//
// HarvestNames: every name this plugin gives to something inside the game.
//
// How it relates to the game: a work order is a "recipe" with an internal name, and the
// game writes that name into save files. A recipe that sends a villager to an object
// also names a task, which the object's adapter is asked about, and a tracking key,
// which marks the kinds of object a plot should keep track of for it. All three are
// built here from the name of the item the work order collects, so that a task name can
// be turned back into its item.
//
// Recipe names begin with "zz" because the game numbers its recipes in alphabetical
// order of their names. Sorting last leaves every one of the game's own recipes with
// the number it has without the plugin.
namespace Geoland.Harvest;

internal static class HarvestNames
{
    private const string RecipePrefix = "zzGeoland_harvest_";
    private const string TaskPrefix = "geolandHarvest_";

    // The game looks a recipe's title up under this prefix plus the recipe's name.
    private const string TitleCodePrefix = "recipe_";

    // Recipe names used before 0.8.1, when the plugin was part of an earlier one. Saves
    // made then still hold them (see SavedNamesPatch).
    private const string OldRecipePrefix = "zzhh_";
    private const string OldGenericRecipePrefix = "zzhh_harvest_";

    /// <summary>The internal name of the work order that collects this item.</summary>
    internal static string RecipeFor(string item)
    {
        return RecipePrefix + item;
    }

    /// <summary>
    /// The task name of the work order that collects this item. The same text serves as
    /// its tracking key and as the name of the spots where a villager stands to do it.
    /// </summary>
    internal static string TaskFor(string item)
    {
        return TaskPrefix + item;
    }

    /// <summary>The item a task collects, or null if the task is not one of this plugin's.</summary>
    internal static string ItemOfTask(string taskName)
    {
        if (taskName == null || !taskName.StartsWith(TaskPrefix)) return null;
        return taskName.Substring(TaskPrefix.Length);
    }

    /// <summary>The title of a work order as the player sees it.</summary>
    internal static string TitleFor(string item)
    {
        switch (item)
        {
            case "berry": return "Gather Berries";
            case "honey": return "Collect Honey";
            case "wax": return "Collect Wax";
            case "fiber": return "Gather Fiber";
            case "wood": return "Gather Bush Wood";
            case "cotton": return "Gather Wild Cotton";

            // A kind of object added by a game update gets a plain title.
            default: return "Harvest " + item;
        }
    }

    /// <summary>
    /// The item whose work order title the game is asking for, or null if the code is not
    /// the title of one of this plugin's work orders.
    /// </summary>
    internal static string ItemOfTitleCode(string code)
    {
        const string prefix = TitleCodePrefix + RecipePrefix;
        if (code == null || !code.StartsWith(prefix)) return null;
        return code.Substring(prefix.Length);
    }

    /// <summary>
    /// The item a recipe name from before 0.8.1 stood for, or null if the name is not one
    /// of those.
    /// </summary>
    internal static string ItemOfOldRecipe(string recipeName)
    {
        if (recipeName == null || !recipeName.StartsWith(OldRecipePrefix)) return null;
        switch (recipeName)
        {
            // The first three work orders had names of their own.
            case "zzhh_gatherBerries": return "berry";
            case "zzhh_collectHoney": return "honey";
            case "zzhh_harvest_fiber_wood": return "fiber";
        }
        if (!recipeName.StartsWith(OldGenericRecipePrefix)) return null;
        return recipeName.Substring(OldGenericRecipePrefix.Length);
    }
}
