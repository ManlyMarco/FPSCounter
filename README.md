# FPS counter and performance statistics for BepInEx
A BepInEx plugin that measures many performance statistics of Unity engine games. It can be used to help determine causes of performance drops and other issues. Here are some of the features:
- Accurately measures true ms spent per frame (not calculated from FPS)
- Measures time spent in each of the steps Unity takes in order to render a frame (e.g. how long all Update methods took to run collectively)
- Measures time spent in each of the installed BepInEx plugins (easy to see performance hogs, only counts code running in Update methods)
- Measures memory stats, including amount of heap memory used by the GC and GC collection counts (if supported)

![preview](https://user-images.githubusercontent.com/39247311/77855748-c1764780-71f2-11ea-8e8e-0e9a35d9866b.png)

## How to use
1. Install [BepInEx 5.0](https://github.com/BepInEx/BepInEx) or newer.
2. Extract the release into your game root, the .dll should end up in BepInEx\plugins directory.
3. Start the game and press U + LeftShift.

The on/off hotkey and looks can be configured in the config file in bepinex\config (have to run the game at least once to generate it), or by using [BepInEx.ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager).
