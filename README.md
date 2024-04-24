# Boplmap Mod

## Overview
The Boplmap Mod is a custom modification for Bopl Battle that introduces support for loading custom map configurations from `.boplmap` files. These files contain detailed information about map elements such as platforms, their positions, rotations, biomes, and types.

## Features
- Load custom maps from `.boplmap` files.
- Define map elements including platforms with specific properties like position, rotation, biome, and type.
- Support for creating new and interesting custom maps.

## Getting Started
To use the Boplmap Mod in your game, follow these steps:

1. **Installation:**
   - Download the latest version of the mod from the releases section.
   - Install BepInEx and Splatch if you haven't already.

2. **Usage:**
   - Place your `.boplmap` files in the BoplBattle/Splotch_config/CustomMaps or the BoplBattle/Bepinex/Plugins/Maps/ folder.
   - Launch the game and select the Boplmap Mod from the mod menu.
   - Load your custom map and start playing!

3. **Creating Custom Maps:**
   - Use the provided `.boplmap` file example as a template.
   - Define your map elements including platforms, their positions, rotations, biomes, and types.
   - Save the file and place it in the maps folder.

4. **Feedback:**
   - We welcome any feedback or suggestions for improving the mod. Feel free to reach out to us through the mod's community forums or GitHub repository.

## Example `.boplmap` File
```json
{
  "version": "1.0",
  "mapName": "Boplmap",
  "description": "A Bopl Map",
  "developer": "Abstractmelon",
  "dateCreated": "2024-03-17",
  "mapId": "1",
  "siteVersion": "0.3",
  "icon": "https://raw.githubusercontent/abstractmelon/boplmapmaker/main/images/icon.jpeg",
  "platforms": [
    {
      "transform": { "x": 10.0, "y": 20.0 },
      "size": { "width": 10.0, "height": 10.0 },
      "radius": 1.0,
      "rotation": 0.0,
      "biome": "Plain",
      "visibility": true,
      "AntiLockPlatform": false,
      "blank": false,
      "UseCustomMass": false,
      "CustomMass": 10
    },
    {
      "transform": { "x": 10.0, "y": 20.0 },
      "size": { "width": 10.0, "height": 10.0 },
      "radius": 1.0,
      "rotation": 0.0,
      "biome": "Plain",
      "visibility": true,
      "AntiLockPlatform": false,
      "blank": false,
      "UseCustomMass": true,
      "CustomMass": 10
    }
  ]
}
```
## Compatibility
The Boplmap Mod is compatible with the following game & mod versions:

- Bopl Battle 2.1.3
- Solpch 0.2.0, 0.2.1, 0.3.0, 0.4.0
- BepInEx 5.4.22

Ensure that you are using the appropriate version of BepInEx and Splatch for your game.

## Credits
- MapMakerSite & .boplmap Developed by **Abstractmelon**.
- CustomMapLoader Developed by **DavidLovesJellycarWorlds**
- Special thanks to **Unluckycrafter** for their support and contributions.
- **shad0w_dev**

## License
This mod is released under the Creative Commons Attribution 4.0 license. See the `LICENSE` file for more information.
