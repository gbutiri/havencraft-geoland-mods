# How it works: HavenCraft from a modder's side, and how the Geoland mods hook in

> **About this document:** unofficial notes on how HavenCraft is put together as far as a modder needs to know, and on how the four Geoland mods hook into it. HavenCraft is the work of Dionic Software; nothing of the game (no code, no data, no assets) is included here, only descriptions in our own words and the names of things. It describes HavenCraft Alpha 6.0.5 and may be out of date after any game update. Published with the developer's permission. Not made, endorsed or supported by Dionic Software. It was written with Claude, Anthropic's AI assistant.

**How to read the certainty marks.** "Seen working" means the author saw it behave that way in the running game. "From the code" means it was only read in the game's code or data and has not been tested. Anything without a mark is from the code.

## Contents

1. The game build and the mod loader
2. Mod detection
3. The official mod interface, and how a plugin registers with it
4. Reaching the game's modules and private members
5. Data files: object definitions and recipes
6. World objects: spawning, heights, ghosts
7. Player: inventory, keys, clicks, notifications
8. Work orders, villager tasks and the planner's rules
9. Harvestable objects
10. Plots and the one lookup function
11. The plot painter
12. Travelers and joining prices
13. Saving and save parts
14. Aiming and layers
15. Seasons
16. How each mod hooks in
17. Pitfalls
18. Checklist for a new plugin
19. Assemblies worth reading

---

## 1. The game build and the mod loader

- HavenCraft Alpha 6.0.5 is a Unity 6 game running on Mono. Its managed assemblies are ordinary .NET files in the game's `Managed` folder. The game's own are named `Haven.*`, `Dionic.*` and `Assembly-CSharp`. Almost all game types are in the global namespace.
- The Geoland mods are BepInEx 5 plugins (Windows x64). The mods write to `BepInEx\LogOutput.log` in the game folder; the same lines also appear in the game's own `Player.log`.
- The plugins target `netstandard2.1` and reference the game's assemblies from a local install. Those assemblies are only referenced, never copied into the build output or the repository.
- The source in `Shared/` is compiled into every plugin, so each plugin is a single file that works on its own. There is no separate framework plugin.
- The game's version, as it writes it into saves and its log, comes from the public `Versions.GetVersion()`. The plugins log it at start-up and use it for their version check.

## 2. Mod detection

- The game detects third-party mod loaders. When it does, it marks every save written afterwards as modded, permanently, and says so in its error reports. The game is not blocked.
- Seen working: saves written by a game with BepInEx installed carry the mark; saves written by an unmodded install do not.
- The game also has a list of the mods in use (`ModModule.GetMods()`), which goes into error reports and into each save's description.
- The Geoland plugins add their own name and version to that list with a patch that runs after `GetMods` (`Shared/AnnounceModPatch.cs`), so that it is visible what was installed. This is the author's own choice, made so that anyone reading an error report or a save's description can see which mods were in use. Seen working for the save's description: a save written with the four plugins loaded lists each by name and version. The error report itself has not been looked at.
- These plugins do nothing to prevent, hide or undo the detection or the mark, and must not. Please keep it that way in anything built on this.

## 3. The official mod interface, and how a plugin registers with it

- The game has an official mod interface that is planned for later and does not yet cover what these mods need. Its `IMod` has one call when a world has loaded (`OnGameLoaded`) and one every frame (`UpdateFrame`). Both receive a `Game` object.
- Of that `Game` object, these mods use the call that shows a notification.
- The game creates a mod loader module (`DionicModLoaderModule`) with every world. It calls each mod in its list when the world has loaded and every frame.
- **How we register** (`Shared/RegisterModPatch.cs`): the plugins add themselves to that list so that they run on the game's own schedule. A patch that runs just before the module's "world loaded" call (`WorldInitAfterSaveLoad`) adds the plugin's `IMod`. The plugin is then told about every world, including a reload. Seen working. Several plugins can each add their own.
- The main menu also loads a world, with no player in it. Check that there is a main player before showing notifications.

## 4. Reaching the game's modules and private members

- A world is a `WorldOrchestrator` holding modules that implement `IWorldModule`. Its `GetModule<T>()` is public.
- The official mod interface does not yet hand the orchestrator out, so `Shared/GameHook.cs` finds it through the `GameOrchestrator` (two private members, read by name) and wraps the lookup as `GameHook.Module<T>()`.
- Modules are constructed in a fixed order. Items and recipes come early, the world object module after them, the villager work and harvest modules later, and the mod loader module last. This matters when you patch constructors or data loading.
- `InputModule` is not a world module. The plugins take it from a module that holds it privately (`PlayerActionModule`).
- **Private members are looked up once, at start-up.** Each plugin has a `GameAccess.cs` that finds every private field, method, constructor and non-public class it needs through `Shared/GameMembers.cs`. When one is missing, the log says which, and the feature that needed it stays off instead of failing in the middle of play.
- **Patches are applied one class at a time**, each inside its own error handling (`GameHook.ApplyPatches`), so one patch that no longer fits the game cannot take the others down. A patch whose target is missing skips itself.
- Several game classes we patch are not public. Such a patch names its target at run time and receives the instance as a plain `object`.

## 5. Data files: object definitions and recipes

- **Object definitions** are several JSON text files loaded by `WorldObjectModule` from the game's resources. Each top-level key is an object's internal name. A definition gives the object's size, colliders, the named spots where a villager stands to work on it, a construction mode (natural objects have their own), the keys under which plots keep track of it, and a set of components such as `resourceHarvest`, `resourceNode`, `farmPlant` or `workplace`.
- **Recipes** are one JSON text file loaded by `RecipeModule`. A recipe has input and output items, a work duration, how it is unlocked, and optionally a villager task: a tracking key naming the kind of object to go to, and the name of the standing spot to use. An output amount can be left open to mean "whatever the object yields".
- **Both are parsed with Json.NET's `JObject.Parse`.** A patch that runs after the two-argument overload sees every parsed file and can add to it before the game turns it into its own objects. Recognise the recipes file by the presence of a couple of recipes you know; recognise a definitions file by the object name you are looking for. Guard against re-entry when you parse JSON of your own. Seen working. Nothing is changed on disk.
- **Order:** recipes are parsed before object definitions, because the recipe module is built first. A plugin that needs to know the object types when it adds recipes must read the definition files itself at that earlier moment.
- **Recipe numbering:** recipes get a number in alphabetical order of their internal names. Names that sort last leave every existing recipe with the number it has without the plugin; ours begin with `zz` for that reason.
- **Titles:** a work order is shown under the text the language table (`Loca`) has for `recipe_` plus the recipe's internal name, falling back to the name of the first output item. The table is read through four functions, two that ask whether a text exists and two that fetch it; patches that run before them can supply titles. The icon is the first output item's.
- **Saves refer to recipes by name** (from the code, and from looking inside a save):
  - Each plot's work orders are saved as recipe name, amount and mode. On load, an order whose recipe is unknown is skipped. If the remaining orders no longer match the plot type's list, all of that plot's work orders are rebuilt with defaults. So a save loads without the plugin, but a farm that used its work orders loses its settings.
  - Each villager's progress on an order is saved by recipe name too. An unknown name is skipped; only a little progress is lost.
  - Task names and tracking keys are not saved. They exist only in what is built at start-up.

## 6. World objects: spawning, heights, ghosts

- `WorldObjectModule` has public calls to spawn and destroy an object, look up a definition by name, list all objects, and create and destroy a ghost.
- A `WorldObject` is a small handle: its definition, its ID, its `GridTransform`, and access to its components.
- A `GridTransform` holds a grid cell, one of four rotations, and a packed sub-cell offset and scale.
- **Heights.** Terrain is regenerated from the world's seed, not stored. The game does not snap objects to the ground on load; the stored height is used as it is (seen in the game). To place something on the ground, ask the game: `HeightAndBiomeModule.SampleHeight` gives the terrain height, and `WorldObjectUtil` has a function that turns a world position into the right `GridTransform` for a given object. This is what the game's own wild-plant spawner does. Seen working.
- **Ghosts.** A ghost from `SpawnWorldObjectGhost` can be moved, shown, hidden and given the game's building or demolishing outline. The game puts ghosts on their own Unity layer. Seen working.
- Spawned natural objects start full and get their components (harvest, resource node) automatically.

## 7. Player: inventory, keys, clicks, notifications

- `PlayerCharacterModule` gives the main player's position and rotation. The height is the surface stood on, a few hundredths above the grid height.
- `PlayerInventoryModule` gives the main player's inventory, and from it the selected hotbar item with its internal item name. Items can be counted in and removed from the inventory's container. Seen working.
- **Keys.** The Geoland mods use two keys by default: G (planting mode) and K (pick a plot, in plot levels). Both were checked against the game's own bindings and are unbound by default in Alpha 6.0.5. Both can be changed in each plugin's settings. Check before you choose a key: the game binds many letters and most function keys.
- **Reading a key** directly through Unity's input system works, but the game does not block it while the player types. Ask the input module first whether a text field is active or a menu is open.
- **Clicks.** `PlayerActionModule` funnels "use item" and "interact" through three private handlers: the click itself, the button going down, and the button coming up. A patch that runs before them and tells the game the click was dealt with takes it away from the selected item. Leave clicks on the interface alone; the input module can say whether the interface was used this frame.
- **Tools** are player actions registered with `PlayerActionModule` and active for the items they claim. The plot painter is one.
- **Notifications:** the `Game` object from the mod interface shows a line of text.

## 8. Work orders, villager tasks and the planner's rules

- **A work order is a recipe attached to a kind of plot.** Which recipes each kind of plot offers is a fixed list in the game's code (`WorkPlotUtil`). A patch that runs after the function that fills the list can append to it.
- **Villager-task recipes** send a worker to a world object inside the plot. The plot's tracker supplies objects whose definition lists the recipe's tracking key; the worker stands at one of the object's spots with the recipe's interaction name. All recipes that share a tracking key must use the same interaction name.
- **Each workable object needs an adapter** implementing the game's `IVillagerTaskWO`, registered with the villager work module's task object manager. The planner knows nothing about particular objects. It asks the adapter whether a named task can be done, what the work yields, how it is going, and what to do when it is finished.
- **Only one adapter per object.** Trees, rocks, bushes, berry bushes and wild cotton all carry a resource node, and every such object gets the game's adapter for choppable objects. Fields and crops have their own. Beehives have none, which is why no villager ever worked one.
- **The yield question carries no task name.** The planner asks "can you do this task?" and then "what does it yield?" without repeating the name. When the answer depends on the task, remember the last name asked.
- **Tell the planner when an object's state changes** (the task object manager has a call for it), but only after the world has finished loading.
- **Rules of the planner** (found by running into them):
  - A work order's production mode is judged on its **first output item only**. "Keep N in storage" counts that item in the worker's inventory and the plot's chests. Other outputs do not count.
  - A worker must have **carrying room for every item** the job yields, and the recipe must list every yielded item, or the job is not planned.
  - A worker holding a **full stack of any output item** goes to clear inventory first; with no chest space she waits and retries.
  - So: one recipe per item.
- A work order has a mode, such as keep an amount in storage.

## 9. Harvestable objects

- An object is harvestable when its definition has a `resourceHarvest` component, which lists the items it yields and how long it takes to refill.
- Several kinds of object are harvestable. Each has its own yield and refill time; a beehive, for example, yields more than one item.
- To the game a harvestable object is either full or empty. Its component (`ResourceHarvestWOComponent`) can say whether it is full, and its harvest call takes everything and drops the items on the ground. What it yields, its refill time and the call that empties it without dropping anything are private.
- Every change between full and empty goes through one method of that component, which makes it a good place to notice both.
- From the code: refill does not depend on the season.

## 10. Plots and the one lookup function

- **Storage:** `PlotModule` keeps one flat grid with a plot number for every ground cell. Zero means wilderness. There is no height: a plot covers its cells from the ground through every floor above.
- **Lookup:** every question in play of the kind "which plot is this in?" ends in `PlotModule.GetPlot`, which takes a full world position, drops the height and reads the grid. "Which plot is this object in?" passes the object's centre. Only the plot module's own internals (saving, the overlay, borders, multiplayer chunks) and the painter read the grid directly. One patch that runs after `GetPlot` therefore changes the answer everywhere. Seen working.
- **Object tracking:** the plot module's tracker files objects that have tracking keys under their plot, on creation and at load. It re-files them when the module raises its "plot shapes changed" event. If you change which plot a place belongs to, raise that event for both the plot that lost space and the one that gained it. Plot signs, chests and the work planner listen to it too.
- A plot has an ID, a name, bounds and a configuration that says what kind it is (a home, a farm, a workshop and so on). The game derives a default colour from the plot's ID.
- **Homes:** a bed counts for a home when it is indoors and filed under a Home plot. Assigning a villager through the home's panel gives the first free bed on the plot, on any floor. Seen working with plot levels: beds on an upstairs floor given to a Home plot, above a shop, count for that home.
- Villagers' pathfinding does not look at plots (from the code).

## 11. The plot painter

- The painter is a player action in `Haven.Features.PlotPaint`. Its class is not public.
- It keeps a square brush of 1 to 5 cells a side, the point aimed at, and the cell at the brush's corner. Each frame it works out the brush position; on a click, or while a stroke is dragged, it applies the brush to the flat grid.
- From the code: a stroke chooses its plot at the press, as the plot under most of the brush. That plot is then extended; if the brush is over no plot, a new one is created. The brush is applied again only in frames where it has moved to a new cell. Erasing sets cells back to no plot.
- **The painter aims only at the terrain** (see section 14). Upstairs, its brush lands on the ground below the building.
- From the code: the brush marker's colour and the side panel's text are worked out from the flat grid, so upstairs they still describe the ground underneath. Geoland.PlotLevels does not change that feedback.

## 12. Travelers and joining prices

- `TravelerModule` holds a fixed list of joining prices, used in order, each once.
- `GetNPCsToConvince` is the one function the game asks for what each waiting traveler wants. It pairs travelers with the prices not yet used. When the list is used up, every traveler asks for the last one. Which prices are used is saved with the world.
- The join panel shows the price, checks the player has the goods, takes them, and calls `ConvinceNPC`, which makes the traveler a villager and records the price as used by finding the same list object in the module's list. From the code: the multiplayer path uses the same two functions.
- **Prices are compared by reference**, not by content. Always hand the panel the same list object for the same traveler.
- `ItemModule.TryGetItem` returns nothing for an unknown item name, which is how a plugin can check names from its settings.

## 13. Saving and save parts

- A save is a zip of named files, most of them JSON. Each registered save part (the game's `ISavable`) writes one file and is handed that file again on loading. Parts are added to and removed from `SaveModule`; remove yours before the world is destroyed, or the game logs an error.
- **Loading** walks the registered parts in the order they were added and looks each one's file up in the zip. A file that no part asks for is never opened, so a save holding a mod's file loads normally without the mod.
- A registered part whose file is missing logs an error. To stay quiet on saves made without your plugin, tell the game the part has no file to load and read the zip yourself when the load starts.
- **Order matters.** Add your part ahead of a part it must precede. Geoland.PlotLevels adds its part in a patch that runs before the `PlotModule` constructor, so levels are in force before the plots are loaded and objects are filed under them. Seen working: levels saved, reloaded, and in force at load.
- The zip's file name is the save's name and its folder is the world's name; nothing inside repeats either.

## 14. Aiming and layers

- The game uses separate Unity layers for the terrain, for click colliders (the boxes building pieces are clicked by), for move colliders, for ghosts, and for characters.
- `RaycastCollisionModule` has a screen-centre raycaster that returns the hits along the crosshair ray in order of distance. It sees the terrain, click colliders and character click colliders.
- The plot painter accepts only the first hit on the terrain or a move collider within a short range, which in practice is the terrain. Click colliders are passed over. To aim at a floor, take the first hit on the terrain or a click collider and require a surface that faces up. Seen working upstairs.
- A floor piece's click collider top sits slightly above its grid height; allow a small margin when turning a hit into a height level.
- The game has its own collision system for world objects besides Unity physics.
- **Drawing your own geometry:** a mesh handed to Unity to draw each frame, with a material on Unity's built-in shader `Hidden/Internal-Colored`, needs no objects in the world. Seen working.

## 15. Seasons

All from the code:

- `SeasonsModule` gives the current season (four of them).
- When winter starts, the crop module destroys every crop, and fields refuse sowing until winter ends.
- The wild plants the forage module spawns daily depend on season and biome, with none in winter.
- Harvest refill timers ignore the season.

## 16. How each mod hooks in

All four start the same way. BepInEx creates the plugin's `Plugin` class, which reads the settings, checks through `GameAccess` that the parts of the game it relies on are still there, and applies its patches. `RegisterModPatch` then puts the plugin's mod in the game's own list for every world.

### Geoland.Planting

- **What it does:** a key switches planting mode on. With a plantable item selected (berries, cotton or fiber), a ghost previews the spot aimed at and a click plants the matching wild plant there for one of the item.
- **Where it hooks in:** the three private click handlers of `PlayerActionModule`. `PlantClickPatch` takes the "use item" click while planting mode is on and a plantable item is selected, so the item is not also used the normal way. `SwallowClickPatch` skips the held and released halves of that click. Every other click is passed on untouched.
- **Why there:** it is the one place every world click goes through, before the selected item's action sees it.
- **Aiming:** the aim point is found by walking along the view ray and asking the height module for the terrain under each step. It is therefore always on the ground, never on a building in the line of sight.
- **Placing:** the plant is spawned through `WorldObjectModule` with the same two calls the game's wild-plant spawner uses, and paid for through the player's inventory. A planted object is an ordinary one: it can be harvested, grows back and is saved with the world.
- **Preview:** the game's own ghost, with the building outline where the plant may go and the demolishing outline where it may not.
- The table of which item plants what is in code on purpose: each pair was checked against the game's object definitions.

### Geoland.Harvest

- **What it does:** adds work orders to the Farm plot, one per item (Gather Berries, Collect Honey, Collect Wax, Gather Fiber, Gather Bush Wood, Gather Wild Cotton). A farm worker harvests the objects that stand inside the farm.
- **Data** (`GameDataPatch`, `HarvestCatalog`): as the game parses its data, the plugin adds one villager-task recipe per item, and gives each harvestable definition a tracking key and standing spots for each item it yields. Trees and crops have both in the game's data; bushes and hives have neither. The catalog reads the game's own definition files, so nothing about particular bushes or hives is hard-coded, and it reads them early because recipes are parsed first. The wild bee nest and the giant berry bush are left to the player.
- **The Farm's list** (`FarmWorkOrdersPatch`): the orders are appended to the Farm's fixed list. The Farm was chosen because harvesting plants is what its workers already do.
- **Titles** (`WorkOrderTitlePatches`): patches in front of the language table's four functions answer for the plugin's own work orders only.
- **Adapters:** an object with no adapter, such as a beehive, gets the plugin's own (`HarvestTaskAdapter`) when it is created and loses it when it is destroyed (`HarvestableLifecyclePatches`). Bushes and wild cotton already carry the game's adapter for choppable objects, and only one is allowed, so that adapter's five methods are patched to answer for harvest tasks too (`ChoppableAdapterPatches`). For anything else, every tree and rock included, the game's own answer stands.
- **Timing** (`WorldStartedPatch`): adapters must be registered as objects are created, which is before the "world loaded" call. The plugin therefore also listens for the villager work module being built.
- **One order per item** (`HarvestWork`): because of the planner's rules in section 8, each order's recipe lists one item, and the order is judged by that item alone. The plugin keeps its own note of which items were taken from an object the game still calls full. When the last item is taken, the object is emptied and refills the game's own way. Whenever the game itself empties or refills an object, the note is dropped. The notes are in memory only; after loading, a full object offers all its items again.
- **One visit takes everything that fits** (`HarvestWork`, since 0.9.0; a setting, off by default): when the order's own item has been handed to the villager the game's way, the object's other items are put straight into the villager's inventory, each only if all of it fits. What does not fit stays on the object for a later visit. Why straight into the inventory: the game's hand-over of work output accepts only the items the work order's recipe lists and discards anything else. And listing them in the recipe would make the planner demand carrying room for all of them before sending anyone, which is the rule that led to one recipe per item. Items taken along do not count against their own order's target, so a store can go over it. The setting `TakeEverythingThatFits` switches this off. From the code; not yet seen working.
- **Picking by hand** (`HandHarvestPatch`, since 0.9.0): the game's harvest call gives everything the kind of object yields, so an object a villager had partly collected would give the taken items a second time. The list of what is yielded belongs to the kind of object, not to the one object. A patch around the harvest call swaps in a list of only what is left for that one harvest and puts the full list back afterwards, whether the harvest succeeded or not. From the code; not yet seen working.
- **Old saves** (`SavedNamesPatch`): the plugin's recipes had other internal names in earlier versions. A patch in front of the order book module's load translates them so a farm keeps its settings. Seen working: a save with thirty work orders under the old names loaded with every order's mode and amount unchanged, and the next save held the new names.

### Geoland.PlotLevels

- **What it does:** lets an upstairs floor belong to a different plot than the ground below, so the bedrooms of a Home can sit above a shop. With the plot painter out: K picks the plot aimed at, a click on an upstairs floor gives it to that plot from that height up, and the erase button hands the floor back to the ground plot.
- **Data** (`LevelMap`, `PlotLevel`): the game's flat grid is left exactly as it is and means "ground level". The plugin adds, per cell, a short list of upper levels. A position belongs to the highest level at or below it, and to the ground plot when there is none. Whenever a level changes, the plugin raises the game's "plot shapes changed" event for the plots that gained or lost space.
- **The one lookup hook** (`PlotLookupPatch`): a patch after `PlotModule.GetPlot` answers with the level's plot when a level covers the position. Nothing else in the game is told about levels.
- **Saving** (`LevelSavePart`, `SaveRegistrationPatches`): the levels are one more save part, added just before the plot module adds its own, and read from the zip by the plugin itself so saves without the file load quietly.
- **Painter** (`PainterAim`, `PainterBrush`, `PainterPatches`): after the painter has worked out its brush, the plugin looks along the same ray, accepts the top of a building piece, and puts the brush on that floor using the painter's own brush. The "apply the brush" step is taken over while the brush is upstairs. Aimed at the ground floor or the terrain, both steps run as the game made them.
- **Strokes** (`PlotLevelsMod`): a stroke that starts on a section already painted on that floor extends it. On bare floor the picked plot is used, because a new plot cannot be created upstairs: a plot needs ground cells to exist.
- **Overlay** (`LevelOverlay`): outlined tiles in the plot's colour on every cell with an upper level, and a paler preview of the brush. Plain geometry drawn each frame; nothing is added to the world.
- **Limits:** the plot must already exist on the ground. Single-player only. Nothing less than a set height above the terrain ever becomes an upper level. Loaded without the plugin, a save ignores the levels and upstairs objects fall back to the ground plot.
- If the painter's private parts change, levels already in a save are still honoured but cannot be edited. If the save hooks are missing, the plugin switches itself off completely: levels that cannot be saved must not be made.

### Geoland.RecruitWishes

- **What it does:** once the game's fixed list of joining prices is used up, each traveler asks for a wish of their own instead of the repeated last price.
- **Where it hooks in:** `WishedPricePatch` runs after `GetNPCsToConvince` and swaps the last price for the traveler's wish, only where that last price has already been paid once. `WishPaidPatch` runs before `ConvinceNPC` and swaps a paid wish back for the game's last price, so the game's record of used prices stays exactly as it would be without the plugin.
- **Why there:** those two functions are the only way in and out of the join panel.
- A wish is worked out from the traveler's ID number and nothing else, so it should stay the same across saving and loading, which has not yet been checked in the game. Nothing is written to the save. Each traveler's wish is made once and the same list object handed out every time, because the game compares prices by reference.
- The pool of wishes is read from the settings when a world has loaded, since item names can only be checked against a loaded world.

## 17. Pitfalls

- **Assuming a key is free.** Check the game's bindings first.
- **Expecting a multi-output recipe to be worked for its second output.** The planner judges only the first.
- **Assuming the plot painter aims at what the crosshair is on.** It aims at the terrain.
- **Letting a tool act where the game already has an answer.** The ground floor belongs to the game's flat grid and its painter; plot levels never makes an upper level there.
- **"The level in force at this height" is not "the level that starts at this floor".** Erasing must act on the floor aimed at.
- **Raising game events or notifying the planner before the world has finished loading.**
- **Registering a second adapter on an object that already has one.**
- **Showing notifications from the main menu's world,** where there is no player.
- **Relying on a patch of a very small function.** A function of a few instructions may be inlined into its callers by the runtime, which bypasses the patch. The recipe lookup by name is that small; we patch an earlier, larger step instead.
- **A save part whose file is missing from older saves** logs an error on every load unless the part reads the zip itself.
- **Comparing or rebuilding joining prices by content.** The game finds them by reference.

## 18. Checklist for a new plugin

1. A new folder beside the others, with a project file that imports `Common.props` and so gets the shared source.
2. A `Plugin` class (a BepInEx `BaseUnityPlugin`) that sets up logging through `GameHook`, binds its settings, tells `GameHook` how to create its mod, and calls `GameHook.ApplyPatches`.
3. A `GameAccess` class that looks up every private member by name once, through `GameMembers`, and switches the feature off when one is missing.
4. An `IMod` that resolves the modules it needs when a world has loaded and does its per-frame work in the frame call, both inside error handling.
5. Open every source file with what it is for and how it relates to the game.
6. Log every player action with its position and its result. The log is how problems are diagnosed afterwards.
7. Include no code, data or assets from the game, and leave the game's mod detection alone.
8. Test in a separate copy of the game and a separate world. Every save written with a mod loader present is marked as modded for good.

## 19. Assemblies worth reading

| Assembly | What is in it |
|---|---|
| `Assembly-CSharp` | The game and world orchestrators, the module list |
| `Haven.ModAPI_1.0`, `Haven.Extensions.DionicModLoader`, `Dionic.Game.Mod` | The mod interface, the loader module, mod detection |
| `Dionic.Core` | Grid transforms, layers, utilities |
| `Dionic.Game.Save` | Save parts, the save module, loading, the save description |
| `Dionic.Game.Input`, `Dionic.Game.Keybindings` | Input actions and default bindings |
| `Dionic.Game.RaycastCollision` | The screen-centre raycaster, collision |
| `Dionic.Game.Navigation` | Pathfinding settings |
| `Dionic.Game.Localization` | The language table |
| `Haven.Core.WorldObject` | Objects, definitions, ghosts, placement utilities |
| `Haven.Core.WorldGen.HeightAndBiomes` | Terrain height |
| `Haven.Core.PlayerCharacter`, `Haven.Core.PlayerInventory`, `Haven.Core.PlayerAction` | Player position, inventory, clicks and tools |
| `Haven.Core.Item`, `Haven.Core.Recipe` | Items, containers, recipes |
| `Haven.Systems.VillagerWork`, `Haven.Systems.VillagerWorkBehaviourTree`, `Haven.Systems.OrderBook` | Work plots, adapters, the planner, work orders |
| `Haven.Systems.ResourceHarvest`, `Haven.Systems.ResourceNode`, `Haven.Features.FarmPlant`, `Haven.Features.Forage` | Harvest, chopping, crops, wild plants |
| `Haven.Systems.Plot`, `Haven.Features.PlotPaint`, `Haven.Features.PlotSignAndUI`, `Haven.Systems.PlotStats` | Plots, the painter, signs, stats |
| `Haven.Features.VillagerHome`, `Haven.Features.Sleep` | Homes and beds |
| `Haven.Features.Traveler` | Travelers and joining prices |
