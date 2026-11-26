using Adventure.Core;
using Xunit;

namespace Adventure.Core.Tests;

public class ScenarioTests
{
    [Fact]
    public void CommentsAndDefaultsStartGameOneInTheYellowCastle()
    {
        var setup = Scenario.Parse(
            """
            {
              // start here
              "player": { "room": "yellow castle" },
            }
            """);

        Assert.Equal(0, setup.GameIndex);
        Assert.Equal(RoomCatalog.YellowCastle, setup.PlayerRoom);
        Assert.Equal(GameWorld.StartX, setup.PlayerX);
        Assert.Equal(GameWorld.StartY, setup.PlayerY);
        Assert.Null(setup.Carrying);
        Assert.False(setup.Ignore);
        Assert.Empty(setup.Objects);
    }

    [Fact]
    public void IgnoreStartsOnTheTitleScreen()
    {
        var world = Scenario.Create(
            """
            {
              "ignore": true,
              "game": 3,
              "player": { "room": "Black Castle" }
            }
            """);

        Assert.Equal(RoomCatalog.NumberRoom, world.Player.Room);
        Assert.Equal(0, world.Player.X);
        Assert.Equal(0, world.GameIndex);
    }

    [Fact]
    public void AScenarioPlacesThePlayerAndPatchesObjects()
    {
        var world = GameWorld.ForPlaytest(Scenario.Parse(
            """
            {
              "game": 2,
              "player": { "room": "Black Castle", "x": "0x40", "y": 48 },
              "carrying": "black_key",
              "dragons_fear_sword": true,
              "objects": {
                "yellow_dragon": { "state": "dead" },
                "bat": { "enabled": false },
                "black_portcullis": { "state": "open" },
                "chalice": { "room": "0x10" }
              }
            }
            """));

        Assert.Equal(1, world.GameIndex);
        Assert.Equal(0x10, world.Player.Room);
        Assert.Equal(0x40, world.Player.X);
        Assert.Equal(48, world.Player.Y);
        Assert.True(world.DragonsFearSword);
        Assert.Equal(ObjectIds.BlackKey, world.Carried?.Id);
        Assert.Equal(world.Player.Room, world.Carried?.Room);
        Assert.Equal(GameWorld.DragonDead, Item(world, ObjectIds.YellowDragon).State);
        Assert.Equal(GameWorld.GateOpen, Item(world, ObjectIds.BlackPortcullis).State);
        Assert.Equal(0x10, Item(world, ObjectIds.Chalice).Room);
        Assert.Equal(0, Item(world, ObjectIds.Bat).State);

        world.MovePlayer(Direction.None);
        world.MovePlayer(Direction.None);
        world.MovePlayer(Direction.None);

        Assert.Equal(0, Item(world, ObjectIds.Bat).State);
    }

    [Fact]
    public void ResetRestoresTheScenarioAfterObjectsMove()
    {
        var world = GameWorld.ForPlaytest(Scenario.Parse(
            """
            {
              "player": { "room": "White Castle" },
              "objects": { "red_dragon": { "room": "White Castle", "state": "dead" } }
            }
            """));
        world.Player.Room = RoomCatalog.YellowCastle;
        Item(world, ObjectIds.RedDragon).Room = 0x04;
        Item(world, ObjectIds.RedDragon).State = GameWorld.DragonChase;

        world.Reset();

        Assert.Equal(0x0F, world.Player.Room);
        Assert.Equal(0x0F, Item(world, ObjectIds.RedDragon).Room);
        Assert.Equal(GameWorld.DragonDead, Item(world, ObjectIds.RedDragon).State);
    }

    [Fact]
    public void AnAmbiguousRoomNameAsksForTheHexId()
    {
        var exception = Assert.Throws<FormatException>(() => Scenario.Parse(
            """
            { "player": { "room": "Side Corridor" } }
            """));

        Assert.Contains("0x0C", exception.Message, StringComparison.Ordinal);
        Assert.Contains("0x0D", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AnUnknownPropertyIsRejected()
    {
        var exception = Assert.Throws<FormatException>(() => Scenario.Parse(
            """
            { "no_bat": true }
            """));

        Assert.Contains("no_bat", exception.Message, StringComparison.Ordinal);
    }

    private static WorldObject Item(GameWorld world, int id) =>
        Assert.Single(world.Objects, item => item.Id == id);
}
