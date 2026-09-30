# Second Life: A Jobless Rebirth Tale

An NES fan game inspired by *Mushoku Tensei* and the "reborn in another world" isekai genre.
It's a real NES ROM (mapper 0 / NROM, 32 KB PRG + 8 KB CHR), written in 6502 assembly.
It runs in any NES emulator or on a flash cart.

**Play it:** load `second_life.nes` in an emulator (Mesen, FCEUX, Nestopia, or a browser emulator).

## Story

A 34-year-old shut-in dies saving some kids from a truck and wakes up as a baby in a world of swords
and sorcery. Now named **ARLO**, he gets a second chance to live without regrets.

Chapter 1 covers his childhood:

1. Meet **LYRA**, a blue-haired demon-race mage who comes to be your tutor. She's shocked to find you
   can cast without chanting.
2. Help **MIRA**, a green-haired girl the village kids bully by the river.
3. Learn Fireball, then pass LYRA's final exam: defeat the fire wyrm nesting deep in the East Woods.

As in the source material, your mana grows: each time you drain it to zero, your max MP goes up.

All characters, names and dialogue are original. The game is an homage, not a port.

## Controls

| Button | Action |
| --- | --- |
| D-pad | Walk |
| A | Talk / read signs / advance text |
| B | Cast the current spell |
| SELECT | Switch spell (Water Bolt / Fireball) |
| START | Start / pause |

Tips: your mom heals you at home, and the healing spring north of the East Woods restores HP and MP.

## Building

Requires `python3` and the [cc65](https://cc65.github.io/) toolchain (`apt install cc65`).

```sh
make          # builds second_life.nes
```

- `tools/make_chr.py` draws all graphics (tiles, sprites, font, the procedurally drawn wyrm) as pixel art in code
  and writes the CHR ROM.
- `tools/make_data.py` holds the maps, dialogue (auto word-wrapped), music (written in a tiny MML dialect),
  and sound effects.
- `src/` is the engine: `main.s` (boot, PPU, HUD, rooms, modes), `dialog.s` (typewriter text box),
  `player.s`, `entities.s` (NPCs, enemies, boss, projectiles) and `sound.s` (3-channel music + SFX).

## Testing

`make test` plays the whole game headlessly with [jsnes](https://github.com/bfirsh/jsnes). A small bot
with pathfinding walks the story from the title screen to the ending, fights with magic, and checks the
story flags at each step. It saves screenshots to `build/shots/`. The boss fight gets an HP/MP top-up
through RAM pokes so the bot's dodging doesn't matter. Run `npm install` in `tests/` first.
