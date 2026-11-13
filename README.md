# The Geoland library: fan-made mods for HavenCraft

Five small mods for HavenCraft, the village-building game by Dionic Software. They are unofficial and made by a fan. HavenCraft is the work of Dionic Software, and this project is not affiliated with them. Published with the developer's permission. Not made, endorsed or supported by Dionic Software.

Written for HavenCraft **Alpha 6.0.5**, on Windows. The library as a whole has a version of its own (see `VERSION` and the Releases page); each mod inside also keeps its own.

| Mod | What it does |
|---|---|
| **Geoland.Planting** | Plant berry bushes, wild cotton and fiber bushes yourself. A key switches planting mode on; a click plants where you aim, for one of the item. |
| **Geoland.Harvest** | Villagers harvest. Farm plots get work orders for berries, honey, wax, fiber, bush wood and wild cotton. |
| **Geoland.PlotLevels** | An upstairs floor can belong to a different plot than the ground below it, so bedrooms can sit above a shop. |
| **Geoland.RecruitWishes** | Once the game's fixed list of joining prices is used up, each traveler asks for something of their own instead of the same thing every time. |
| **Geoland.Mines** | A Tree Stake placed on a Miner Zone becomes a mine. A rock appears in it for your miners, and a new one some time after it is mined out. |

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
   - **With the installer, if you prefer (optional, Windows only):** double-click `install.cmd` in the unzipped folder. It finds the game through Steam, checks that the game is closed and that BepInEx is there, and copies the five mods into `BepInEx\plugins`. It changes nothing else. It is a plain PowerShell script, `install.ps1`, that you can read before running.
7. Start the game. `BepInEx\LogOutput.log` in the game folder has a line for each mod that loaded, a line containing for example `Geoland.Planting 0.8.2 loaded, 4 patches applied, 0 failed`.

You never need the installer; it only saves the copying. More about it, run from a command prompt in the unzipped folder.

If the game is not found, or found twice, name its folder. Do not end the folder path with a backslash.

```
install.cmd -GameDir "C:\path\to\the game folder"
```

To show what would be copied, and copy nothing:

```
install.cmd -DryRun
```

To remove the five Geoland mods again:

```
install.cmd -Uninstall
```

The installer does not install BepInEx, and it never deletes or overwrites anything except the five Geoland DLLs.

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
- A new plant starts empty. It grows its first crop after the plant's normal refill time, like something you build.
- Limits: within 12 cells of your character, not too close to water, and not on or beside the spot where another object stands (a large object is checked by its position, not its whole footprint).

Settings, in `geoland.havencraft.planting.cfg`: `ToggleKey` (default `G`). Set it to `None` to switch the key off.

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
- If the worker has no room for all of an item, it stays on the object for a later visit.
- Optional, off by default: one visit takes everything that fits. With the setting on, a villager who collects an order's item from an object also takes the object's other items in the same visit, each only if all of it fits in their inventory: the wood along with the fiber from a bush, the wax along with the honey from a hive. What does not fit stays on the object for a later visit.
- Items taken along in this way do not count against their own "keep" target, so a store can go over it.
- Each item grows back on its own, after the object's normal refill time.
- Picking by hand an object a villager has partly collected gives only what is left on it.
- Which items have already been taken from an object is not saved. After loading, a full object offers all its items again.
- Wild bee nests and the giant berry bush are left to the player. Villagers do not harvest them.
- Taking everything in one visit and hand-picking giving only what is left are new in 0.9.0. Both have been seen working in the game.

Settings, in `geoland.havencraft.harvest.cfg`: in section `Behaviour`, `TakeEverythingThatFits` (default `false`: each order takes only its own item). Set it to `true` for the one-visit behaviour described above. In section `Harvest`, one on/off switch for each kind of harvestable object. All are on by default. The switches for the kinds of object are added to the file the first time a world is loaded. Its switches are named after the game's internal names for the kinds of object.

If you remove the mod: a world saved with these work orders still loads, but that farm's work orders are reset to the game's defaults. This comes from reading how the game loads saves; it has not been tried.

### Geoland.PlotLevels

In the game, every floor of a building belongs to the plot painted on the ground under it. This mod lets an upstairs floor belong to a different plot, so the bedrooms of a home can sit above a shop. It works through the game's own plot painter, and only while the painter is the selected item.

**The idea in three lines**

1. Plots live on the ground. You make them with the painter, as you always did.
2. **K** picks a plot. Think of it as picking a colour.
3. A click on an upstairs floor gives that floor to the picked plot. That is the brush.

**Controls**, with the plot painter selected:

| Input | What it does |
|---|---|
| **K** | Pick the plot you are aiming at, on the ground or upstairs. A notice names the plot and its number. The key is ignored while you are typing in a text field or have a menu open |
| Click on an upstairs floor | Give that floor to the picked plot. Hold and drag to cover more. If the brush starts on a section already painted on that floor, the stroke extends that section instead |
| Aim at an upstairs floor, then hold the painter's erase button | Hand that floor back to the plot on the ground beneath it |

A stroke that begins on the ground floor or the terrain paints the ground as it always did.

While the painter is selected, every upstairs cell that has been given to a plot shows an outlined tile in that plot's colour. A paler patch shows the brush. The patch has the picked plot's colour, or plain white when no plot is picked. Select another item and the tiles are hidden. They are not gone.

**A home above a shop, step by step**

You start with a building of two floors. The ground under it is painted as the shop's plot, and the second floor is empty. So far the second floor belongs to the shop too.

1. Select the plot painter.
2. Paint a Home plot on the ground, with the painter as usual. One cell is enough, for example the foot of the stairs. The home must have this base on the ground, or it cannot exist.
3. Aim at that ground cell and press **K**. A notice names the plot you picked. Check that it is the home.
4. Go upstairs. Aim at the bare floor, not at a wall or a piece of furniture. The brush patch now lies on the floor you are standing on.
5. Click, or hold and drag across the room. A notice says how many cells now belong to the home, and outlined tiles appear on them.
6. Place a bed on the painted floor. It counts for the home, not for the shop. A bed that already stood there moves over to the home when you paint under it.
7. Assign a villager to the home as you would for any home. The bed upstairs is one of the home's beds.

**Rules worth knowing**

- **A plot needs a base on the ground.** An upstairs floor can only be given to a plot that has at least one cell on the ground somewhere. You cannot create a new plot upstairs.
- **Lose the base, lose the upstairs.** If a plot is deleted, or its last ground cell is erased, its upstairs floors go with it. They go back to whatever is beneath them, and beds and other objects up there count for that plot from then on.
- **A stroke stays on the kind of floor it began on.** A stroke begun upstairs does nothing on a wall, past the floor's edge or on the ground. A stroke begun on the ground does nothing on an upstairs floor. To switch, release the button and press again.
- **Where the stroke begins decides everything.** If you press the button while aiming at a wall or at the ground, it is a ground stroke, even when you are standing upstairs. Look where the brush patch lies before you press.
- **To erase upstairs, aim first.** Aim at the upstairs floor, then hold the painter's erase button. Each cell you erase goes back to the plot on the ground beneath it.
- **Painting a cell with the plot it already has changes nothing and says nothing.** This includes a floor directly above the plot's own ground cells. It belongs to that plot already, so no tile appears there.
- **A stroke that starts on a painted section extends that section.** It uses that section's plot, not the picked one, and that plot becomes the picked one. To paint over a section with another plot, pick the other plot and start the stroke on unpainted floor, then drag in.
- **With no plot picked, a click on bare upstairs floor paints nothing.** A notice tells you to pick one first.
- **Colours can mislead.** The tiles use the game's own plot colours, and two plots can have very similar ones. The notice from **K** is the reliable way to tell which plot a cell belongs to.
- A level runs from its floor upward until the next painted level above it. A third floor belongs to whatever the second floor was given, until you paint or erase the third floor itself.
- Aim at bare floor. The height is taken from the surface under the crosshair, so aiming at a table top gives the table's height.
- A floor less than 2 blocks above the terrain counts as the ground floor. The painter paints the ground there, as usual. The number is a setting, see below.

**If something looks wrong**

- **Nothing happens when I click upstairs.** Either no plot is picked (the brush patch is white, and a notice says to pick one), or the floor already belongs to the picked plot, or the stroke began on a wall or on the ground. Aim at the plot's ground cells and press **K**, aim at bare floor, then press the button again.
- **The upstairs tiles vanished.** If the painter is not the selected item, select it and they are back. If they are still missing, the plot lost its last ground cell or was deleted, and its upstairs floors went with it. Paint the plot on the ground again, press **K** on it, and paint the upstairs floor again.
- **I painted the ground by accident.** The stroke began with the brush on the ground: you aimed at a wall, through a gap, or at a floor too low to count as upstairs. Repair the ground with the painter as usual. Then aim at the upstairs floor, check that the brush patch lies on it, and press again.
- **I cannot tell two colours apart.** Aim at the cell and press **K**. The notice names the plot and gives its number. This also makes that plot the picked one, so pick the plot you want to paint with again before you go on.

**Limits**

- Known rough edge: the painter's own feedback text still describes the ground plot while you aim upstairs.
- Not tested in multiplayer and not expected to work there: the levels are not sent to other players. If you try it anyway, do it on a copy of a world you do not mind losing.
- Levels are stored inside the save file. If you remove the mod, the game ignores them. This comes from reading the game's code; it has not been tried. Two things follow. Without the mod, upstairs objects such as beds count for the plot on the ground again. And the first save made without the mod no longer contains the levels, so they are gone for good.

Settings, in `geoland.havencraft.plotlevels.cfg`: `PickPlot` (the pick key, default `K`; set it to `None` to switch the key off) and `MinHeightAboveGround` (default `2`): an upper level must start at least this many blocks above the terrain, and anything lower counts as the ground floor. Lowering or raising it only affects what can be painted from then on; levels already painted are kept.

### Geoland.RecruitWishes

A traveler joins the village when you bring what they ask for. The game has a fixed list of joining prices and uses each once, in order. When the list is used up, every traveler asks for the last one. This mod changes only that repeat:

- Before the game's own list is used up, nothing changes.
- After that, each traveler has a wish of their own: one kind of good a village makes, in an amount a person could carry. Food and farm goods, workshop products, furniture and decoration. No stone or wood.
- A traveler's wish is worked out from that traveler's ID number, so it does not change while you gather it. Nothing is stored in the save.

Settings, in `geoland.havencraft.recruitwishes.cfg`: `Enabled` (default `true`) and `Pool`, the list of goods and amounts, written as `item:min-max` separated by commas. Item names are the game's internal names. Names the game does not know are skipped and listed in the log.

### Geoland.Mines

In the game, a rock that has been mined out is gone for good, so a village's stone runs out near home. This mod makes a mine out of two things the game already has: the Tree Stake, the craftable stake that woodcutters sow trees in, and the Miner Zone. A Tree Stake placed on a Miner Zone is a mine. One of the game's own rocks appears in the middle of it, your miners mine it, and some time after it is mined out the next one appears.

**This makes stone unlimited, which changes the game's balance.** How long a mine waits and which rock it gets are both settings, see below.

**A mine, step by step**

You need what the game itself asks for: the techs that unlock the Tree Stake and the Miner Zone's "Mine Rocks" order.

1. Paint a Miner Zone, or use one you have.
2. Craft a Tree Stake and place it on the Miner Zone.
3. Wait. With the default settings the first rock appears after three and a half minutes of play.
4. Assign a worker to the Miner Zone and switch its "Mine Rocks" order on, as you would for any rocks on the plot.
5. When the rock is mined out, the stake is empty again and the wait starts again. Then the next rock appears.

When you load a world that has a Miner Zone, a notice says that the mod is active.

**Rules worth knowing**

- **Only Tree Stakes on a Miner Zone are mines.** A Tree Stake on any other kind of plot is never touched, so woodcutters' stakes work as they always did.
- **One rock per stake.** A mine that holds a rock gets no second one.
- **The wait starts when the mine is empty.** That is when the mod first sees an empty stake, and again each time a rock is gone. It makes no difference who broke the rock, a villager or you.
- **The wait only runs while the game runs.** It is counted in the game's own ticks, which stop while the game is paused.
- **The wait is not kept in the save.** After loading, an empty mine waits the whole time again. A mine that held a rock when you saved is expected to hold it still, because the game itself saves which stake holds what. This comes from reading the game's code; it has not been seen.
- **You can still sow a sapling by hand in an empty mine.** The mine then waits until the sapling or the tree is gone, and after that for the usual time. No villager sows a mine.
- **The rocks are ordinary rocks.** The mod brings none of its own. It asks the game to place one of the rocks the game already has.
- If you repaint the plot as something else, or take the stake away from the Miner Zone, it is no longer a mine. The mod does not remove a rock that is already there.
- Nothing of the mod's is stored in the save, so the mod can be removed at any time.
- The game may get mines of its own one day. This mod can simply be removed then.

**Limits**

- Villagers mining the rock in a stake has not yet been seen. See "What is tested and what is not".
- The larger rocks, `rockA` and `rockB`, are two cells wide and may not fit inside the stake. They have not been tried.
- Not tested in multiplayer. The mod is written to act only for the host of a game.
- The BepInEx log has a line for each rock placed and for each rock found gone, with the stake's position and the plot's number.

Settings, in `geoland.havencraft.mines.cfg`, section `Mines`:

- `Enabled` (default `true`). Set it to `false` and no rocks are placed.
- `Rock` (default `rockC`): which of the game's rocks appears, by the game's internal name, or `random`. `rockC` is the medium rock and gives 45 stone. `rockD` and `rockE` are small and give 21 each. `random` picks afresh each time, a medium or a small rock with an even chance. `rockA` and `rockB` are two cells wide and give 65; they may not fit inside the stake and are never picked by `random`. Anything else is treated as `rockC`, with a line in the log.
- `WaitTicks` (default `21000`): how long an empty mine waits for its next rock, in game ticks. The game counts 100 ticks a second while it is not paused. The default is three and a half minutes of play, a little under three and a half hours on the game's clock at its usual day length. Values below 100 are treated as 100.

## What is tested and what is not

Tested here means: seen working in the game by the author, on Alpha 6.0.5. The current versions were tidied up after much of that testing, without meaning to change what they do, so the list says which version each thing was seen with. Three of the mods then got fixes from a code review: Geoland.Planting 0.8.2, Geoland.Harvest 0.9.1 and Geoland.PlotLevels 0.5.2. The author has played those three versions briefly. The list says what was seen in them; the rest was seen in the versions before, and the fixes not yet seen are in the last column.

The first four mods, current versions, load cleanly on Alpha 6.0.5. Geoland.Mines 0.1.0 loaded cleanly beside them. Geoland.Mines 0.1.2 has not yet been loaded in the game.

| Mod | Seen working | Not yet seen |
|---|---|---|
| **Geoland.Planting** 0.8.2 | Planting a berry bush, a fiber bush and wild cotton, each starting empty, growing its first crop and then harvested (0.8.2). A wild cotton farm set up from planted cotton and worked by villagers (0.8.1). The planting key ignored while a menu is open (0.8.1) | The key switched off with `None`. The planting key while typing in a text field |
| **Geoland.Harvest** 0.9.1 | Villagers harvesting through a day of play (0.9.1). All of this in 0.9.0. All six work orders: berries, honey, wax, fiber, bush wood and wild cotton. Each order taking only its own item and all of it: wax taken and the honey left for the next visit; both fibers from a bush. Picking a half-collected object by hand giving only what is left. Wild cotton still being picked in winter. With the one-visit setting on, a worker clearing a whole bush in one visit | Everything new in 0.9.1: an item staying on the object when the worker has no room for it. A worker who cannot unload moving on to another order |
| **Geoland.PlotLevels** 0.5.2 | A home above a shop, and a home above a tavern, built from nothing: picking the plot, painting the upstairs floor, repainting and removing paint, villagers assigned to work downstairs and to live upstairs (0.5.1). Tiles showing while the painter is out (0.5.1). Painting and erasing upstairs, and a stroke staying on the kind of floor it began on (0.5.2). A plot erased down to nothing on the ground losing its upstairs floor with it, and a bed on that floor counting for the home again as soon as the floor was repainted (0.5.2). Levels loaded from a save (0.5.2). A building with a different plot on each of three floors, painted from nothing (0.5.2) | No levels made on the ground floor of a slope; the tiles redrawn after an erase that changes no owner; the key switched off with `None`. Erasing on a third floor |
| **Geoland.RecruitWishes** 0.2.1 | Travelers recruited through varied wishes; wishes staying the same across saving and loading, as far as the author has tried (current version) | |
| **Geoland.Mines** 0.1.2 | All of this in 0.1.0, with the rocks broken by the player by hand: the mod loading cleanly beside the other four. A Tree Stake placed on a Miner Zone. The rock appearing after the wait. A new rock appearing after the last was broken, six times in a row at one stake | A villager mining a rock in a stake. This is what the mod is for; from the game's code it should work. The medium rock and the `random` setting (new in 0.1.1 and 0.1.2, not yet run). The rock still in its stake after saving and loading. A larger rock inside a stake |

Also not yet seen: the mods' names appearing in the game's error report.

Nothing here has been tested in multiplayer. Geoland.PlotLevels is not tested in multiplayer and not expected to work there: the levels are not sent to other players. If you try it anyway, do it on a copy of a world you do not mind losing.

## Uninstall

**Back up your saves first** (see "Before you install"), and read what removing a mod means for a world that used it:

- **Geoland.Harvest:** the farm's work orders are reset to the game's defaults. This comes from reading the game's code; it has not been tried.
- **Geoland.PlotLevels:** the game ignores the levels. This too comes from reading the game's code and has not been tried. Upstairs objects such as beds count for the plot on the ground again. The first save made without the mod no longer contains the levels, so they are gone for good.
- **Geoland.Planting:** plants you planted are ordinary objects of the game and are expected to stay. This has not been tried.
- **Geoland.RecruitWishes:** travelers go back to asking for the game's last joining price, including any traveler who was waiting with a wish. This is expected from how the mod works; it has not been tried.
- **Geoland.Mines:** nothing of the mod's is in the save. A rock already standing in a stake is an ordinary rock and is expected to stay; no new ones appear. The Tree Stakes stay where they are. This comes from reading the game's code; it has not been tried.

To remove the mods:

1. Close the game.
2. Delete the five `Geoland.*.dll` files from the game's `BepInEx\plugins` folder, or run `install.cmd -Uninstall`.

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
