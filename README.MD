# Quake 2 C# port
A C# Quake 2 port.

## Wishlist
Split project into multiple projects.

**1. Quake2Sharp.Engine**
This project should contain all core engine code.
Its output will be a library only.
At this level, dependencies should not be allowed, to ensure runnability on all platforms.
The engine itself should include game detection and loading mechanisms.

**2. Quake2Sharp.Game.Quake2**
This project should contain the actual Quake 2 game implementation for the engine to load.
Its output will be a library only.
At this level, dependencies should not be avoided, to ensure runnability on all platforms,
but it depends on the game implementation if limiting dependencies are used.

**3. Quake2Sharp.Server**
This project should produce the dedicated server lib and its starting mechanism.
It will load up the engine, make sure the game is loaded and spawn a dedicated server for people to play on.

**4. Quake2Sharp.Client**
This project should produce the client executable. Its should also implement the platform loader.
Upon starting, it should check the available platforms, initialize the preferred one and provide
a frontend to interact with the engine (menu, playing the game).

**5. Quake2Sharp.Platform.OpenTk**
This project should contain all platform specific implementations.

## Ideas
- Maybe implement Silk.Net
- Maybe implement renderer code dll, so multiple platforms can use the same renderer
- Maybe implement .png loading support
- Maybe implement gltf model format as alternative to .MD2
- Maybe implement loading raw trenchbroom .MAP files as alternative to .BSP
- Maybe implement modern renderer
- Maybe implement powerfull ui library for more complex menus and uis
- Maybe implement doom3-style ui textures
- Maybe implement own map editor instead of using trenchbroom

## Credits
- ID Software for releasing their Quake 2 source code https://github.com/id-Software/Quake-2
- Bytonic for their Java port https://bytonic.de/html/jake2.html
