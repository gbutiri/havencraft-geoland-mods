# The Geoland library: fan-made mods for HavenCraft

Four small mods for HavenCraft, the village-building game by Dionic Software. They are unofficial and made by a fan. HavenCraft is the work of Dionic Software, and this project is not affiliated with them. The developer has kindly said these may be published.

**Status: not released yet.** The source is here while the mods are being tested. Install instructions and ready-to-use files will follow with the first release.

| Mod | What it does |
|---|---|
| **Geoland.Planting** | Plant berry bushes, wild cotton and fiber bushes yourself. A key switches planting mode on; a click plants where you aim, for one of the item. |
| **Geoland.Harvest** | Villagers harvest. Farm plots get work orders for berries, honey, wax, fiber, bush wood and wild cotton. |
| **Geoland.PlotLevels** | An upstairs floor can belong to a different plot than the ground below it, so bedrooms can sit above a shop. |
| **Geoland.RecruitWishes** | Once the game's fixed list of joining prices is used up, each traveler asks for something of their own instead of the same thing every time. |

Each mod is a single file and works on its own.

## Before you use them

- **They are unofficial.** They run on [BepInEx](https://github.com/BepInEx/BepInEx), a third-party mod loader. The game notices this: it marks every save written afterwards as modded, permanently, and says so in its error reports.
- **Please do not send the developer bug reports from a modded game.** If something goes wrong with a mod installed, report it here.
- **The game is in alpha.** A game update can break a mod. When that happens the mod says so in the BepInEx log and switches the affected part off.
- Written for HavenCraft Alpha 6.0.5.

## For readers of the code

Every source file opens with what it is for and how it relates to the game. In each mod's folder, start with `Plugin.cs`. `GameAccess.cs` holds every place where the mod reaches into the game by name, and `CHANGELOG.md` has its history. `Shared/` is compiled into each mod.

Nothing from the game is included in this repository: no code, no data, no assets.

## How these were made

The ideas, the design decisions and all the testing are the author's, from playing the game. The code was written with Claude, Anthropic's AI assistant.

## Licence

MIT. See `LICENSE`.
