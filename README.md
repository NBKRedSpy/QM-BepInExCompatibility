# QM-BepInExCompatibility

This is *only* required if BepInEx is installed and running.

A BepInEx mod that fixes Quasimorph Steam Workshop mod load issues when BepInEx is installed and that mod has reference dlls in their folder.

Install the mod and the workaround will be executed.

By default, the mod will use the Quasimorph/mods folder if it exists.  If not the Steam Workshop directory.
If the user has a CustomModsPath set in the config, that will always be used.

# Configuration

The configuration file can be found at ```BepInEx\config\nbk_redspy.QM-BepInExCompatibility.cfg```
The file will be created when the game is run once after the install.


The CustomWorkshopPath below is a hack for Steam installs where the game's libraries are installed in a different directory than the default install.

|Name|Default|Description|
|--|--|--|
|CustomModsPath|""|If set, will be used as the folder to search for mods|



# Issue
When BepInEx is installed, a component called Doorstop prevents Steam Workshop mods from loading any dlls that are in the mod's directory.
This is because Doorstop excludes searching the mod's folder and instead only searches the game's and  BepInEx's directories.

Normally .NET will automatically search for and load any required dlls in the Steam Workshop mod's folder.

This will cause file load exceptions when running the game.

# Fix
This mod fixes the issue by loading every dll located in a Quasimorph Workshop mod folder.

When .NET tries to load a reference, the mod will use the dll that is already in memory instead of searching for a file  to load.

Note that if a dll is loaded more than once, the subsequent loads will use the version in memory instead of loading the file again.

# Important! Harmony Based Mods
When creating Harmony based mods with BepInEx installed, the mod may work with BepInEx installed, but not without it.  Workshop users will not have BepInEx installed.

Make sure to include the following files to the Steam Workshop mod folder for a Harmony based mod.

```
0Harmony.dll
Mono.Cecil.dll
MonoMod.RuntimeDetour.dll
MonoMod.Utils.dll
```


The reason is BepInEx will load the modding dlls from the BepInEx directories.  When BepInEx is not installed or enabled, the mod will not have the required dlls.



Test the mod without BepInEx running.  BepInEx can be disabled by renaming the winhttp.dll in the game's directory.

## Other Mods Load Fixing
A corner case would be if another Steam Workshop that has the required modding dlls is loaded before the user's mod was loaded.  

It would be best to test the mod with no other subscribed mods.


# Alternatives To This Mod

## Manifest Change For Every Mod
For every mod, add all dlls in the directory to the "Assemblies" entry in the modmanifest.json.

## Custom dll Load
Implement this mod's functionality by using Assembly.LoadFrom on all the dlls in the directory.  However, every mod author would have to do this.

## Change Doorstop Config

Add every Steam Workshop mod's path in the ```doorstop_config.ini``` search path.

```dll_search_path_override =```

Example:

```dll_search_path_override = <Steam Dir>>\steamapps\workshop\content\2059170\3282459391;<Steam Dir>>\steamapps\workshop\content\2059170\1234567890```

# Support
If you enjoy my mods and want to buy me a coffee, check out my [Ko-Fi](https://ko-fi.com/nbkredspy71915) page.
Thanks!

# Change Log
## 1.4.0
* Support for non Steam versions.
* If no custom folder is defined, searches for the game's `mods` folder and then for the Steam Workshop folder.

## 1.3.0

Changed Custom path to indicate the root of a mods folder instead of expecting it to be a Steam Workshop layout.

## 1.2.0

Supports multiple Steam libraries using Steam's libraryfolders.vdf file.
