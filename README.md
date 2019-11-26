![preview](https://user-images.githubusercontent.com/39247311/64912038-bf064180-d729-11e9-98dd-17c48efe0a7f.PNG)
# FPSCounter for BepInEx
An FPS Counter plugin for BepInEx 5.0 or later. Here are some of the features:
- Accurately measures true ms spent per frame (not calculated from FPS)
- Measures time spent in each of the steps Unity takes in order to render a frame (e.g. how long all Update methods took to run collectively)
- Measures time spent in each of the installed BepInEx plugins (easy to see performance hogs, only counts code running in Update methods)

## How to use
1. Install [BepInEx 5.0](https://github.com/BepInEx/BepInEx) or newer.
2. Extract the release into your game root, the .dll should end up in BepInEx\plugins directory.
3. Start the game and press U + LeftShift.

The on/off hotkey and looks can be configured in the config file in bepinex\config (have to run the game at least once to generate it), or by using [BepInEx.ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager).
