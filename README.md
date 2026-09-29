# GK2 Zombie HQ

A BepInEx plugin for **Graveyard Keeper 2** that adds a zombie count HUD and a manager
panel: see every zombie, open the game's zombie window, and safely recall a zombie from
its workstation.

## Features

- **Count HUD** (bottom-left, draggable): zombie icon + the current number of zombies
  (only zombies actually in the world are counted). Hidden in the main menu and during
  cutscenes/loading, matching the game's own HUD.
- **Manager panel** (default `F8`, or a gamepad button): name, type, white/red skulls,
  collar, status per zombie.
- **Open** - opens the game's own zombie window (organs, talents, equipment).
- **Recall** - takes the zombie off whatever it is doing exactly like the game's own "take"
  (stations, mine/sawmill/clay/sand with carried items, porter, the basement wheel, the
  garrison) and moves it to store. Zombies on the wheel show as "On the wheel".
- **Camera follow** - locks the camera to a zombie and follows it; Esc restores your camera.
- **Gamepad support** - D-pad / left stick to move, A = select, B = close; the game pauses
  while the panel is open; long lists scroll (wheel, gamepad or drag).
- **Settings** in the in-game **Mods** menu: language (auto = game language / en / ru / any
  file you add), HUD on/off, font size, offsets, hotkeys, gamepad button.
- **Translations** (1.5.2): all texts live in `BepInEx\plugins\GK2ZombieHQ\Localization\<lang>.json`
  (`en.json`/`ru.json` are written on first launch and never overwritten). Copy `en.json` to e.g.
  `de.json`, translate the values, restart and pick `de`. Missing lines fall back to English;
  `settings.*`/`mod.*` keys translate the Mods settings screen.

## Requirements

- Graveyard Keeper 2 (Steam).
- BepInEx 5.4.23.5 x64 and **GK2 Mod Framework** (both installed by
  [GK2 Mod Installer](https://github.com/otkosss-png/GK2ModInstaller)).

## Install

Copy `BepInEx/plugins/GK2ZombieHQ/GK2ZombieHQ.dll` into
`<game>\BepInEx\plugins\GK2ZombieHQ\`, or install the Workshop item via the
[GK2 Workshop Auto-Loader](https://steamcommunity.com/sharedfiles/filedetails/?id=3807406994).

## Build

```
& "<dotnet>" build -c Release
```
References the game's assemblies from `$(GameDir)` (default
`E:\SteamLibrary\steamapps\common\Graveyard Keeper 2`).

## Layout

- `src/GK2ZombieHQ.Core` — pure logic (netstandard2.0), unit-tested.
- `src/GK2ZombieHQ` — the BepInEx plugin (netstandard2.1).
- `tests/GK2ZombieHQ.Core.Tests` — xUnit.
- `docs/superpowers` — design spec, plan, art.

Not affiliated with the developers or publishers of Graveyard Keeper 2.
