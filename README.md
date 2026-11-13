# The Geoland library: fan-made mods for HavenCraft

Four small mods for HavenCraft, the village-building game by Dionic Software. They are unofficial and made by a fan. HavenCraft is the work of Dionic Software, and this project is not affiliated with them. Published with the developer's permission. Not made, endorsed or supported by Dionic Software.

Written for HavenCraft **Alpha 6.0.5**, on Windows. The library as a whole has a version of its own (see `VERSION` and the Releases page); each mod inside also keeps its own.

| Mod | What it does |
|---|---|
| **Geoland.Planting** | Plant berry bushes, wild cotton and fiber bushes yourself. A key switches planting mode on; a click plants where you aim, for one of the item. |
| **Geoland.Harvest** | Villagers harvest. Farm plots get work orders for berries, honey, wax, fiber, bush wood and wild cotton. |
| **Geoland.PlotLevels** | An upstairs floor can belong to a different plot than the ground below it, so bedrooms can sit above a shop. |
| **Geoland.RecruitWishes** | Once the game's fixed list of joining prices is used up, each traveler asks for something of their own instead of the same thing every time. |

Each mod is a single file and works on its own. Install only the ones you want.

## Before you install

- **Your saves get marked as modded, permanently.** The mods run on [BepInEx](https://github.com/BepInEx/BepInEx), a third-party mod loader. The game detects the mod loader. From then on every save it writes is marked as modded, and that mark cannot be removed, even after the mods and the mod loader are gone. The game's error reports also say that unofficial mods were detected.
- **The mark is caused by the mod loader, not by these mods.** Every world you save while BepInEx is installed gets marked, including worlds in which you never use the mods. Removing only the Geoland DLLs does not stop that. To play unmarked again you must remove BepInEx itself (the files listed under Uninstall). Saves already marked stay marked.
- **Do not send the developer bug reports from a modded game.** If something goes wrong with a mod installed, report it [here](https://github.com/gbutiri/havencraft-geoland-mods/issues) instead. The mods add their own names and versions to the game's list of mods, on purpose, so that anyone looking can see what was installed. This has been seen in a save's description. The error report itself has not been looked at.
- **The game is in alpha.** A game update can break a mod. For the parts of the game they look up by name, the mods are designed to say so in the BepInEx log and switch the affected part off, so that the game behaves as it does without it. This has not been seen with a real game update. If anything behaves oddly after an update, the safe course is to remove the mod (see Uninstall for what that means for a world first).
- **Back up your saves first.** They are in `%USERPROFILE%\AppData\LocalLow\Dionic Software\HavenCraft\saves`, one folder per world. Copy that folder somewhere safe.
- **To keep a world unmarked, play modded in a copy of it.** Before any of this, keep one more backup of the saves folder somewhere outside it, and outside any cloud-synced folder. Do not rely on the copy inside the saves folder as the backup. Then, with the game closed, copy the world's folder inside the saves folder to a new folder name next to it. The game lists worlds by the name of their folder, so give the copy a clearly different folder name, for example the world's name followed by "Modded", and pick that one in the game's world list. This is how the author's own copy shows up; it has not been tried on other setups. Play modded only in the copy, and do not save the original world while BepInEx is installed. A caution: Steam Cloud may sync the saves folder.

## Install

1. Close the game and back up your saves (see above).
2. Download **BepInEx 5**, the Windows x64 build, from <https://github.com/BepInEx/BepInEx/releases>. These mods were made with 5.4.23.6. BepInEx 6 is a different thing and is not what you want.
3. Unzip BepInEx into the game's folder, so that `winhttp.dll` and the `BepInEx` folder sit next to `HavenCraft.exe`. On Steam, right-click the game, then Manage, then Browse local files; the demo's folder is `steamapps\common\HavenCraft Demo`.
4. Start the game once, wait for the main menu, and quit. BepInEx has now created its folders, `BepInEx\plugins` among them.
5. Download the Geoland library zip from the [Releases page](https://github.com/gbutiri/havencraft-geoland-mods/releases) and unzip it anywhere.
6. Put the mods in, one of two ways:
   - **By hand:** copy the `Geoland.*.dll` files you want from the zip's `BepInEx\plugins` folder into the game's `BepInEx\plugins` folder. That is all an install is.
   - **With the installer, if you prefer (optional, Windows only):** double-click `install.cmd` in the unzipped folder. It finds the game through Steam, checks that the game is closed and that BepInEx is there, and copies the four mods into `BepInEx\plugins`. It changes nothing else. It is a plain PowerShell script, `install.ps1`, that you can read before running.
7. Start the game. `BepInEx\LogOutput.log` in the game folder has a line for each mod that loaded, a line containing for example `Geoland.Planting 0.8.1 loaded, 4 patches applied, 0 failed`.

You never need the installer; it only saves the copying. More about it, run from a command prompt in the unzipped folder.

If the game is not found, or found twice, name its folder. Do not end the folder path with a backslash.

```
install.cmd -GameDir "C:\path\to\the game folder"
```

To show what would be copied, and copy nothing:

```
install.cmd -DryRun
```

To remove the four Geoland mods again:

```
install.cmd -Uninstall
```

The installer does not install BepInEx, and it never deletes or overwrites anything except the four Geoland DLLs.

To update, install the new version the same way. The old files are replaced.

## Using the mods

Each mod writes a settings file into `BepInEx\config` the first time it runs. They are plain text; edit them with the game closed.

### Geoland.Planting

Plant things yourself. The item selected in your hotbar decides what gets planted:

| Selected item | Plants | Cost |
|---|---|---|
| Berries | Berry bush | 1 berry |
| Cotton | Wild cotton | 1 cotton |
| Fiber | Fiber bush (the small plain bush) | 1 fiber |

- Press **G** to switch planting mode on and off. The key is ignored while you are typing in a text field or have a menu open.
- With the mode on and one of those items selected, a ghost of the plant shows the spot you are aiming at. Its outline shows whether the spot is good or refused.
- Click to plant. One of the item is used up. With the mode off, or any other item selected, clicks do what they always do.
- Limits: within 12 cells of your character, not too close to water, and not on or beside the spot where another object stands (a large object is checked by its position, not its whole footprint).

Settings, in `geoland.havencraft.planting.cfg`: `ToggleKey` (default `G`).

### Geoland.Harvest

Villagers harvest. Paint a Farm plot over berry bushes, beehives, plain bushes or wild cotton, assign a worker, and add the work orders you want to the farm:

| Work order | What the worker harvests |
|---|---|
| Gather Berries | berry bushes |
| Collect Honey | placed beehives |
| Collect Wax | placed beehives |
| Gather Fiber | the plain bushes |
| Gather Bush Wood | the plain bushes |
| Gather Wild Cotton | wild cotton plants |

- Each order follows its own target and is judged by its own item alone. A full store of honey does not stop the wax being collected.
- Each work order takes its own item, all of it (both fibers from a bush, say), and leaves the object's other items for their own work orders. The order of the work orders in the farm's panel decides which is collected first.
- Optional, off by default: one visit takes everything that fits. With the setting on, a villager who collects an order's item from an object also takes the object's other items in the same visit, each only if all of it fits in their inventory: the wood along with the fiber from a bush, the wax along with the honey from a hive. What does not fit stays on the object for a later visit.
- Items taken along in this way do not count against their own "keep" target, so a store can go over it.
- Each item grows back on its own, after the object's normal refill time.
- Picking by hand an object a villager has partly collected gives only what is left on it.
- Which items have already been taken from an object is not saved. After loading, a full object offers all its items again.
- Wild bee nests and the giant berry bush are left to the player. Villagers do not harvest them.
- Taking everything in one visit and hand-picking giving only what is left are new in 0.9.0 and have not yet been seen working in the game.

Settings, in `geoland.havencraft.harvest.cfg`: in section `Behaviour`, `TakeEverythingThatFits` (default `false`: each order takes only its own item). Set it to `true` for the one-visit behaviour described above. In section `Harvest`, one on/off switch for each kind of harvestable object. All are on by default. The switches for the kinds of object are added to the file the first time a world is loaded. Its switches are named after the game's internal names for the kinds of object.

If you remove the mod: a world saved with these work orders still loads, but that farm's work orders are reset to the game's defaults. This comes from reading how the game loads saves; it has not been tried.

### Geoland.PlotLevels

In the game, every floor of a building belongs to the plot painted on the ground under it. This mod lets an upstairs floor belong to a different plot. It works through the game's own plot painter. With the painter selected:

| Input | What it does |
|---|---|
| **K** | Pick the plot you are aiming at |
| Click on an upstairs floor | Give that floor to the picked plot. Hold and drag to cover more. If the brush starts on a section already painted on that floor, the stroke extends that section instead |
| The painter's erase button on an upstairs floor | Hand that floor back to the plot on the ground |

Aimed at the ground floor or the terrain, the painter does exactly what it always did. While the painter is selected, every upstairs cell that has been given to a plot shows an outlined tile in that plot's colour, and a paler patch shows the brush.

Bedrooms above a shop, step by step:

1. Paint the foot of the stairs on the ground as a Home plot, with the painter as usual. The home needs at least one cell on the ground to exist.
2. Aim at those cells and press K.
3. Go upstairs and click or drag on the bedroom floor.
4. Beds on that floor now count toward the Home plot.

Good to know:

- A level runs from its floor upward until the next painted level above it. A third floor belongs to whatever the second floor was given, until you paint or erase the third floor itself.
- Aim at bare floor. The height is taken from the surface under the crosshair, so aiming at a table top gives the table's height.
- Known rough edge: the painter's own feedback text still describes the ground plot while you aim upstairs.
- Not tested in multiplayer and not expected to work there: the levels are not sent to other players. If you try it anyway, do it on a copy of a world you do not mind losing.
- Levels are stored inside the save file. If you remove the mod, the game ignores them. This comes from reading the game's code; it has not been tried. Two things follow. Without the mod, upstairs objects such as beds count for the plot on the ground again. And the first save made without the mod no longer contains the levels, so they are gone for good.

Settings, in `geoland.havencraft.plotlevels.cfg`: `PickPlot` (the pick key, default `K`) and `MinHeightAboveGround` (default `2`): an upper level must start at least this many blocks above the terrain, and anything lower counts as the ground floor. Lowering or raising it only affects what can be painted from then on; levels already painted are kept.

### Geoland.RecruitWishes

A traveler joins the village when you bring what they ask for. The game has a fixed list of joining prices and uses each once, in order. When the list is used up, every traveler asks for the last one. This mod changes only that repeat:

- Before the game's own list is used up, nothing changes.
- After that, each traveler has a wish of their own: one kind of good a village makes, in an amount a person could carry. Food and farm goods, workshop products, furniture and decoration. No stone or wood.
- A traveler's wish is worked out from that traveler's ID number, so it does not change while you gather it. Nothing is stored in the save.

Settings, in `geoland.havencraft.recruitwishes.cfg`: `Enabled` (default `true`) and `Pool`, the list of goods and amounts, written as `item:min-max` separated by commas. Item names are the game's internal names. Names the game does not know are skipped and listed in the log.

## What is tested and what is not

Tested here means: seen working in the game by the author, on Alpha 6.0.5. The current versions were tidied up after much of that testing, without meaning to change what they do, so the list says which version each thing was seen with.

All four mods load cleanly on Alpha 6.0.5. For Geoland.Harvest that was seen with 0.8.1; 0.9.0 has not yet been run in the game.

| Mod | Seen working | Not yet seen |
|---|---|---|
| **Geoland.Planting** 0.8.1 | Planting berry bushes and fiber bushes (previous version; no change in behaviour intended since) | Planting wild cotton |
| **Geoland.Harvest** 0.9.0 | A farm worker gathering berries up to the order's target, unloading and stopping (seen with 0.8.1). Honey and wax collected (seen with 0.8.0) | Anything with 0.9.0 itself. The fiber, bush wood and wild cotton orders. One visit taking everything that fits. Picking by hand giving only what is left |
| **Geoland.PlotLevels** 0.5.1 | Beds above a shop counting for the home; painting on the second and third floors (previous version). Tiles showing while the painter is out (current version) | A villager sleeping in an upstairs bed |
| **Geoland.RecruitWishes** 0.2.1 | Three travelers recruited through varied wishes (previous version) | A wish staying the same after saving and loading |

Also not yet seen: the mods' names appearing in the game's error report.

Nothing here has been tested in multiplayer. Geoland.PlotLevels is not tested in multiplayer and not expected to work there: the levels are not sent to other players. If you try it anyway, do it on a copy of a world you do not mind losing.

## Uninstall

**Back up your saves first** (see "Before you install"), and read what removing a mod means for a world that used it:

- **Geoland.Harvest:** the farm's work orders are reset to the game's defaults. This comes from reading the game's code; it has not been tried.
- **Geoland.PlotLevels:** the game ignores the levels. This too comes from reading the game's code and has not been tried. Upstairs objects such as beds count for the plot on the ground again. The first save made without the mod no longer contains the levels, so they are gone for good.
- **Geoland.Planting:** plants you planted are ordinary objects of the game and are expected to stay. This has not been tried.
- **Geoland.RecruitWishes:** travelers go back to asking for the game's last joining price, including any traveler who was waiting with a wish. This is expected from how the mod works; it has not been tried.

To remove the mods:

1. Close the game.
2. Delete the four `Geoland.*.dll` files from the game's `BepInEx\plugins` folder, or run `install.cmd -Uninstall`.

That removes the mods. It does not stop the game marking saves: the mod loader causes the mark, and it is still installed.

**Removing BepInEx is a separate step, and it is required if you want new saves to be unmarked:**

1. Close the game.
2. The `BepInEx` folder also holds any other mods you have installed, their settings and the logs. Deleting it deletes those too. Copy anything you need out of it first.
3. Delete these from the game folder: the `BepInEx` folder, `winhttp.dll`, `doorstop_config.ini`, `.doorstop_version` and `changelog.txt`.

Saves written while the mod loader was installed stay marked as modded.

## Troubleshooting

- **The log:** the mods write to `BepInEx\LogOutput.log` in the game folder; the same lines also appear in the game's own `Player.log`. `LogOutput.log` is rewritten each time the game starts. Look there first.
- **There is no `BepInEx\LogOutput.log` at all, or it has no lines from this run:** the mod loader is not running. Check that `winhttp.dll` is next to `HavenCraft.exe` and that you took the x64 build of BepInEx 5.
- **The log is there but has no Geoland lines:** the mod files are not in `BepInEx\plugins`, or they failed to load. If they failed to load, the log has an error naming the file.
- **A line containing `The game has changed:`** means a game update removed or renamed something a mod relied on. The line names what is missing and which part of the mod has been switched off. If anything behaves oddly, remove the mod. Look for a newer version of the mod.
- **A warning that a mod `was built for HavenCraft ... and this is ...`** means your game is a different version from the one the mod was written for. It may still work. If it does not, look for a newer version of the mod.
- **A setting seems to be ignored:** edit settings files with the game closed, and check the file name matches the mod (`geoland.havencraft.<mod>.cfg`).
- **Anything else:** open an issue [here](https://github.com/gbutiri/havencraft-geoland-mods/issues) and attach `LogOutput.log`. The log can contain folder paths that include your Windows user name, so look through it and remove anything personal before posting it publicly. Please do not take it to the developer.

## Building from source

You need the .NET SDK and a HavenCraft game folder with BepInEx 5 installed, because the mods compile against the game's own files. Set the `HAVENCRAFT_GAME_DIR` environment variable to that folder, then build the solution, `Geoland.sln`:

```
dotnet build Geoland.sln -c Release
```

Each mod's file is written to its own `build` folder. If the game folder is not set, the build stops with one line saying so. `Common.props` lists the other ways to name the folder.

`install.ps1` then installs what was built, and `package.ps1` makes the release zip in `dist\`.

Every source file opens with what it is for and how it relates to the game. In each mod's folder, start with `Plugin.cs`. `GameAccess.cs` holds every place where the mod reaches into the game by name, and `CHANGELOG.md` has its history. `Shared/` is compiled into each mod. In the release zip the changelogs are named `CHANGELOG-<mod>.md`, and the source, `Geoland.sln` and `Common.props` are in the repository, not in the zip. Nothing from the game is included in this repository: no code, no data, no assets.

[docs/How-It-Works.md](docs/How-It-Works.md) describes what was learned about how the game is put together, as far as a mod needs it, and how each of these mods hooks in. It may help anyone writing their own.

## How these were made

The ideas, the design decisions and all the testing are the author's (`gbutiri`), from playing the game. The code was written with Claude, Anthropic's AI assistant.

## Licence

MIT. See `LICENSE`.
