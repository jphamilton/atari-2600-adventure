using Adventure.Core;
using Xunit;

namespace Adventure.Core.Tests;

public class SurroundTests
{
    [Fact]
    public void AVisibleRoomClearsTheSurround()
    {
        var world = new GameWorld();

        VerticalField(world);

        Assert.False(world.HasSurround);
    }

    [Fact]
    public void AnInvisibleRoomPlacesTheSurroundBesideThePlayer()
    {
        var world = new GameWorld();
        world.Player.Room = 0x09;
        world.Player.X = 0x50;
        world.Player.Y = 0x20;

        VerticalField(world);

        Assert.True(world.HasSurround);
        Assert.Equal(0x42, world.SurroundX);
        Assert.Equal(0x2E, world.SurroundY);
    }

    [Fact]
    public void TheSurroundClampsToEitherSideOfTheScreen()
    {
        var world = new GameWorld();
        world.Player.Room = 0x09;
        world.Player.Y = 0x20;
        world.Player.X = 0x90;

        VerticalField(world);

        Assert.Equal(0x81, world.SurroundX);

        world.Player.X = 0x05;
        world.MovePlayer(Direction.None);
        world.MovePlayer(Direction.None);
        world.MovePlayer(Direction.None);

        Assert.Equal(0x01, world.SurroundX);
    }

    private static void VerticalField(GameWorld world)
    {
        world.MovePlayer(Direction.None);
        world.MovePlayer(Direction.None);
    }
}
