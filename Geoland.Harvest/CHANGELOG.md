# Geoland.Harvest: changelog

> **About this file:** what changed in each version of Geoland.Harvest, newest first. Part of the Geoland library, unofficial mods for HavenCraft. Written with Claude, Anthropic's AI assistant, as the rest of the library was.

## 0.9.0

- **One visit takes everything that fits.** A villager who collects a work order's item from an object now also takes the object's other items in the same visit, if they have room to carry them: fiber and the wood from a bush, honey and the wax from a hive. What does not fit stays on the object for a later visit. Each work order is still judged by its own item, so a full store of one item does not stop the other being collected. The extra items do not count toward any target while they are being carried in this way, so a store can go over its "keep" amount. Setting: `TakeEverythingThatFits` (on by default).
- **Picking by hand gives only what is left.** If a villager has already taken the fiber from a bush, harvesting it yourself gives the wood and not the fiber again. Before, a half-collected object gave everything a second time.

## 0.8.1

- Wild bee nests and the giant berry bush are no longer harvested by villagers. They are found in the wild and cannot be made, so they are left to the player. Berry bushes, beehives, the plain bushes and wild cotton are harvested as before, through the same six work orders.
- The work orders have new internal names. A save made with an earlier version still loads with its farm work orders and their settings: the old names are translated as the save is read, and the next save holds the new ones.
- The source was reorganised: one class per file, each opening with what it is for and how it relates to the game.
- The plugin now checks at start-up that the parts of the game it reaches into are still there. If a game update has changed one, the log says what is missing and the plugin adds no work orders, so the game behaves as it does without the plugin.
- The log notes the game version the plugin was built for, and warns when the running game is a different one.
- The in-game notice shows the full version number.
- The plugin adds its name and version to the game's own list of mods, so the game's error reports and save descriptions say which mods were in use, not only that unofficial mods were detected.

## 0.8.0

- Renamed from HavenHarvest to Geoland.Harvest, as part of the Geoland library. The settings file is now `geoland.havencraft.harvest.cfg`.
- No change in behaviour.

## 0.7.1

- One work order per item instead of one per kind of object: "Collect Honey" and "Collect Wax", "Gather Fiber" and "Gather Bush Wood". Each item is collected on its own and grows back on its own.
- Fixes villagers not harvesting hives once honey had reached its target, which also stopped the wax. The game judges a work order by its first output only.

## 0.7.0

- Split out of the earlier combined plugin, HavenHelper, as a plugin of its own.
- Made general: every kind of object the game marks as harvestable gets work orders, found by reading the game's own object definitions. Each kind can be switched off in the settings file.

## Before the split (HavenHelper)

- 0.6.0: "Gather Berries" and "Collect Honey" work orders on the Farm plot.
