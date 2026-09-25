# GK2 Zombie HQ

A BepInEx plugin for **Graveyard Keeper 2** that adds a zombie count HUD and a manager
panel: see every zombie, open the game's zombie window, and safely recall a zombie from
its workstation.

## Features

- **Count HUD** (bottom-left, draggable): zombie icon + number. Optional "current / allowed"
  (set the allowed value in settings; the number turns red when above).
- **Manager panel** (default `F8`): name, type, white/red skulls, collar, status per zombie.
- **Open** - opens the game's own zombie window (organs, talents, equipment).
- **Recall** - detaches the zombie from its station (game's own flow) and moves it to store.
- **Settings** in the in-game **Mods** menu: language (en/ru/auto), HUD on/off, font size,
  offsets, allowed zombies, hotkeys.

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
