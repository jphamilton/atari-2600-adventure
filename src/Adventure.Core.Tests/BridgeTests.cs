using Adventure.Core;
using Xunit;

namespace Adventure.Core.Tests;

public class BridgeTests
{
    [Fact]
    public void CarryingTheBridgeDoesNotOpenAWall()
    {
        var world = new GameWorld();
        world.Player.Y = 0x18;
        var bridge = Item(world);
        bridge.Room = world.Player.Room;
        bridge.X = world.Player.X;
        bridge.Y = world.Player.Y;
        world.ResolveCarry(buttonDown: false);

        for (var i = 0; i < 80; i++)
        {
            world.MovePlayer(Direction.Left, pixels: 1);
        }

        var x = world.Player.X;
        Assert.True(world.Blocks(world.Player.Room, x - 1, world.Player.Y));
        world.MovePlayer(Direction.Left, pixels: 1);

        Assert.Same(bridge, world.Carried);
        Assert.Equal(x, world.Player.X);
    }

    [Fact]
    public void DroppedBridgeOpensTheWallInsideItsWindow()
    {
        var world = new GameWorld();
        var wallX = WalkToLeftWall(world);
        var bridge = Item(world);
        bridge.Room = world.Player.Room;
        bridge.X = wallX - 0x0A;
        bridge.Y = world.Player.Y;

        world.MovePlayer(Direction.Left, pixels: 1);

        Assert.Null(world.Carried);
        Assert.Equal(wallX, world.Player.X);
    }

    [Fact]
    public void DroppedBridgeDoesNotOpenAWallOutsideItsWindow()
    {
        var world = new GameWorld();
        var wallX = WalkToLeftWall(world);
        var x = world.Player.X;
        var bridge = Item(world);
        bridge.Room = world.Player.Room;
        bridge.X = wallX - 0x09;
        bridge.Y = world.Player.Y;

        world.MovePlayer(Direction.Left, pixels: 1);

        Assert.Equal(x, world.Player.X);
        Assert.True(world.Blocks(world.Player.Room, wallX, world.Player.Y));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(24, 0)]
    [InlineData(4, 8)]
    public void SolidBridgePixelsPickUpTheBridge(int xOffset, int yDown)
    {
        var world = new GameWorld();
        var bridge = Item(world);
        world.Player.Room = bridge.Room;
        world.Player.X = bridge.X + xOffset;
        world.Player.Y = bridge.Y - yDown;

        world.ResolveCarry(buttonDown: false);

        Assert.Same(bridge, world.Carried);
    }

    [Theory]
    [InlineData(0, 0, -1, 0)]
    [InlineData(0, 0, 5, 0)]
    [InlineData(0, 0, 1, 1)]
    [InlineData(0, 0, 1, 2)]
    [InlineData(24, 0, -1, 0)]
    [InlineData(24, 0, 5, 0)]
    [InlineData(24, 0, 1, 1)]
    [InlineData(24, 0, 1, 2)]
    [InlineData(4, 8, -1, 0)]
    [InlineData(4, 8, 5, 0)]
    [InlineData(4, 8, 1, 1)]
    [InlineData(4, 8, 1, 2)]
    public void BridgePixelsPickUpFromEverySide(int xOffset, int yDown, int playerXOffset, int playerYOffset)
    {
        var world = new GameWorld();
        var bridge = Item(world);
        var pixelX = bridge.X + xOffset;
        var pixelY = bridge.Y - yDown;
        world.Player.Room = bridge.Room;
        world.Player.X = pixelX + playerXOffset;
        world.Player.Y = pixelY + playerYOffset;

        world.ResolveCarry(buttonDown: false);

        Assert.Same(bridge, world.Carried);
        Assert.NotEqual(pixelX, world.Player.X);
        Assert.False(world.Player.X >= pixelX && world.Player.X < pixelX + ObjectSprites.BridgePixelScale
            && world.Player.Y == pixelY);
    }

    [Fact]
    public void StandingInTheBridgeHoleDoesNotPickItUp()
    {
        var world = new GameWorld();
        var bridge = Item(world);
        world.Player.Room = bridge.Room;
        world.Player.X = bridge.X + 0x10;
        world.Player.Y = bridge.Y - 0x0C;

        world.ResolveCarry(buttonDown: false);

        Assert.Null(world.Carried);
    }

    [Fact]
    public void InitialBridgeHoleCanBeCrossedInBothDirections()
    {
        var world = new GameWorld();
        var bridge = Item(world);
        world.Player.Room = bridge.Room;
        world.Player.X = bridge.X + 0x10;
        world.Player.Y = bridge.Y + 4;
        var top = world.Player.Y;
        var bottom = bridge.Y - 0x18;

        for (var i = 0; i < 40 && world.Player.Y > bottom; i++)
        {
            world.MovePlayer(Direction.Down, pixels: 1);
        }

        Assert.Equal(bottom, world.Player.Y);
        Assert.Equal(bridge.Room, world.Player.Room);

        for (var i = 0; i < 40 && world.Player.Y < top; i++)
        {
            world.MovePlayer(Direction.Up, pixels: 1);
        }

        Assert.Equal(top, world.Player.Y);
        Assert.Equal(bridge.X + 0x10, world.Player.X);
    }

    private static int WalkToLeftWall(GameWorld world)
    {
        world.Player.Y = 0x18;
        for (var i = 0; i < 80 && !world.Blocks(world.Player.Room, world.Player.X - 1, world.Player.Y); i++)
        {
            world.MovePlayer(Direction.Left, pixels: 1);
        }

        var wallX = world.Player.X - 1;
        Assert.True(world.Blocks(world.Player.Room, wallX, world.Player.Y));
        return wallX;
    }

    private static WorldObject Item(GameWorld world) =>
        Assert.Single(world.Objects, item => item.Id == ObjectIds.Bridge);
}
