# Adventure

A C# port of Warren Robinett's *Adventure* for the Atari 2600. 
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