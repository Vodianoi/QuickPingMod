# QuickPing
[ExploreTogether](https://valheim.thunderstore.io/package/Rolo/ExploreTogether/)'s ping feature remastered. 

Compatible with **Valheim 1.0.21** (Ashlands, Bog Witch, Deep North).

## Install Instruction
Ping position and markers will always work but must be installed on all clients if you want everyone able to see pinged object's name display.

## Features
- Use T (by default, see config) to **ping what you point**, name of pointed object/creature is displayed in the world and on the minimap to all players.
  Some objects automatically add a pin on map if a player ping them:
  - **Ores and resources** (hammer): copper, tin, obsidian, silver, Petrified Gammeltroll, muddy scrap piles, Mistlands giant remains, flametal, bog iron, sulfur rock
  - **Pickables** (point): berries, mushrooms, thistle, dandelion, Jotun puffs, royal jelly, vineberries, fiddlehead, smoke puff, volture eggs, luminous larva, wild seeds...
  - **Dungeons and points of interest** (rune): Troll Caves, Burial Chambers, Bear Caves, Sunken Crypts, Frost Caves, Infested Mines and Citadel, Putrid Holes, Charred Fortress, Deep North dungeons, ruins, towers, lighthouses, shipwrecks, portals
  - **Traders and settlements** (house): Haldor, Hildir, the Bog Witch, villages, abandoned houses, swamp huts, farms, harbours
  - **Hildir's quest dungeons** use Hildir's map icons
- ***(New)*** **Ping whole structures**: pointing at any part of a village, ruin or dungeon entrance (walls, roofs, doors...) pings and pins the whole location. Your own buildings and resources worth pinning on their own (ores, berries...) are kept.
- Use G (by default, see config) to **force add a marker** (exclamation point by default if not filtered) **on map** with pointed object's name.
- If a pinned object is destroyed, it will also destroy its attached marker.
- Use Alt + Ping (by default, see config) and enter a custom name to **ping an object**.
- Change text colors (chat, ping, shout, whisper, names) in config 
- Change already existing Portal pin by pinging the portal again, it will change the marker's name to portal tag.

## What's Changed
* Compatibility with Valheim 1.0.21 (new save system, chat, destructibles)
* Ping whole structures (villages, ruins, dungeon entrances) instead of the building piece
* New pins for Ashlands, Deep North, Mistlands, Hildir and traders
* Fix chat showing a platform ID instead of the player name
* Fix pins not removed when a creature dies, and other ping/pin fixes
* Fix QuickPing files written outside the worlds folder on Steam Cloud (they are moved back automatically)

**Full Changelog**: https://github.com/Vodianoi/QuickPingMod/compare/1.5.11...1.6.0

## Configuration
The settings for QuickPing can be accessed in the BepInEx config file located in the `Valheim/BepInEx/config` folder. The settings include:

- **Bindings**
  - **PingKey**: The keybind to trigger a ping where you are looking (default: T)
  - **PingEverythingKey**: The keybind to add a pin on minimap to whatever you're looking at (default: G)
  - **RenameKey**: The keybind to rename a ping (default: T + LeftAlt)

- **General**
  - **PingWhereLooking**: Create a ping where you are looking when you press the Ping key (default: true)
  - **AddPinOnMap**: Add a marker on map when useful resources are pinged (ignored by G) (default: true)
  - **ClosestPinRange**: Minimum distance between objects to pin/replace portal tag (default: 2)
  - **DefaultPinType**: Default marker when forcing adding a pin on map (default: RandomEvent (!))
  
- **Colors**
  - **PlayerColor**: Color for Player name in pings/messages (default: green)
  - **ShoutColor**: Color for Shout ping (default: yellow)
  - **WhisperColor**: Color for Whisper ping (default: white with 75% opacity)
  - **PingColor**: Color for "Ping" ping (default: light blue)
  - **DefaultColor**: Default color (default: white)
  


## Known issues and ideas 
You can find the github and contribute by adding an issue at: [Github](https://github.com/Vodianoi/QuickPingMod)
Please report any bug !

## Downloads
*___[Thunderstore](https://valheim.thunderstore.io/package/Atopy/QuickPing/)___*

*___[Nexus](https://www.nexusmods.com/valheim/mods/2033)___*
