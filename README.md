# CNStudio
Check out the [original AssetStudio project](https://github.com/Perfare/AssetStudio) for more information.

This is the release of CNStudio, Modded AssetStudio that should work with CN unity games.
_____________________________________________________________________________________________________________________________

Some features are:
```
- Togglable debug console.
- Build Asset List of assets inside game files.
- CLI version.
- Option "Option -> Export Options -> Ignore Controller Animations" to export model/aniamators without including all animations (slow).
- Load Assets listed in `.txt` file.
- Select/Add/Remove entries "Option -> Specify CNUnity Key" | NOTE: double-click on row header to select an entry.
```
_____________________________________________________________________________________________________________________________
How to use:

```
1. Build CABMap (Misc. -> Build CABMap).
2. Load files.
```

CLI Version:
```
Usage:
  AssetStudioCLI <input_path> <output_path> [options]

Arguments:
  <input_path>   Input file/folder.
  <output_path>  Output folder.

Options:
  --silent                                                Hide log messages.
  --names <names>                                         Specify name regex filter(s).
  --types <Texture2D|Sprite|etc..>                        Specify unity class type(s)
  --containers <containers>                               Specify container regex filter(s).
  --key_index <GAMES> (REQUIRED)                          Specify key index.
  --map_op <AssetMap|Both|CABMap|None>                    Specify which map to build. [default: None]
  --CAB_name <CAB_name>                                   Specify CABMap file name. [default: CABMap]
  --map_name <map_name>                                   Specify AssetMap file name. [default: assets_map]
  --group_assets_type <ByContainer|BySource|ByType|None>  Specify how exported assets should be grouped. [default: 0]
  --version                                               Show version information
  -?, -h, --help                                          Show help and usage information
```

Looking forward for feedback for issues/bugs to fix and update.
_____________________________________________________________________________________________________________________________
Special Thank to:
- Perfare: Original author.
_____________________________________________________________________________________________________________________________

If you find `CNStudio` useful, you can leave a star 😄
If you want to support (optional), you can do so [here](https://ko-fi.com/razmoth)

Thank you, looking forward for your feedback
