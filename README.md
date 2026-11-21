# Adventure

A C# port of Warren Robinett's *Adventure* for the Atari 2600. The rules come from `adventure.asm`. The MonoGame shell draws the rooms and plays the sound. It does not emulate the 6502, and it does not run the display kernel.

`docs/behavior.md` is the checked rulebook. When the note and a comment in the disassembly disagree, the note is the one that was compared with the code that actually runs. The comments call room-record offset 6 "left". The edge checks use offset 6 for the right exit.

## Play

```text
dotnet run --project src/Adventure.Game
```

The game targets .NET 9 and MonoGame DesktopGL. Tests:

```text
dotnet test src/Adventure.Core.Tests
```

| Key | 2600 control |
| --- | --- |
| Arrows or WASD | Joystick |
| Space | Button |
| Enter | Reset |
| Tab | Select |

Select on the number room cycles games 1, 2, and 3. Select anywhere else returns to that screen. Reset during play drops you in the yellow castle and leaves objects where they are. Reset after a win reloads the title.

The window is the 160-pixel playfield, scaled up. Coordinates match the cartridge: x grows to the right, y grows up. The visible picture is scanlines `$68` through `$08`.

## What was left on the cartridge

The 2600 kernel can show two player sprites, two missiles, the ball, and a reflected playfield. Adventure spends them like this:

- The player is the ball. One column wide, four scanlines tall, with the coordinate in the middle of those lines.
- The two player sprites are whichever objects the kernel picked for that frame. This port draws every object in the room and tests lit pixels instead of `CXP0FB` and `CXP1FB`.
- The missiles are the thin vertical bars, at x `$0D` and x `$96`.
- The playfield is 40 cells, but only 20 are stored. Bit 0 of the room's playfield control reflects the left half onto the right. A few ROM graphics are short because the next room's first row is also their last. The maps here store the full seven rows.

Rooms colored `$08` match the gray background, so the maze is invisible. The surround is a quad-width block parked on the player in those rooms. Walls draw over it, which is what reveals the passages.

The bridge is the other quad-width sprite. Its hole is empty pixels. Standing in the opening does not pick it up, and it does not open a wall while you are carrying it. Drop it so the hole sits on the wall, then walk through.

## Three fields, one loop

`MainGameLoop` waits for vertical sync three times. The television still shows 60 fields a second. The game logic is spread across those three fields, and a lot of the timing only makes sense that way.

The joystick is read on the first field. A clear field stores "no direction" and a snapshot of the ball. The other two fields do not read the stick, so in open space the ball moves 3 pixels once per three fields: 60 pixels a second. If that step hits a wall, a thin bar, or a closed gate, the later fields restore the snapshot and try again with one axis removed. The second field is vertical only. The third is horizontal only. That is the slide along a wall, not a slower cruise.

Other work is pinned to one field of the three:

- Sound and the win check run at the top of the loop, before the first move.
- The bat and the portcullises run on the vertical field. The green dragon, then the yellow dragon, run after that picture.
- The red dragon and the magnet run on the horizontal field. The magnet does not move. It pulls the first key, sword, bridge, or chalice in its room one pixel toward a point 8 pixels below its coordinate.
- `$E5`, the frame count, increments with that same clock. The chalice and the author credit index it, so they change color every three fields during play. After the win, `NonActiveLoop` returns to the top once per field, and both the fanfare and the color step at 60 Hz.

Dragons and the bat do not test walls. Their step is an integer pixel count on their own field: 3 for the red dragon and the bat, 2 for the yellow and green dragons. There is no fractional velocity anywhere in the ROM.

## TIA sound

The picture does not contain samples. `MakeSound` writes the three channel-0 registers and leaves them there. The television-rate oscillator inside the TIA turns those registers into audio between writes. Channel 1 is only ever silenced.

While the game is active, `MakeSound` runs once per three fields. A roar, a swallow, a death, a pickup, and a drop are a countdown in `$DF` plus a type in `$E0`. Each call decrements the count and derives `AUDC0`, `AUDF0`, and `AUDV0` from what remains. The win fanfare stores `$FF` and also writes that count to `COLUPF`, which is why the walls cycle color with the tune. After the win the same routine runs every field, so the fanfare steps three times as fast as a sound that started during play.

`TiaAudio` is the oscillator, not a recording. Once per 60 Hz field the shell hands it the current registers. It keeps only the low 4 bits of control and volume and the low 5 bits of frequency, which is all the chip uses. A software clock at 31,440 Hz (the usual NTSC TIA audio rate, the color clock divided by 114) advances the circuit, and the samples are resampled to 44.1 kHz.

The circuit is the standard one. `AUDF` is the divider reload. `AUDC` selects the source: a 4-bit, 5-bit, or 9-bit polynomial, a pure toggle, or one of those clocks divided by 6, 15, 31, or 93. Volume scales the one-bit output. Between register writes the polynomials keep running, so a value held for three television fields still has the pitch and the noise the TIA would have made.

## Layout

| Path | What it is |
| --- | --- |
| `adventure.asm` | The disassembly the rules were taken from |
| `docs/behavior.md` | The rules already checked against that disassembly |
| `src/Adventure.Core` | Rooms, objects, and the three-field game |
| `src/Adventure.Game` | MonoGame window, drawing, and `TiaAudio` |
| `src/Adventure.Core.Tests` | The rulebook, as tests |
