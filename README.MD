# Quake2Sharp
An ID Tech 2 Engine modernization project.

## Compiling / Running

Method 1 - Release build:
- Make sure the Quake2Sharp files are next to your baseq2 directory.
- Run

Method 2 - Building from source:
- Install latest .NET SDK
- Run (replace win-x64 with your target platform. Examples: linux-x64, osx-x64, osx-arm64, ...)
  - `dotnet publish -c Release --self-contained true -p:PublishReadyToRun=true -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:DebugSymbols=false -r win-x64 -p:TargetPlatform=win-x64`
- Get the output files from Quake2Sharp/bin/Release/<.NET version>/<platform>/publish/
- Follow method 1

Method 3 - Running from IDE:
- Install latest .NET SDK
- Make sure the baseq2 directory lies in the project root directory.
- Open the solution file and run

## Goals
- The codebase should be raised to modern c# standards
- The engine should be highly moddable
- Provide a good base for custom games
- The repository should contain a fully compatible Quake2 gamecode
- Get the codebase running on any technically possible platform

## Roadmap
- Refactor the whole codebase

- Split project into multiple projects

  - **Quake2Sharp.Engine**

    This project should contain all core engine code.
    Its output will be a library only.
    At this level, platform specific dependencies should not be allowed, to ensure runnability on all platforms.
    The engine itself should include game detection and loading mechanisms.

  - **Quake2Sharp.Game.Quake2**

    This project should contain the actual Quake 2 game implementation for the engine to load.
    Its output will be a library only.
    At this level, platform specific dependencies should not be allowed, to ensure runnability on all platforms.
    Other games might ofcourse decide to use platform specific dependencies.

  - **Quake2Sharp.Server**

    This project should produce the dedicated server executable and its starting mechanism.
    It will load up the engine, make sure the game is loaded and spawn a dedicated server for people to play on.

  - **Quake2Sharp.Client**

    This project should produce the client executable. Its should also implement the platform loader.
    Upon starting, it should check the available platforms, initialize the preferred one and provide
    a frontend to interact with the engine (menu, playing the game).

  - **Quake2Sharp.Platform.OpenTk**

    This project should contain all platform specific implementations.

- Rendering
  - Switch to Silk.Net
  - Implement Silk.Net OpenGL 4.6 renderer
  - Implement Silk.Net Vulkan renderer

- Moddability
  - Support .png textures
  - Support .glb models
  - Support trenchbroom .map maps

- Other
  - Implement powerful ui system for more complex menus and uis
  - Implement Doom3-style ui textures

## Credits
- ID Software for releasing their Quake 2 source code https://github.com/id-Software/Quake-2
- Bytonic for their Java port https://bytonic.de/html/jake2.html
