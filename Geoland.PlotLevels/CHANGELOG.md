# Geoland.PlotLevels: changelog

> **About this file:** what changed in each version of Geoland.PlotLevels, newest first. Part of the Geoland library, unofficial mods for HavenCraft. Written with Claude, Anthropic's AI assistant, as the rest of the library was.

## 0.5.2

- A painter stroke stays on the kind of floor it began on. A stroke begun upstairs does nothing while the brush is off the upstairs floors, on a wall or past the floor's edge. A stroke begun on the ground does nothing on an upstairs floor. Before, a stroke begun upstairs and dragged onto a wall was handed back to the game's ground painter and could paint or erase ground plots. To continue on the other kind of floor, release the button and press again.
- When a plot is deleted or erased down to nothing, its upper levels are removed first, so beds and other objects upstairs are filed under the plot that is there now. Before, they stayed listed under the deleted plot until the save was loaded again.
- On a slope, a brush no longer makes levels in cells where its height is that cell's ground floor.
- The plugin can no longer stop the game from saving if a game update changes what it reaches for.
- Erasing a level that did not change who owns the floor now redraws the tiles.
- Setting `PickPlot` to `None` in the settings file now switches the key off.

## 0.5.1

- The keys J, L and H are gone. Levels are painted and erased with the plot painter's own click and erase button, which already did the same. K, to pick a plot, stays, and now only acts while the plot painter is selected.
- Levels on the ground floor, which versions before 0.3.1 could make by mistake, are removed when a world loads. The ground floor follows the game's normal plots again. The plugin says so once when it removes any. Raising the `MinHeightAboveGround` setting does not remove levels painted under a lower value.
- The source was reorganised: one class per file, each opening with what it is for and how it relates to the game.
- The plugin now checks at start-up that the parts of the game it reaches into are still there, and the log says what is missing. If the plot painter has changed, levels already in a save are still honoured but cannot be painted. If the save system has changed, the plugin switches itself off.
- The painter's values are no longer looked up by name every frame, and the brush preview is rebuilt only when the brush moves.
- The log notes the game version the plugin was built for, and warns when the running game is a different one.
- The in-game notice shows the full version number.
- The plugin adds its name and version to the game's own list of mods, so the game's error reports and save descriptions say which mods were in use, not only that unofficial mods were detected.

## 0.5.0

- Renamed from HavenPlotLevels to Geoland.PlotLevels, as part of the Geoland library. The settings file is now `geoland.havencraft.plotlevels.cfg`, and the plugin's file inside a save is `geolandPlotLevels.json`. The earlier file name is still read.
- No change in behaviour.

## 0.4.1

- A stroke that starts on a section already painted on that floor extends that section, as the game's painter does on the ground.
- Erasing a floor hands that floor back to the ground plot. It no longer removes the level of the floor below.

## 0.4.0

- The plot painter aims at the floor being looked at, at that floor's height, and its click and erase button paint and erase upper levels there. Before this the painter's brush landed on the ground underneath the building.

## 0.3.1

- Levels can only be made above the ground floor.
- Tiles take the colour the game gives the plot they belong to, with an outline.

## 0.3.0

- The brush is the plot painter's own, with its aim and its size. The overlay shows only while the painter is selected.

## 0.2.0

- An overlay shows the upper levels and a preview of the brush.

## 0.1.0

- First version: an upstairs floor can be given to a different plot than the ground below it, with keys acting on a square under the character. Levels are stored inside the save file.
