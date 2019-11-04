# XCom2ModTool

A command line tool for XCOM 2 modding.

Amongst other things, this tool can:

- Create new (mostly empty) mods
- Rename a mod, making all required changes (though I'd take a backup first!)
- Build mods without opening ModBuddy
- Quickly navigate to useful XCOM 2 folders
- Update .x2proj files to include all of a mod's files

It does currently require that you keep the same folder structure for your mods
as ModBuddy generates. Although in theory there's no reason a future version of
the tool needs either this or the .x2proj file to do its job.

The tool is compatible with both the base game and War of the Chosen.

It looks in the Windows registry to find out where you've installed Steam, and
interrogates Steam to find out where you've installed XCOM 2, but you don't need
Steam running.

## Usage

    usage: XCom2ModTool [--version ] [-w | --wotc] [ -v | --verbose ]
                        [options]
                        <command> [<args>]

Commands include:

      help           Display help on a command
      create         Create a mod
      rename         Rename a mod
      build          Build a mod
      open           Open a specific XCOM folder
      clip           Copy a specific XCOM folder to the clipboard
      update-project Update a mod's project file

Other commands may be available.

Specify `-w` or `--wotc` when working on War of the Chosen mods, or to open
War of the Chosen rather than base game folders.

## Building Mods

The `build` command can build mods. Unlike ModBuddy, the tool
defaults to a minimalist approach, performing only the absolutely required steps
in order to build a mod. You can still `build full` if you want (or need) a full
build. You may want to do a full build the first time you use the tool.

A full build involves (in ModBuddy, or with the tool):

- Copying your mod into `%SDK%\XComGame\Mods`
- Deleting `%SDK%\Development\Src` and refreshing it from `%SDK%\Development\SrcOrig`
- Copying your mod's source code into `%SDK%\Development\Src`
- Deleting compiled scripts from `%SDK%\XComGame\Script`
- Compiling the game's scripts
- Compiling your mod's scripts
- Compiling your mod's shaders

ModBuddy performs all of these steps, even if they're unnecessary, which they usually are.

This tool only compiles the game if it needs to, which means a build is often faster, as it's just:

- Updating only the relevant parts of `%SDK%\Development\Src`
- Cleaning only your scripts from `%SDK%\XComGame\Script`
- Compiling your mod from source
- Compiling your mod's shaders

If your mod is INI and/or INT only, the tool skips most of these steps as well.

It only compiles shaders if it finds `.upk` or `.umap` files in your mod's
Content folder. In future it may be smarter, since if these files haven't
changed since your last build, then recompiling shaders is unnecessary.

The game scripts do need to be rebuilt if you switch from a debug to release
build. The tool detects this by checking whether the SDK's `Core.u`
file was last built in debug or release; by default, it'll do a debug build
if the game was built in debug, or a release build otherwise. If you want to
change between debug and release, you can explicitly do a `build full` for
release or a `--debug build full` for debug. Release is the default.

## Renaming Mods

The `rename` command can rename mods. Renaming a mod involves changing:

- Several folder, solution and project names
- Specific text in the solution file
- Specific XML in the project file
- Certain parts of your mod's config files
- Certain parts of your mod's localization files

I usually forget some of these and they're easy to mess up, especially the
`.ini` and `.int` files, since you have to test the mod to see if you got it
right, since broken config or localization will still build. So the tool can do
these for you.

The tool tries to be smart about it: it's better than grep, but worse than human
effort. It's no doubt possible to craft a mod which the tool will helpfully
break for you. Backups are recommended, though I use it without issues.

## License

XCom2ModTool is licensed under the GPL v2.0. I'd prefer to release it under the
MIT license, but some (currently experimental) save game parsing code uses the
LZO decompression library which is GPL v2.0, so the tool has to be too.