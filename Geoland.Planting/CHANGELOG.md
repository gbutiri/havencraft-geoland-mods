# Geoland.Planting: changelog

> **About this file:** what changed in each version of Geoland.Planting, newest first. Part of the Geoland library, unofficial mods for HavenCraft. Written with Claude, Anthropic's AI assistant, as the rest of the library was.

## 0.8.1

- No change in what the plugin does for the player.
- The source was reorganised: one class per file, each opening with what it is for and how it relates to the game.
- The plugin now checks at start-up that the parts of the game it reaches into are still there. If a game update has changed one, the log says what is missing and planting stays switched off, so the game behaves as it does without the plugin.
- The log notes the game version the plugin was built for, and warns when the running game is a different one.
- The in-game notice shows the full version number.
- The plugin adds its name and version to the game's own list of mods, so the game's error reports and save descriptions say which mods were in use, not only that unofficial mods were detected.

## 0.8.0

- Renamed from HavenBerryPlanting to Geoland.Planting, as part of the Geoland library. The settings file is now `geoland.havencraft.planting.cfg`.
- No change in behaviour.

## 0.7.2

- Fiber as the selected item plants a fiber bush (the small plain bush) for one fiber.

## 0.7.1

- Cotton as the selected item plants wild cotton for one cotton. Each kind of plant has its own ghost.

## 0.7.0

- Split out of the earlier combined plugin, HavenHelper, as a plugin of its own.
- The planting key moved from F8 to G, because the game uses F8 itself. The key can be changed in the settings file.
- The key is ignored while typing in a text field and while a menu is open.

## Before the split (HavenHelper)

- 0.5.1: a ghost of the bush previews the spot, outlined as good or refused.
- 0.5.0: a key toggles planting mode; with berries selected, a click plants a berry bush where you aim and uses one berry.
- 0.2.0: a key plants a berry bush in front of the character.
