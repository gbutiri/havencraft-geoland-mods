# Geoland.RecruitWishes: changelog

> **About this file:** what changed in each version of Geoland.RecruitWishes, newest first. Part of the Geoland library, unofficial mods for HavenCraft. Written with Claude, Anthropic's AI assistant, as the rest of the library was.

## 0.2.1

- No change in what the plugin does for the player.
- The source was reorganised: one class per file, each opening with what it is for and how it relates to the game.
- The plugin now checks at start-up that the part of the game it reads is still there. If a game update has changed it, the log says what is missing and the plugin makes no wishes, so the game behaves as it does without the plugin.
- The log notes the game version the plugin was built for, and warns when the running game is a different one.
- The in-game notice shows the full version number.
- The plugin adds its name and version to the game's own list of mods, so the game's error reports and save descriptions say which mods were in use, not only that unofficial mods were detected.

## 0.2.0

- Renamed from HavenRecruitWishes to Geoland.RecruitWishes, as part of the Geoland library. The settings file is now `geoland.havencraft.recruitwishes.cfg`.
- No change in behaviour.

## 0.1.0

- First version. Once the game's fixed list of joining prices is used up, each traveler has a wish of their own: one good from a pool, in an amount drawn from a range for that good.
- A traveler's wish is worked out from the traveler's ID number, so it stays the same across saving and loading and nothing is stored in the save.
- The pool, the amounts, and an on/off switch are in the settings file.
