# Adventure behavior

This note is the rulebook for the C# game. It comes from the comments and data tables in `adventure.asm`. It does not describe the 2600 display kernel (scanlines, TIA registers, sprite sorting, or the B&W switch).

The MonoGame game draws each room as a wall grid, draws the objects in that room, and runs the rules below. Keyboard replaces the joystick and the console switches: arrows or WASD move, space is the button, Enter is reset, and Tab is select.

## Win and flow

The player wins when the chalice's room is `$12`, the yellow castle entry (the room outside the gate, not the castle interior `$11`). The check is at the start of the three-field loop, after `MakeSound`. The game becomes inactive and the game-over noise starts. Movement still runs for that loop. The next loop is `NonActiveLoop`, which calls `MakeSound` once per field and skips movement, so the fanfare steps every frame. A sound that starts while the game is active still steps once per three fields.

Power-on, from `CheckGameStart`, shows room `$00` with the player at y `0` (x is `0` because RAM was cleared). Game 1's objects are loaded and nothing is carried. The game is active. The player stays off the screen until movement wraps that y.

Reset while the game is active, including on that title, places the player in the yellow castle (`$11`) at (`$50`, `$20`), drops the carried object, sets the three dragon states to `$00`, and clears the note count. Object rooms stay where they are. Reset after a win reloads the title for the current game and makes the game active again. The note that was already running keeps counting.

Select in room `$00` advances the level `$00`, `$02`, `$04`, then back to `$00`, and reloads that game's objects with the player at y `0` again. Select in any other room reloads the title without changing the level. A reload copies the layout, including the bat's movement and the gate states, and leaves the player's x alone.

## Rooms

There are 31 rooms, `$00` through `$1E`. Each room has a color, a 7-row playfield, optional thin vertical bars, and four exits.

Playfield bytes are `PF0`, `PF1`, `PF2` for each row, top row first. The left half is 20 cells: `PF0` bits 4..7, then `PF1` bits 7..0, then `PF2` bits 0..7. Bit 0 of the room's playfield control reflects that half to form the right 20 cells. When the bit is clear, the right half repeats the left half (the black-maze rooms marked duplicated). A few graphics in the ROM are stored short because the next room's first row is also their last row. The C# maps store the full 7 rows.

Playfield control bit 7 is a thin wall at x `$0D`. Bit 6 is a thin wall at x `$96`. Those bars block the player the same way a wall cell does.

Room colors in the source include yellow `$1A` (yellow castle), white `$0C`, black `$00`, purple `$66`, blue `$86`, red `$36`, and green `$C8`. Rooms colored `$08` match the gray background, so their walls are invisible (catacombs and the black maze).

Exits are stored at offsets 5..8 of each room record. Offset 5 is the room entered by walking up. Offset 7 is down. The disassembly comments call offset 6 "left" and offset 8 "right", but the edge checks use them the other way: offset 6 is the room entered by walking off the right, and offset 8 is the room entered by walking off the left. Trust the edge checks.

If an exit byte is `$80` or higher, the real room depends on the game. Subtract `$80`, add the game index (0, 1, or 2), and read `RoomDiffs`. On game 1 that sends the player down from room `$01` into the black castle, and it rewires the castle entries.

Crossing an edge wraps the player:

| Edge | Trigger | New position | Exit offset |
| --- | --- | --- | --- |
| Up | y ≥ `$6A` | y = `$0D` | 5 |
| Right | x ≥ `$9F` | x = `$03` | 6 |
| Down | y < `$0D` | y = `$69` | 7 |
| Left | x < `$03` | x = `$9E` | 8 |

Y increases upward. X increases to the right.

The right edge of room `$03` is special. If the black dot is not in room `$15`, that edge sends the player to the secret room `$1E` at x `$03` instead of the normal exit. On game 1 the dot starts in `$15`, so the secret room stays closed until the dot is moved. The right thin wall does not block the player while the dot is in the same room. The cartridge ignores that missile hit when the dot is the sprite in its slot, and this game has no sprite sort, so the dot's room is the stand-in. Dragons and the bat do not take the secret exit.

## Invisible surround

From `Surround`. It runs on the vertical field, after the player steps and before the bat. A room colored `$08` places the surround at the player's x − `$0E` and y + `$0E`, as bytes. An x of `$F0` or above becomes `$01`. An x of `$82` or above, and below `$F0`, becomes `$81`. Any other room clears it. The picture is `GfxSurround`, a solid block, in color `$28`. The walls draw over it, so the block shows the maze around the player.

The yellow castle's walkable exit in this first slice is down, into room `$02` (the corridor under the castle). Its side edges are walled.

## Objects

Every carried or roaming object has a room, an x, and a y. Dragons and the bat also store a movement byte and a state. Portcullises store only a state. Nothing carried is object id `$A2`.

Game 1 layout (room, x, y):

- Black dot: `$15`, `$51`, `$12`
- Red dragon: `$0E`, `$50`, `$20`, state normal
- Yellow dragon: `$01`, `$50`, `$20`
- Green dragon: `$1D`, `$50`, `$20`
- Magnet: `$1B`, `$80`, `$20`
- Sword: `$12`, `$20`, `$20`
- Chalice: `$1C`, `$30`, `$20`
- Bridge: `$04`, `$29`, `$37`
- Yellow key: `$11`, `$20`, `$40`
- White key: `$0E`, `$20`, `$40`
- Black key: `$1D`, `$20`, `$40`
- Portcullises: state `$1C` (closed), in rooms `$11`, `$0F`, `$10` at (`$4D`, `$31`)
- Bat: `$1A`, `$20`, `$20`, movement `$00`, flap `$00`. Carrying `$78`, which is not an object, and fed-up `$00`, so it is already hunting.

Games 2 and 3 share a second layout (red dragon `$14`, yellow `$19`, green `$04`, magnet `$0E`, sword `$11`, chalice `$14`, bridge `$0B`, yellow key `$09`, white key `$06`, black key `$19`, bat `$02`). Game 3 then scatters eleven objects into a random room. The dot and the portcullises stay put, and the scattered objects keep the game 2 coordinates.

From `RandomizeLevel3`. The seed is the frame count in `$E5`, which increments once per three-field loop while the game is active. Each attempt stores `seed = (seed >> 5) + seed + 1` as a byte, and the room is the low 5 bits. A room below the low bound or above the high bound is thrown away and the seed runs again. The bounds are inclusive. The objects are taken from the end of the table: magnet, bat, black key, white key, yellow key, bridge, sword, green dragon, yellow dragon, red dragon, chalice. The chalice may only land in `$13`–`$1A`. The white key may only land in `$01`–`$16`. The black key may only land in `$01`–`$12`. The other eight may land in `$01`–`$1D`.

Object ids, from the object table, step by `$09`. The portcullis positions are `PortInfo1` through `PortInfo3`, not the game layout tables.

| Id | Object | Game 1 room |
| --- | --- | --- |
| `$09` | Yellow portcullis | `$11` at (`$4D`, `$31`), state `$1C` |
| `$12` | White portcullis | `$0F` at (`$4D`, `$31`), state `$1C` |
| `$1B` | Black portcullis | `$10` at (`$4D`, `$31`), state `$1C` |
| `$36` | Red dragon | `$0E` |
| `$3F` | Yellow dragon | `$01` |
| `$48` | Green dragon | `$1D` |
| `$51` | Sword | `$12` |
| `$5A` | Bridge | `$04` |
| `$63` | Yellow key | `$11` |
| `$6C` | White key | `$0E` |
| `$75` | Black key | `$1D` |
| `$7E` | Bat | `$1A` |
| `$87` | Black dot | `$15` |
| `$90` | Chalice | `$1C` |
| `$99` | Magnet | `$1B` |
| `$A2` | Nothing carried | — |

## Player

From `BallMovement`, `NoCollision`, `PlayerCollision`, and `ReadStick`.

The ball is moved on every field, by 3 pixels. The joystick is read only on the first field of the loop (`Y` is 0). A field that finds no ball collision stores `$FF` in the stick byte and snapshots the room and the ball position. The first field then replaces that `$FF` with `SWCHA`. The second and third fields do not read the stick, so in open space they keep `$FF` and the ball stays put. A held direction steps 3 pixels once per three fields, on both axes when the stick asks for both. `ReadStick_3` is `$00`, `$C0`, `$30`: the second field clears horizontal, and the third clears vertical.

A collision skips that snapshot and leaves the previous stick byte alone. The field restores the snapshot, then steps 3 with the axis mask. That is the slide along a wall. The collision is the ball against the playfield, a thin wall, or a sprite other than the surround. The right thin wall does not count when the black dot is the sprite in that slot. On the third field the bridge opening takes the no-collision path instead, so the snapshot updates and the masked step does not run.

The C# game does not enter a wall and has no sprite collision bits. `MovePlayer(Direction)` snapshots the position when the previous step was clear, reads the direction on the first field, and steps up to 3 pixels, stopping at a wall, a thin wall, or a closed gate. A stopped step is the collision. The later fields restore that snapshot and retry the saved direction with one axis removed. Letting go of the key on those fields does not change the saved direction. Overlap with a carried or roaming object is not a collision. `MovePlayer(Direction, int)` steps the requested pixels immediately and does not use the three-field stick.

## Pickup and drop

From `PickupPutdown`, `CollisionDetected`, `PickupObject`, and `MoveCarriedObject`.

An object is carriable when its id is `$51` or higher and it is not `$A2`. That is the sword, bridge, three keys, bat, black dot, chalice, and magnet. Dragons and portcullises are below `$51` and are not picked up.

The cartridge samples the trigger once per three-frame loop and remembers the current and previous bits. A drop happens only on the press edge (down now, up on the previous sample), and only when something is carried. The drop sets the carried id to `$A2` and leaves the object where it is. Noise type 4 is the drop.

That same pass then looks at the ball-to-sprite collision bits (`CXP0FB` and `CXP1FB` bit 6) for the two sprites just drawn. The C# game has no collision registers and draws every object, so a touch is the player's ball overlapping a lit pixel of that object's picture, in the player's room. The ball covers world x from `playerX - 2` through `playerX + 1` and scanlines `playerY - 4` through `playerY - 1`. `PrintDisplay` subtracts 4 from the ball's Y before enabling it for four scanlines, so the coordinate is the middle and the ball hangs below it. Row `r`, bit `b` is scanline `objectY - 1 - r` and covers x from `objectX + b * scale` for `scale` pixels. The first byte is the scanline just under the object's Y. Scale is 4 for the bridge and 1 for every other picture, including the sword, keys, bat, dot, chalice, magnet, dragons, and portcullises. Empty pixels are not a touch. The bridge's hole is empty, so standing in the opening does not pick it up, and a contact on any side of either rail does. The pass acts on the first touched carriable object in id order. Touching is enough to pick it up. The button is not required.

If that object is already carried, the pass keeps it and refreshes the offset. If it is a different object, it becomes the carried object and the previous one stays where it was. Noise type 5 is a new pickup. A press edge that drops an object still overlapping the player picks that object up again in the same pass, so the drop does not stick until the player and the object are apart.

The offset is the object's x and y minus the player's. `MoveCarriedObject` keeps the object in the player's room at the player position plus that offset, including across an exit.

`PickupObject` calls `MoveObjectDelta` with delta 6 and the held joystick byte before it stores the offset. The joystick is active-low; a held direction shifts the object 6 pixels that way. The disassembly marks the call as unknown. The C# applies that 6-pixel shift (`GameWorld.PickupNudge`) on the pickup pass that follows the all-axes movement step, once per three frames. A left shift of the sword still leaves its full-width row on the ball, so a press drops it and the same pass picks it up again. A shift up clears the ball, and the next press can leave it dropped. The same left shift often leaves the bridge's rails overlapping the player, so the next press picks it up again until the player leaves the shape.

## Keys and gates

From `Portals` and the portcullis check at the start of `MoveGroundObject`.

`Portals` runs once per three-field loop, on the vertical field, after the bat and before the green dragon. That call is the gate pass. The shell runs `ResolveGates` on that field.

Yellow key `$63` opens portcullis `$09` (castle `$11`, entry `$12`). White key `$6C` opens `$12` (castle `$0F`, entry `$1A`). Black key `$75` opens `$1B` (castle `$10`, entry `$1B`). State `$1C` is closed. State `$01` is open. Any other state advances by one on each gate pass until `$38`, which becomes `$01`.

A closed gate blocks when the player's ball overlaps a lit pixel of the gate's picture, at the position the player is about to step into. The matching key, carried or lying in the room, advances `$1C` to `$1D` when a lit pixel of the key overlaps a lit pixel of the gate. Another object, such as a dragon, does not, even when its picture overlaps the gate. The picture is `GfxPort01` and the bytes that follow it, through the zero byte at `GfxPort09`. The state picks the first portcullis threshold that is at least the state, and the picture starts there, so the gate shortens as it opens. State `$01` is below the first threshold `$04`, so it draws `GfxPort07`: four rows, then the zero. That stub is the open gate. The ball at the castle interior (`$50`, `$2C`) is just below it, and walking up into the bars leaves.

While the gate is not closed, the same pass does two different transfers:

- Touching the gate inside its castle puts the player there, or the object that touched it, in the entry room at y `$10` and sets the gate to `$01`. The player's touch is the ball against the gate's picture. Another object's touch is its lit pixels against the gate's. The player's x is unchanged.
- An object in the entry room and below y `$0D` is placed in the castle at (`$50`, `$2C`), and the gate is set to `$01`. This is checked before the normal bottom-edge wrap, so the entry does not wrap to its down exit.

## Bridge

From `PlayerCollision`. The bridge is object `$5A`.

Holding the bridge does not open a wall. On a collision, carrying `$5A` skips the opening test and the wall wins. Drop the bridge so its opening sits on the wall, then walk through. The opening is used only when the bridge is in the same room and it is not the carried object:

- `playerX - bridgeX` is `$0A` through `$16`
- `bridgeY - playerY` is `$00` through `$18`, or `$FC` through `$FF` (the player is from 24 pixels below the bridge to 4 above it)

That suppresses playfield walls and the thin bars. A closed gate still blocks. The view draws `GfxBridge` on the scanlines under the object's Y: byte 0 is scanline `bridgeY - 1`. It is 8 bits by 24 rows, four pixels per bit, bit 7 at the left. `$C3` is `XX    XX` and `$42` is ` X    X`, so the hole is the middle and it sits on the same side of the coordinate as the opening.

## Dragons

From `MoveRedDragon`, `MoveYellowDragon`, `MoveGreenDragon`, `MoveDragon`, `GetLinkedObject`, and `DragonDiff`.

The red dragon steps on the horizontal field, by 3. The green dragon, then the yellow dragon, step on the vertical field, by 2. A step does not test walls. The coordinate is a byte. After the step, the first edge that fits changes the room:

- Up: y ≥ `$6A`, new y `$0D`
- Left: x < `$03` or x ≥ `$F0`, new x `$9A` (the player's left wrap is `$9E`)
- Down: y < `$0D`, new y `$69`
- Right: x ≥ `$9B`, new x `$03` (the player's right edge is `$9F`)

There is no secret-room check. An open castle entry is still the gate pass, not this step.

State `$00` chases. The dragon keeps its movement byte and steps by it. A listed pair that shares a room replaces that byte. The byte is active-low: bit 7 clear is right, bit 6 clear is left, bit 4 clear is up, bit 5 clear is down. The direction is the first object's direction toward the second, and the dragon is the object that steps. The dragon runs away when it is the second object, and toward the second when it is the first. The first matching pair wins. No match leaves the old byte.

The right difficulty switch is P1, bit 7 of `SWCHB`. Amateur skips a pair that contains the sword. Pro runs from the sword. `GameWorld.DragonsFearSword` is that pro setting. It defaults to amateur.

Red, in order: sword (flee), player (chase), chalice (chase), white key (chase). Yellow: sword (flee), yellow key (flee), player (chase), chalice (chase). Green: sword (flee), player (chase), chalice (chase), bridge (chase), magnet (chase), black key (chase).

Still in state `$00`, a touch of the player after the step starts the roar and puts the dragon on the player's position. The touch is the player's ball overlapping a lit pixel of the dragon's picture. Nothing flips that picture, so both sides of the drawn shape count. The roar state is `DragonDiff`, indexed by the level (`$00`, `$02`, `$04`) plus the left difficulty switch (P0, bit 6). Game 1 is `$D0` amateur and `$E8` pro. Games 2 and 3 are `$F0` amateur and `$F6` pro. `GameWorld.ShortDragonRoar` is that pro column. It defaults to amateur.

The same pass then checks the sword. A sword touch is a lit pixel of the sword overlapping a lit pixel of the dragon. The cartridge reads that from the two sprites just drawn. This game draws every object, so the test uses those pictures. A sword touch sets state `$01` and overwrites a roar that started on this pass. State `$01` does not move.

Any other state increments by one on the dragon's turn and wraps from `$FF` to `$00`. The turn that wraps does not chase. While the state is `$FC` or higher and the player still touches the dragon, the state becomes `$02`. X above `$9B` becomes `$9B`. Y at or below `$17` becomes `$17`. That turn does not move the player. State `$02` does not move the dragon. Each later turn of that dragon places the player in the dragon's room at x + `$03` and y − `$0A`, as bytes.

## Bat

From `MoveBat` and `BatMatrix`. The bat steps on the vertical field, by 3, before the green and yellow dragons. It does not test walls. A step uses the same room-edge wrap as a dragon.

Each turn advances the flap. It runs `$00` through `$07` and then returns to `$00`. Flap `$00` through `$03` uses `GfxBat1`. Flap `$04` through `$07` uses `GfxBat2`.

Fed-up `$00` means the bat is hunting. That is how it starts. A listed pair that shares its room sets the movement byte, the same active-low byte the dragons use. The bat is the first object of every pair, so it flies toward the second. The first match wins. The object it is already carrying is skipped. The list is the chalice, sword, bridge, yellow key, white key, black key, red dragon, yellow dragon, green dragon, and magnet. No match leaves the old byte. Game 1's byte starts at `$00`, which steps nowhere. Game 2's byte starts at `$90`, left and down.

After the step, the quarry is picked up when its x and its y are each within the bat's reach. The object's coordinate minus the bat's, plus 4, must keep only its low three bits. That is a difference of `-4` through `+3`. A pickup stores that object and sets fed-up to `$10`.

Any other fed-up value increments, wrapping at `$FF`, and the bat steps by the byte it already has. It does not hunt. The carried object, when there is one, is placed in the bat's room at x + `$08` and the bat's y. `$78` is the starting carry and is not an object, so nothing is moved until the first pickup. If the player is carrying that same object, the player drops it. The bat keeps holding the object while it hunts for the next one, and a new pickup replaces it.

## Magnet

From `Mag_1` and `MagnetMatrix`. The magnet runs on the horizontal field, after the red dragon. It does not move. Its Y is lowered by `$08` for the check and then restored. That point is the pole.

The first listed object in the magnet's room is pulled one pixel toward the pole. The direction is the same active-low byte as a dragon, and the step uses the same room-edge wrap. The object's own movement byte is left alone. No match pulls nothing. The list is the yellow key, white key, black key, sword, bridge, and chalice. The player, the dragons, the bat, and the dot are not pulled.

## Pictures

From `PrintDisplay` and `ChangeColor`. The view is 97 scanlines, `$68` at the top through `$08` at the bottom. The screen row of a scanline is `$68` minus that scanline. Playfield row 0 is scanlines `$68` through `$61` (8 lines). A new row is stored every 16 scanlines after that, so rows 1 through 5 are 16 lines and row 6 is `$10` through `$08`. Walking into a wall is the ball against the cell that is drawn. The ball is one column wide, at the coordinate, and covers scanlines `playerY - 4` through `playerY - 1`. A scanline outside `$68` through `$08` is not a wall.

The bridge and the surround are four pixels per bit. That is the quad size in the object table. Every other picture is a single-width sprite, one pixel per bit. Byte `r` is scanline `objectY - 1 - r`. A dragon in state `$00` or `$02` uses `GfxDrag0`. State `$01` uses `GfxDrag2`. Any other state, including the roar, uses `GfxDrag1`. The sword, keys, dot, chalice, and magnet each have one picture. The number room shows `GfxNum1`, `GfxNum2`, or `GfxNum3` for games 1, 2, and 3. That sprite is one pixel per bit, and the lit digits are centered on the screen. The secret room shows `GfxAuthor` at (`$50`, `$69`), so its first byte is scanline `$68`.

`ChangeColor` runs on an object's color and on the author name. A color with bit 0 clear is kept while the high input count `$E6` is below `$80`. A color with bit 0 set is replaced by the zero-page byte at `$80` plus the color shifted right once. `$CB`, the chalice and the author, indexes `$E5`, the frame count (`GameWorld.FrameSeed`). The displayed byte is that value shifted left once. When `$E6` is `$80` or more, the value is merged with `$E6` and masked with `AND #$FB` before the shift. `CheckInput` increments `$E5` once per three-field loop while the game is active, including the number room, and once per field during the win fanfare. When `$E5` wraps, `$E6` increments, and a wrap of `$E6` stores `$80`. A held direction, reset, or select stores `$E6` as 0. The chalice and the author name therefore take a new color every three fields during play and every field during the fanfare. The shell calls `CountFrame` on those same fields. Room colors are even, and the walls are not passed through this routine.

## Sounds

From `MakeSound`. While the game is active it runs once per three-field loop, before movement. After a win, `NonActiveLoop` runs it once per field. The count in `$DF` decrements first. The formulas below use that new count. `$E0` is the type. A new event overwrites both. When the count is already 0, both volumes are cleared and the routine returns. Channel 1 is only silenced. The picture does not play a sample. The game holds the three channel-0 registers until the next call and a television-rate oscillator turns them into sound. The chip uses the low 4 bits of control and of volume, and the low 5 bits of frequency.

- **0, game over.** The win check stores count `$FF` and type 0. Control is the count. Volume is the count shifted right once. Frequency is the count shifted right three times. The same count is stored as the playfield color, so the walls use it while the fanfare runs.
- **1, roar.** An odd count uses control `$03`. An even count uses `$08`. Volume is the count. Frequency is the count shifted right twice, plus `$1C`. A bite stores count `$10`.
- **2, eaten.** Control `$06`. Frequency is the count XOR `$0F`. Volume is the count shifted right once, plus `$08`. The swallow stores count `$10`.
- **3, dragon dies.** Control `$04`. Volume is the count. Frequency is the count XOR `$1F`. The sword stores count `$10`.
- **4, drop.** Control `$06`, volume `$05`, frequency the count XOR `$03`. A drop stores count `$04`.
- **5, pickup.** Control `$06`, volume `$05`, frequency the count itself. A new pickup stores count `$04`. Refreshing an object that is already carried does not. A drop that grabs the same object again overwrites type 4 with type 5.
