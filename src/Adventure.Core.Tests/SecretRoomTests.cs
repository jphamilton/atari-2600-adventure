using Adventure.Core;
using Xunit;

namespace Adventure.Core.Tests;

public class SecretRoomTests
{
    [Fact]
    public void TheDotAtHomeKeepsTheNormalRightExit()
    {
        var world = new GameWorld();
        world.Player.Room = 0x03;
        world.Player.X = 0x9E;
        world.Player.Y = 0x20;

        world.MovePlayer(Direction.Right, 1);

        Assert.Equal(0x01, world.Player.Room);
        Assert.Equal(0x03, world.Player.X);
    }

    [Fact]
    public void MovingTheDotOpensTheSecretRoom()
    {
        var world = new GameWorld();
        var dot = Item(world, ObjectIds.Dot);
        world.Player.Room = 0x03;
        world.Player.X = 0x20;
        world.Player.Y = 0x20;

        world.MovePlayer(Direction.Right, 200);

        Assert.Equal(0x03, world.Player.Room);
        Assert.Equal(0x95, world.Player.X);

        dot.Room = 0x03;
        world.MovePlayer(Direction.Right, 200);

        Assert.Equal(0x1E, world.Player.Room);
        Assert.Equal(0x03, world.Player.X);
    }

    [Fact]
    public void ADotLeftBehindStillOpensTheEdgeOnceThePlayerIsPastTheBar()
    {
        var world = new GameWorld();
        Item(world, ObjectIds.Dot).Room = 0x00;
        world.Player.Room = 0x03;
        world.Player.X = 0x9E;
        world.Player.Y = 0x20;

        world.MovePlayer(Direction.Right, 1);

        Assert.Equal(0x1E, world.Player.Room);
        Assert.Equal(0x03, world.Player.X);
    }

    [Fact]
    public void ADragonDoesNotTakeTheSecretExit()
    {
        var world = new GameWorld();
        Item(world, ObjectIds.Dot).Room = 0x00;
        var dragon = Item(world, ObjectIds.RedDragon);
        dragon.Room = 0x03;
        dragon.X = 0x99;
        dragon.Y = 0x20;
        dragon.Movement = 0x7F;

        for (var i = 0; i < 3; i++)
        {
            world.MovePlayer(Direction.None);
        }

        Assert.Equal(0x01, dragon.Room);
        Assert.Equal(0x03, dragon.X);
    }

    private static WorldObject Item(GameWorld world, int id) =>
        world.Objects.First(item => item.Id == id);
}
