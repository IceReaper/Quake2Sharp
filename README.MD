# IdEngine

A modern C# port of the ID Tech 2 engine along its games, based on the Yamagi Quake II source code.

## Progress

The project already runs and lets you play Quake II, however it is still largely based on an older source port.
The Yamagi Quake II source code will be ported on top of the existing source code, ensuring changes can be tested immediately.

### Base project

- [x] Project setup
- [ ] Port of the Yamagi Quake II source code.
	- [ ] Engine
      - [ ] common
      - [ ] backend
      - [ ] server
	  - [ ] client
	- [ ] Quake 2
	- [ ] The Reckoning
	- [ ] Ground Zero
	- [ ] Capture the Flag

### Modernize codebase

While the port will be pretty straight forward, this phase focuses on readability and maintainability.

- [ ] Make code non-static
- [ ] Make the addons properly extend the base game
- [ ] Remove limits

### Modernize toolchain

Doing whatever required to make the project a solid foundation for new games or mods.

- [ ] Deploy the projects as NuGet packages
- [ ] Implement png texture support
- [ ] Implement GLTF model support
- [ ] Implement TrenchBroom .map support
- [ ] Implement bare-bones example game

### Platform support

Ensure that projects using IdEngine will run nearly everywhere.

- [ ] Implement Vulkan platform
- [ ] Implement Godot platform
- [ ] Implement Unity platform

## Based upon:

| Project          | Source                                                                                   | Version |
|------------------|------------------------------------------------------------------------------------------|---------|
| IdEngine         | [Yamagi Quake II](https://github.com/yquake2/yquake2)                                    |    8.20 |
| Quake2           | [Yamagi Quake II](https://github.com/yquake2/yquake2)                                    |    8.20 |
| Quake2Xatrix     | [The Reckoning](https://github.com/yquake2/xatrix)                                       |    2.11 |
| Quake2Rogue      | [Ground Zero](https://github.com/yquake2/rogue)                                          |    2.10 |
| Quake2Ctf        | [Capture the Flag](https://github.com/yquake2/ctf)                                       |    1.09 |
