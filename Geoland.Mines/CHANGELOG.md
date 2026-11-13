# Geoland.Mines: changelog

> **About this file:** what changed in each version of Geoland.Mines, newest first. Part of the Geoland library, unofficial mods for HavenCraft. Written with Claude, Anthropic's AI assistant, as the rest of the library was.

## 0.1.0

- First version. It builds, but it has not yet been run in the game: everything below is what the code is written to do, from reading the game's code.
- A Tree Stake placed on a Miner Zone is a mine. One of the game's own rocks appears in it, and the villagers of the Miner Zone mine it with the game's "Mine Rocks" order. Some time after the rock is mined out, a new one appears.
- A Tree Stake on any other plot is left alone, so woodcutters' stakes work as they always did. A mine that holds a sapling or a tree, sown by hand, is left alone too until it is empty again.
- Nothing is stored in the save. The rocks are ordinary rocks, and the game itself remembers which stake holds which. A save made with the plugin loads without it. The wait is not saved: after loading, an empty mine waits the whole time again.
- Which rock appears, how long an empty mine waits, and an on/off switch are in the settings file, `geoland.havencraft.mines.cfg`. The defaults are the small rock `rockE` (21 stone) and 21000 ticks, the time a berry bush takes to refill.
- The log has one line for each rock placed and one for each rock found gone, with the stake's position and the plot's number.
