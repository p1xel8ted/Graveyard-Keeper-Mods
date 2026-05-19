# Graveyard Keeper Mods

A collection of **38 BepInEx mods** for [Graveyard Keeper](https://store.steampowered.com/app/599140/Graveyard_Keeper/).

## Install

### 1. Install BepInEx 5

Every mod here needs the [**Graveyard Keeper BepInEx 5 Pack**](https://www.nexusmods.com/graveyardkeeper/mods/79). Install that first - it sets up the modding framework.

### 2. Install the mods

**Vortex (recommended).** Every mod page has a "Mod Manager Download" button - click it and Vortex handles the rest.

**Manually.** Download the mod's ZIP from its Nexus page and extract it into:

```
...\steamapps\common\Graveyard Keeper\BepInEx\
```

That drops the mod's DLL into `BepInEx\plugins\<ModName>\` where the game will load it.

### 3. Configure (optional)

Launch the game, load a save, then press **F1** to open BepInEx Configuration Manager. Every mod's settings live there. You can also edit the TOML files in `BepInEx\config\` directly if you prefer.

## Updates

When you're on the main menu, an auto-updating notice on the side of the screen will flag any mod in this collection that has a newer version on Nexus. Click an entry to open its Nexus page. If you'd rather not see it, every mod has a **"Check for Updates"** toggle in its config - turn that off and the mod is silent.

The notice reads from a manifest file that refreshes every 12 hours, and the game caches results for 4 hours on disk, so newly-uploaded versions can take a few hours to appear.

## The mods

### Quality of life & UI

- **[Save Now](https://www.nexusmods.com/graveyardkeeper/mods/41)** - Save anytime with a keybind, auto-save on a timer, save on new day/on exit, restore your exact position on reload, tidier load-game list with pin-last-played.
- **[Max Buttons Redux](https://www.nexusmods.com/graveyardkeeper/mods/59)** - Min/Max buttons on the craft window (queues as many as you can afford in one click) + Max button on vendor slider. Controller triggers snap to min/max. Requires [Rest In Patches](https://www.nexusmods.com/graveyardkeeper/mods/125).
- **[Queue Everything](https://www.nexusmods.com/graveyardkeeper/mods/57)** - Turns the craft window into a proper queueing interface. Optional idle auto-crafts you can walk away from.
- **[Show Me Moar](https://www.nexusmods.com/graveyardkeeper/mods/81)** - Modern display support: native resolution, higher refresh rates, zoom, HUD scaling.
- **[Thoughtful Reminders](https://www.nexusmods.com/graveyardkeeper/mods/52)** - When the day flips over, the keeper thinks about what day of the week it is and what's happening, so you don't walk past merchant days or tavern nights.
- **[Rest In Patches](https://www.nexusmods.com/graveyardkeeper/mods/125)** - Collection of small vanilla bug fixes: restores missing craft-window arrow icons, smooths player movement, tunes footprints, keeps big stockpiles visible, swallows a few harmless vanilla exceptions. More added over time.
- **[Misc. Bits and Bobs](https://www.nexusmods.com/graveyardkeeper/mods/55)** - Grab-bag: movement-speed multiplier, Evict Church Visitors button, Skip Intro Video On New Game, Remove Cinematic Letterboxing, more. All toggleable.
- **[Fog Be Gone](https://www.nexusmods.com/graveyardkeeper/mods/46)** - Cleans up the weather overlay. Fog removed by default; wind and rain stay unless you turn them off too.
- **[No Intros](https://www.nexusmods.com/graveyardkeeper/mods/47)** - Skips the publisher and developer logos on launch.
- **[New Game At Bottom](https://www.nexusmods.com/graveyardkeeper/mods/43)** - Moves the "New Game" slot to the bottom of the save list so you don't fat-finger it.

### Travel & storage

- **[Beam Me Up Gerry](https://www.nexusmods.com/graveyardkeeper/mods/61)** - Teleport stones become a fast-travel system to every zone you've visited. Zero cooldown, optional small fee, save your own custom teleport points.
- **[Where's Ma' Storage](https://www.nexusmods.com/graveyardkeeper/mods/62)** - One shared inventory across every crafting station, bigger chests, bigger stacks. **Important:** don't mix with other inventory-modifying mods.
- **[Auto-Loot Heavies](https://www.nexusmods.com/graveyardkeeper/mods/51)** - Teleports heavy drops (timber, stone, marble, ore) straight to the nearest stockpile with space.
- **[Moar Pallets](https://www.nexusmods.com/graveyardkeeper/mods/157)** - Adds wooden pallets to the wooden and cellar build desks. Cellar pallets get filled by the elevator and emptied by the porter; barn pallets count toward merchant trading capacity. Loose crates auto-route to the nearest pallet with space.
- **[Gerry's Junk Trunk](https://www.nexusmods.com/graveyardkeeper/mods/64)** - A buildable trunk Gerry empties at midnight - drop items in, get coin back at dawn. Upgrade it through Woodworking → Engineer → Jeweler tech.
- **[Get Outta Ma Way](https://www.nexusmods.com/graveyardkeeper/mods/88)** - Walk through NPCs instead of pushing them around. No more body-blocks in doorways.

### Crafting, farming & alchemy

- **[FasterCraft Reloaded](https://www.nexusmods.com/graveyardkeeper/mods/65)** - Speed up every crafting-related activity. Each speedup is a separate toggle.
- **[Apple Trees Enhanced](https://www.nexusmods.com/graveyardkeeper/mods/54)** - Turns garden apple trees, berry bushes, and bee hives into passive producers that drop harvests for you to pick up. Optional realistic mode.
- **[Where's Ma' Veggies](https://www.nexusmods.com/graveyardkeeper/mods/76)** - Harvest every ready garden plot of the same crop at once instead of walking to each bed.
- **[The Seed Equalizer](https://www.nexusmods.com/graveyardkeeper/mods/58)** - Stops garden and vineyard seed counts from slowly bleeding dry - keeps at least a 1:1 ratio, usually net-positive.
- **[I Neeeed Sticks](https://www.nexusmods.com/graveyardkeeper/mods/56)** - Adds a "Wooden stick" craft to the circular saw, so you don't have to grind fallen branches.
- **[Moar Wells](https://www.nexusmods.com/graveyardkeeper/mods/158)** - Adds the basic stone well and the pump well to the wooden build desk. The pump well spawns on the nearest existing stone well and can be placed repeatedly; a config toggle removes its Engineer tech requirement.
- **[Keepers Candles](https://www.nexusmods.com/graveyardkeeper/mods/87)** - Candelabras and incense burners stay lit forever; removing a candle returns it to your inventory.
- **[Add Straight To Table](https://www.nexusmods.com/graveyardkeeper/mods/49)** - Skips the "Are you sure?" confirmation when you pick a body part to remove at the autopsy table.
- **[Alchemy Research Redux](https://www.nexusmods.com/graveyardkeeper/mods/90)** - Shows what an alchemy combination will produce before you craft it. Patches a gap where researched recipes weren't actually registered as known.
- **[Decomp Delight](https://www.nexusmods.com/graveyardkeeper/mods/86)** - Adds the decomposition element (Chaos/Life/Death/Body/Mind/Nature…) to every researched item's tooltip.

### Economy & progression

- **[Economy Reloaded](https://www.nexusmods.com/graveyardkeeper/mods/67)** - Disables vendor inflation and deflation so prices stay stable over a long playthrough.
- **[Give Me Moar](https://www.nexusmods.com/graveyardkeeper/mods/70)** - Multipliers for just about every drop, reward, and craft output in the game.
- **[Where's Ma' Points](https://www.nexusmods.com/graveyardkeeper/mods/71)** - Red / green / blue XP goes straight to your bar instead of spawning physical orbs - big performance win when you've been crafting a lot.
- **[Pray The Day Away](https://www.nexusmods.com/graveyardkeeper/mods/72)** - Lifts the vanilla "one sermon per week" rule and gives you fine-grained control over church running costs.
- **[Exhaust-less](https://www.nexusmods.com/graveyardkeeper/mods/42)** - QoL tweaks that reduce grind around energy, sanity, gratitude, tools, meditation, and sleep. All off by default or independently toggleable.

### Gameplay tweaks

- **[Longer Days](https://www.nexusmods.com/graveyardkeeper/mods/53)** - Stretches the in-game day. Vanilla is 7.5 real-time minutes per day - double or triple it without changing anything else.
- **[Bring Out Yer Dead](https://www.nexusmods.com/graveyardkeeper/mods/73)** - Control when and how fast the body-delivery donkey arrives at the graveyard. Multiple deliveries per day; faster walk.
- **[Grave Changes Redux](https://www.nexusmods.com/graveyardkeeper/mods/89)** - Raises the max quality of grave items and decorations from the vanilla cap up to 30.
- **[I Build Where I Want](https://www.nexusmods.com/graveyardkeeper/mods/60)** - Build anywhere on the map instead of only inside the pre-defined construction zones.
- **[Trees No More](https://www.nexusmods.com/graveyardkeeper/mods/50)** - Once you've cleared a tree (felled + stump dug), nothing grows back there. Useful for laying out a graveyard or garden.
- **[Regeneration Reloaded](https://www.nexusmods.com/graveyardkeeper/mods/66)** - Passive life and energy regeneration at a rate you control - the keeper recovers over time without needing to sleep, eat, or meditate.
- **[No Time For Fishing](https://www.nexusmods.com/graveyardkeeper/mods/44)** - Skips the fishing mini-game. Cast your line and the mod handles the rest.

## Save compatibility

- **Most mods are safe to add or remove mid-save** - they patch game behaviour at runtime and don't persist anything unusual to your save file.
- **Where's Ma' Storage** - the bumped capacity is stored per-container inside the save (as a regular `inventory_size` param), so uninstalling doesn't delete items: each chest loads at its bumped size even without the mod. Before uninstalling, it's worth tidying your personal inventory down to 20 items - vanilla caps the player at 20 slots, and anything left in slots 21+ will still exist in the save but can't be reorganised with vanilla UI until you drop some or reinstall the mod.
- **Gerry's Junk Trunk** - if you uninstall the mod, the trunk you built just reverts to a normal trunk. Nothing breaks and nothing needs cleaning up.
- **Back up your save** before any big config change. Most are safe; the exceptions are anything touching save files (Save Now) or inventory structure (Where's Ma' Storage's sliders).

## Support & contact

- **Ko-fi** - [ko-fi.com/p1xel8ted](https://ko-fi.com/p1xel8ted). Development, testing, and user support take real time; any support is greatly appreciated. If you'd rather not donate, clicking the Endorse button on the Nexus pages also helps.
- **Discord** - [discord.gg/Dy5ApMYYY8](https://discord.gg/Dy5ApMYYY8). Faster than Nexus comments for back-and-forth.
- **Bug reports / feature requests** - comment on the specific mod's Nexus page. GitHub issues are disabled; Nexus is the single channel.
- **Crash on startup?** Check `BepInEx\LogOutput.log` - a line starting with `GYK Mods` lists every mod that loaded plus your game/platform details, which is the first thing I'll ask for.

## Licence & credits

Licensed under the **GNU General Public License v3.0** - see [LICENSE](LICENSE) for the full text. In short: you're free to use, study, modify, and redistribute the source under the same licence; any distributed fork must also be GPL v3.

Individual mods credit their original authors on the Nexus page where the mod was based on a prior work (Max Buttons Redux, Rest In Patches, and others - see each Nexus page's Credits section).
