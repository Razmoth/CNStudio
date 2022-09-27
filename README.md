# PGRStudio
Check out the [original AssetStudio project](https://github.com/Perfare/AssetStudio) for more information.

This is the release of PGRStudio, Modded AssetStudio that should work with Punishing: Gray Raven.
_____________________________________________________________________________________________________________________________

Some features are:
```
- Togglable debug console.
- Build Asset List of assets inside game files.
- CLI version.
- Option "Option -> Export Options -> Ignore Controller Animations" to export model/aniamators without including all animations (slow).
- Load Assets listed in `.txt` file.
- Support multiple game versions (Currently: "Global, JP"), Option `Options -> Specify Game Version`.
```
_____________________________________________________________________________________________________________________________
How to use:

```
1. Build PGR Map (Misc. -> Build PGRMap).
2. Load PGR files.
```

CLI Version:
```
AssetStudioCLI 0.16.0.0
Copyright (C) 2022 AssetStudioCLI

  -v, --verbose           Show log messages.

  -t, --type              Specify unity type(s).

  -f, --filter            Specify regex filter(s).

  -m, --map               Build CABMap/AssetMap.

  -s, --version           (Default: 0) Specify game version.

  --help                  Display this help screen.

  --version               Display version information.

  input_path (pos. 0)     Required. Input file/folder.

  output_path (pos. 1)    Required. Output folder.
```

Looking forward for feedback for issues/bugs to fix and update.
_____________________________________________________________________________________________________________________________
Special Thank to:
- Perfare: Original author.
