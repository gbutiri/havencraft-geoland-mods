# The Geoland library: fan-made mods for HavenCraft

Four small mods for HavenCraft, the village-building game by Dionic Software. They are unofficial and made by a fan. HavenCraft is the work of Dionic Software, and this project is not affiliated with them. The developer has kindly said these may be published.

Written for HavenCraft **Alpha 6.0.5**, on Windows.

| Mod | What it does |
|---|---|
| **Geoland.Planting** | Plant berry bushes, wild cotton and fiber bushes yourself. A key switches planting mode on; a click plants where you aim, for one of the item. |
| **Geoland.Harvest** | Villagers harvest. Farm plots get work orders for berries, honey, wax, fiber, bush wood and wild cotton. |
| **Geoland.PlotLevels** | An upstairs floor can belong to a different plot than the ground below it, so bedrooms can sit above a shop. |
| **Geoland.RecruitWishes** | Once the game's fixed list of joining prices is used up, each traveler asks for something of their own instead of the same thing every time. |

Each mod is a single file and works on its own. Install only the ones you want.

## Before you install

- **Your saves get marked as modded, permanently.** The mods run on [BepInEx](https://github.com/BepInEx/BepInEx), a third-party mod loader. The game notices the mod loader. From then on every save it writes is marked as modded, and that mark cannot be removed, even after the mods and the mod loader are gone. The game's error reports also say that unofficial mods were detected.
- **Do not send the developer bug reports from a modded game.** If something goes wrong with a mod installed, report it [here](https://github.com/gbutiri/havencraft-geoland-mods/issues) instead. The mods add their own names and versions to the game's error report and to the save's description, on purpose, so that anyone looking can see what was installed.
- **The game is in alpha.** A game update can break a mod. When that happens the mod says so in the BepInEx log and switches the affected part off, so the game behaves as it does without it.
- **Back up your saves first.** They are in `%USERPROFILE%\AppData\LocalLow\Dionic Software\HavenCraft\saves`, one folder per world. Copy that folder somewhere safe. If you want to keep a world unmarked, play modded in a copy of it.

## Install

1. Close the game and back up your saves (see above).
2. Download **BepInEx 5**, the Windows x64 build, from <https://github.com/BepInEx/BepInEx/releases>. These mods were made with 5.4.23.6. BepInEx 6 is a different thing and is not what you want.
3. Unzip BepInEx into the game's folder, so that `winhttp.dll` and the `BepInEx` folder sit next to `HavenCraft.exe`. On Steam, right-click the game, then Manage, then Browse local files; the demo's folder is `steamapps\common\HavenCraft Demo`.
4. Start the game once, wait for the main menu, and quit. BepInEx has now created its folders, `BepInEx\plugins` among them.
5. Download the Geoland library zip from the [Releases page](https://github.com/gbutiri/havencraft-geoland-mods/releases) and unzip it anywhere.
6. Put the mods in, one of two ways:
   - **With the installer:** double-click `install.cmd` in the unzipped folder. It finds the game through Steam, checks that the game is closed and that BepInEx is there, and copies the four mods into `BepInEx\plugins`. It changes nothing else.
   - **By hand:** copy the `Geoland.*.dll` files you want from the zip's `BepInEx\plugins` folder into the game's `BepInEx\plugins` folder.
7. Start the game. `BepInEx\LogOutput.log` in the game folder has a line for each mod that loaded, such as `Geoland.Planting 0.8.1 loaded`.

More about the installer, run from a command prompt in the unzipped folder:

```
install.cmd -GameDir "C:\path\to\the game folder"    if the game is not found, or found twice
install.cmd -DryRun                                   show what would be copied, copy nothing
install.cmd -Uninstall                                remove the four Geoland mods again
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
- Limits: within 12 cells of your character, not too close to water, and not right next to another object.

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

- Each item is collected on its own. Taking the wax leaves the honey in the hive, and the other way round, and each order follows its own target.
- Each item grows back on its own, after the object's normal refill time.
- Which items have already been taken from an object is not saved. After loading, a full object offers all its items again. Harvesting by hand always gives everything.
- Wild bee nests and the giant berry bush are left to the player. Villagers do not harvest them.

Settings, in `geoland.havencraft.harvest.cfg`: one on/off switch for each kind of harvestable object. All are on by default.

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
- Single-player only.
- Levels are stored inside the save file. If you remove the mod, the game ignores them and upstairs objects fall back to the plot on the ground.

Settings, in `geoland.havencraft.plotlevels.cfg`: `PickPlot` (the pick key, default `K`) and `MinHeightAboveGround` (default `2`): an upper level must start at least this many blocks above the terrain, and anything lower counts as the ground floor.

### Geoland.RecruitWishes

A traveler joins the village when you bring what they ask for. The game has a fixed list of joining prices and uses each once, in order. When the list is used up, every traveler asks for the last one. This mod changes only that repeat:

- Before the game's own list is used up, nothing changes.
- After that, each traveler has a wish of their own: one kind of good a village makes, in an amount a person could carry. Food and farm goods, workshop products, furniture and decoration. Nothing raw such as stone or wood.
- A traveler's wish is worked out from that traveler's ID number, so it does not change while you gather it. Nothing is stored in the save.

Settings, in `geoland.havencraft.recruitwishes.cfg`: `Enabled` (default `true`) and `Pool`, the list of goods and amounts, written as `item:min-max` separated by commas. Item names are the game's internal names. Names the game does not know are skipped and listed in the log.

## What is tested and what is not

Tested here means: seen working in the game by the author, on Alpha 6.0.5. The current versions were tidied up after much of that testing, without meaning to change what they do, so the list says which version each thing was seen with.

All four mods, current versions, load cleanly on Alpha 6.0.5.

| Mod | Seen working | Not yet seen |
|---|---|---|
| **Geoland.Planting** 0.8.1 | Planting berry bushes and fiber bushes (previous version; no change in behaviour intended since) | Planting wild cotton |
| **Geoland.Harvest** 0.8.1 | A farm worker gathering berries up to the order's target, unloading and stopping (current version). Honey and wax collected (previous version) | The fiber, bush wood and wild cotton orders |
| **Geoland.PlotLevels** 0.5.1 | Beds above a shop counting for the home; painting on the second and third floors (previous version). Tiles showing while the painter is out (current version) | A villager sleeping in an upstairs bed |
| **Geoland.RecruitWishes** 0.2.1 | Three travelers recruited through varied wishes (previous version) | A wish staying the same after saving and loading |

Nothing here has been tested in multiplayer. Geoland.PlotLevels is single-player only.

## Uninstall

1. Close the game.
2. Delete the four `Geoland.*.dll` files from the game's `BepInEx\plugins` folder, or run `install.cmd -Uninstall`.
3. To remove the mod loader too, delete these from the game folder: the `BepInEx` folder, `winhttp.dll`, `doorstop_config.ini`, `.doorstop_version` and `changelog.txt`.

Saves written while the mod loader was installed stay marked as modded.

## Troubleshooting

- **The log** is `BepInEx\LogOutput.log` in the game folder. It is rewritten each time the game starts. Look there first.
- **No Geoland lines in the log, or no log at all:** BepInEx is not running. Check that `winhttp.dll` is next to `HavenCraft.exe`, that you took the x64 build of BepInEx 5, and that the DLLs are in `BepInEx\plugins`.
- **A line starting `The game has changed:`** means a game update removed or renamed something a mod relied on. The line names what is missing and which part of the mod has been switched off. The rest of the game is unaffected. Look for a newer version of the mod.
- **A warning that a mod `was built for HavenCraft ... and this is ...`** means your game is a different version from the one the mod was written for. It may still work. If it does not, look for a newer version of the mod.
- **A setting seems to be ignored:** edit settings files with the game closed, and check the file name matches the mod (`geoland.havencraft.<mod>.cfg`).
- **Anything else:** open an issue [here](https://github.com/gbutiri/havencraft-geoland-mods/issues) and attach `LogOutput.log`. Please do not take it to the developer.

## Building from source

You need the .NET SDK and a HavenCraft game folder with BepInEx 5 installed, because the mods compile against the game's own files. Set the `HAVENCRAFT_GAME_DIR` environment variable to that folder, then build the solution, `Geoland.sln`:

```
dotnet build Geoland.sln -c Release
```

Each mod's file is written to its own `build` folder. If the game folder is not set, the build stops with one line saying so. `Common.props` lists the other ways to name the folder.

`install.ps1` then installs what was built, and `package.ps1` makes the release zip in `dist\`.

Every source file opens with what it is for and how it relates to the game. In each mod's folder, start with `Plugin.cs`. `GameAccess.cs` holds every place where the mod reaches into the game by name, and `CHANGELOG.md` has its history. `Shared/` is compiled into each mod. Nothing from the game is included in this repository: no code, no data, no assets.

[docs/How-It-Works.md](docs/How-It-Works.md) describes what was learned about how the game is put together, as far as a mod needs it, and how each of these mods hooks in. It may help anyone writing their own.

## How these were made

The ideas, the design decisions and all the testing are the author's (`gbutiri`), from playing the game. The code was written with Claude, Anthropic's AI assistant.

## Licence

MIT. See `LICENSE`.
