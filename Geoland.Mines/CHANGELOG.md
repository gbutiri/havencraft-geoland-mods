# Geoland.Mines: changelog

> **About this file:** what changed in each version of Geoland.Mines, newest first. Part of the Geoland library, unofficial mods for HavenCraft. Written with Claude, Anthropic's AI assistant, as the rest of the library was.

## 0.1.2

- The default rock is the medium one (`rockC`, 45 stone). `random` stays as an option of the `Rock` setting. In 0.1.1 random was the default. Neither 0.1.0 nor 0.1.1 was released; 0.1.2 is the first version published.
- A `Rock` setting the plugin does not know is replaced by the medium rock.

## 0.1.1

- The rocks are random by default: each new rock in a mine is a medium rock (45 stone) or a small one (21 stone), with an even chance. Before, every rock was the smallest.
- The `Rock` setting takes `random` or one rock's name. A settings file written by 0.1.0 still says `rockE`; change it to `random` to get the new default.
- 0.1.0 was seen working in the game by the author: a Tree Stake on a Miner Zone got its rock after the wait, and the next one after that was mined out. Villagers mining the rock has not yet been seen.
- One mine that cannot be kept no longer stops the others.

## 0.1.0

- First version. It builds, but it has not yet been run in the game: everything below is what the code is written to do, from reading the game's code.
- A Tree Stake placed on a Miner Zone is a mine. One of the game's own rocks appears in it, and the villagers of the Miner Zone mine it with the game's "Mine Rocks" order. Some time after the rock is mined out, a new one appears.
- A Tree Stake on any other plot is left alone, so woodcutters' stakes work as they always did. A mine that holds a sapling or a tree, sown by hand, is left alone too until it is empty again.
- Nothing is stored in the save. The rocks are ordinary rocks, and the game itself remembers which stake holds which. A save made with the plugin loads without it. The wait is not saved: after loading, an empty mine waits the whole time again.
- Which rock appears, how long an empty mine waits, and an on/off switch are in the settings file, `geoland.havencraft.mines.cfg`. The defaults are the small rock `rockE` (21 stone) and 21000 ticks, the time a berry bush takes to refill.
- The log has one line for each rock placed and one for each rock found gone, with the stake's position and the plot's number.
