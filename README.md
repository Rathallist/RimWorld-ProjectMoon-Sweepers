# Project Moon — Sweepers

A [RimWorld](https://rimworldgame.com/) mod that adds the **Sweepers** from the Project Moon universe — humanoid entities whose bodies are made of **Vital Fuel**, a living red liquid contained within structural black suits.

**Repository:** https://github.com/Rathallist/RimWorld-ProjectMoon-Sweepers

> Fully playable in **English** and **French**.

---

## Features

- **Sweeper xenotype** — Beings of Vital Fuel that don't eat or sleep, but must maintain their fuel level by absorbing organic matter. Adapted to darkness, vulnerable to light and piercing weapons, resistant to blunt impacts.
- **Vital Fuel network** — A dedicated pipe network (pipes, reservoirs, taps) carrying Vital Fuel between buildings. Liquidify organic matter to produce fuel; let your Sweepers recharge at taps.
- **The Mother** — An incubator that produces new Sweepers, upgradable with three stackable modules: *gestation* (speed), *multiplication* (count), and *enhancement* (starting skills).
- **Weapons & armor** — Crystallize Vital Fuel into Hooks (melee weapons that drain organic matter), forge the black Sweeper armor, and equip back tanks for extra fuel capacity.
- **Sweeper faction** — A dedicated Sweeper union with its own caravans and Vital Fuel goods. Without researching *Translation*, neither side can trade or communicate.
- **Ideology** *(optional)* — With the Ideology DLC: a full "Will of the City" ideology — the Sweeper meme, the City structure, a dedicated culture, precepts, and the Sweeping Coordinator role.
- **Scenario** — *Sweeper Emergence*: start as two exiled Sweepers with a Mother and rebuild your family.

---

## Requirements

| Mod / DLC | Status |
|-----------|--------|
| [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077) | **Required** |
| Biotech DLC | **Required** (xenotype & genes) |
| Ideology DLC | *Optional* (unlocks the Sweeper ideology content) |

**Load order:** after Harmony and Biotech. Place near the end of your load order for best compatibility.

---

## Installation

### From source
1. Download or clone this repository.
2. Copy the mod folder into your RimWorld `Mods/` directory.
3. Enable the mod in-game (with Harmony loaded first).

### Building the C# assembly
The compiled DLL is included in `Assemblies/`. To rebuild it yourself:
1. Open `Source/ProjectMoonSweepers/ProjectMoonSweepers.csproj`.
2. Set the RimWorld managed path if needed.
3. Build in Release configuration — the DLL is output to `Assemblies/`.

---

## Languages

The mod ships with full French and English localization. The base Defs are written in French, with English translations provided under `Languages/English/`.

---

## Credits & License

**Project Moon** and its universe belong to their respective owners. This is a **non-commercial fan mod**, not affiliated with or endorsed by Project Moon.

Mod code and assets by **Althar**. See [LICENSE](LICENSE) for details.
