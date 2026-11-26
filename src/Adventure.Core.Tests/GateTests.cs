using Adventure.Core;
using Xunit;

namespace Adventure.Core.Tests;

public class GateTests
{
    [Fact]
    public void YellowKeyStartsTheOpenSequenceAndADragonDoesNot()
    {
        var locked = new GameWorld();
        var gate = Item(locked, ObjectIds.YellowPortcullis);
        var dragon = Item(locked, ObjectIds.RedDragon);
        dragon.Room = gate.Room;
        dragon.X = gate.X;
        dragon.Y = gate.Y;

        locked.ResolveGates();

        Assert.Equal(GameWorld.GateClosed, gate.State);
        Assert.Equal(gate.Room, dragon.Room);
        Assert.Equal(gate.X, dragon.X);
        Assert.Equal(gate.Y, dragon.Y);

        var world = new GameWorld();
        world.Player.Y = 0x10;
        gate = Item(world, ObjectIds.YellowPortcullis);
        var key = Item(world, ObjectIds.YellowKey);
        key.Room = gate.Room;
        key.X = gate.X;
        key.Y = gate.Y;

        world.ResolveGates();

        Assert.Equal(0x1E, gate.State);
        Assert.Equal(0x12, key.Room);
        Assert.Equal(gate.X, key.X);
        Assert.Equal(GameWorld.GateEntryY, key.Y);
    }

    [Fact]
    public void ClosedGateBlocksThePlayer()
    {
        var world = new GameWorld();
        var gate = Item(world, ObjectIds.YellowPortcullis);
        gate.Room = world.Player.Room;
        gate.X = world.Player.X + 2;
        gate.Y = world.Player.Y;
        var x = world.Player.X;

        world.MovePlayer(Direction.Right, pixels: 1);

        Assert.Equal(x, world.Player.X);
        Assert.Equal(GameWorld.GateClosed, gate.State);
        Assert.False(world.Blocks(world.Player.Room, x + 1, world.Player.Y));
    }

    [Fact]
    public void OpenGateLetsThePlayerLeaveAndReenterTheYellowCastle()
    {
        var world = new GameWorld();
        var gate = Item(world, ObjectIds.YellowPortcullis);
        gate.State = GameWorld.GateOpen;
        world.Player.Room = gate.Room;
        world.Player.X = gate.X;
        world.Player.Y = gate.Y;

        world.ResolveGates();

        Assert.Equal(0x12, world.Player.Room);
        Assert.Equal(gate.X, world.Player.X);
        Assert.Equal(GameWorld.GateEntryY, world.Player.Y);
        Assert.Equal(GameWorld.GateOpen, gate.State);

        world.Player.X = 0x50;
        world.Player.Y = MapGeometry.BottomEdge;
        world.MovePlayer(Direction.Down, pixels: 1);

        Assert.Equal(0x11, world.Player.Room);
        Assert.Equal(GameWorld.GateInteriorX, world.Player.X);
        Assert.Equal(GameWorld.GateInteriorY, world.Player.Y);
        Assert.Equal(GameWorld.GateOpen, gate.State);
    }

    [Fact]
    public void WalkingUpIntoTheOpenBlackGateLeavesTheCastle()
    {
        var world = new GameWorld();
        var gate = Item(world, ObjectIds.BlackPortcullis);
        gate.State = GameWorld.GateOpen;
        world.Player.Room = gate.Room;
        world.Player.X = GameWorld.GateInteriorX;
        world.Player.Y = GameWorld.GateInteriorY;

        world.ResolveGates();

        Assert.Equal(gate.Room, world.Player.Room);

        for (var i = 0; i < 8 && world.Player.Room == gate.Room; i++)
        {
            world.MovePlayer(Direction.Up, pixels: 1);
            world.ResolveGates();
        }

        Assert.Equal(0x1B, world.Player.Room);
        Assert.Equal(GameWorld.GateEntryY, world.Player.Y);
        Assert.Equal(GameWorld.GateOpen, gate.State);
    }

    private static WorldObject Item(GameWorld world, int id) =>
        Assert.Single(world.Objects, item => item.Id == id);
}
