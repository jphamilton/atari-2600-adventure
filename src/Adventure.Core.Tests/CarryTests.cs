using Adventure.Core;
using Xunit;

namespace Adventure.Core.Tests;

public class CarryTests
{
    [Fact]
    public void Game1PlacesTheSwordDotAndPortcullises()
    {
        var world = new GameWorld();

        var sword = Object(world, ObjectIds.Sword);
        Assert.Equal(0x12, sword.Room);
        Assert.Equal(0x20, sword.X);
        Assert.Equal(0x20, sword.Y);

        var dot = Object(world, ObjectIds.Dot);
        Assert.Equal(0x15, dot.Room);
        Assert.Equal(0x51, dot.X);
        Assert.Equal(0x12, dot.Y);

        var gate = Object(world, ObjectIds.YellowPortcullis);
        Assert.Equal(RoomCatalog.YellowCastle, gate.Room);
        Assert.Equal(0x4D, gate.X);
        Assert.Equal(0x31, gate.Y);
        Assert.Equal(0x1C, gate.State);
        Assert.False(gate.IsCarriable);
    }

    [Fact]
    public void Game2PlacesTheSwordInTheYellowCastle()
    {
        var world = new GameWorld(gameIndex: 1);
        var sword = Object(world, ObjectIds.Sword);

        Assert.Equal(RoomCatalog.YellowCastle, sword.Room);
        Assert.Equal(0x20, sword.X);
        Assert.Equal(0x20, sword.Y);
    }

    [Fact]
    public void TouchingTheSwordPicksItUpAndItFollows()
    {
        var world = new GameWorld();
        var sword = PutOnPlayer(world, ObjectIds.Sword);

        world.ResolveCarry(buttonDown: false);

        Assert.Same(sword, world.Carried);
        world.MovePlayer(Direction.Right, pixels: 1);

        Assert.Equal(RoomCatalog.YellowCastle, sword.Room);
        Assert.Equal(world.Player.X, sword.X);
        Assert.Equal(world.Player.Y, sword.Y);
    }

    [Fact]
    public void TouchingADragonOrGateDoesNotPickItUp()
    {
        var world = new GameWorld();
        PutOnPlayer(world, ObjectIds.RedDragon);

        world.ResolveCarry(buttonDown: false);

        Assert.Null(world.Carried);

        var gate = Object(world, ObjectIds.YellowPortcullis);
        world.Player.Room = gate.Room;
        world.Player.X = gate.X;
        world.Player.Y = gate.Y;
        world.ResolveCarry(buttonDown: true);

        Assert.Null(world.Carried);
    }

    [Fact]
    public void PressingWhileStillTouchingPicksTheObjectBackUp()
    {
        var world = new GameWorld();
        var sword = PutOnPlayer(world, ObjectIds.Sword);
        world.ResolveCarry(buttonDown: false);

        world.ResolveCarry(buttonDown: true);

        Assert.Same(sword, world.Carried);
        Assert.Equal(world.Player.X, sword.X);
        Assert.Equal(world.Player.Y, sword.Y);
    }

    [Fact]
    public void PressingAfterTheObjectIsOutOfReachDropsIt()
    {
        var world = new GameWorld();
        var sword = PutOnPlayer(world, ObjectIds.Sword);
        world.ResolveCarry(buttonDown: false);

        sword.X += 4;
        world.ResolveCarry(buttonDown: true);

        Assert.Null(world.Carried);
        Assert.Equal(GameWorld.StartX + 4, sword.X);
        Assert.Equal(GameWorld.StartY, sword.Y);
        Assert.Equal(RoomCatalog.YellowCastle, world.Player.Room);
        Assert.Equal(GameWorld.StartX, world.Player.X);
    }

    [Fact]
    public void HoldingADirectionOnThePickupPassNudgesTheObject()
    {
        var world = new GameWorld();
        for (var i = 0; i < 80; i++)
        {
            world.MovePlayer(Direction.Left, pixels: 1);
        }

        var sword = PutOnPlayer(world, ObjectIds.Sword);
        var x = world.Player.X;
        world.MovePlayer(Direction.Left);
        world.ResolveCarry(buttonDown: false);

        Assert.Same(sword, world.Carried);
        Assert.Equal(x, world.Player.X);
        Assert.Equal(x - GameWorld.PickupNudge, sword.X);
        Assert.True(world.Touches(sword));

        world.ResolveCarry(buttonDown: true);

        Assert.Same(sword, world.Carried);
        Assert.Equal(x - GameWorld.PickupNudge * 2, sword.X);
    }

    [Fact]
    public void ACarriedBatStaysWithAStationaryPlayer()
    {
        var world = new GameWorld(gameIndex: 1);
        Object(world, ObjectIds.Sword).Room = 0x01;
        var bat = PutOnPlayer(world, ObjectIds.Bat);
        world.ResolveCarry(buttonDown: false);

        for (var i = 0; i < 31; i++)
        {
            world.MovePlayer(Direction.None);
            world.ResolveCarry(buttonDown: false);

            Assert.Same(bat, world.Carried);
            Assert.Equal(world.Player.Room, bat.Room);
            Assert.Equal(world.Player.X, bat.X);
            Assert.Equal(world.Player.Y, bat.Y);
        }
    }

    [Fact]
    public void ACarriedBridgeDoesNotDriftTowardTheMagnetWhileStanding()
    {
        var world = new GameWorld(gameIndex: 1);
        var magnet = Object(world, ObjectIds.Magnet);
        var bridge = Object(world, ObjectIds.Bridge);
        world.Player.Room = magnet.Room;
        world.Player.X = 0x40;
        world.Player.Y = magnet.Y;
        bridge.Room = magnet.Room;
        bridge.X = world.Player.X;
        bridge.Y = world.Player.Y;
        world.ResolveCarry(buttonDown: false);

        for (var i = 0; i < 31; i++)
        {
            world.MovePlayer(Direction.None);
            world.ResolveCarry(buttonDown: false);

            Assert.Same(bridge, world.Carried);
            Assert.Equal(world.Player.Room, bridge.Room);
            Assert.Equal(world.Player.X, bridge.X);
            Assert.Equal(world.Player.Y, bridge.Y);
        }
    }

    [Fact]
    public void AKeyHeldBelowTheCastleExitDropsIntoTheNextRoom()
    {
        var world = new GameWorld();
        var key = Object(world, ObjectIds.YellowKey);
        world.Player.Room = RoomCatalog.YellowCastle;
        world.Player.X = 0x50;
        world.Player.Y = 0x0E;
        key.Room = world.Player.Room;
        key.X = world.Player.X;
        key.Y = 0x0C;
        world.ResolveCarry(buttonDown: false);

        world.MovePlayer(Direction.None);

        Assert.Same(key, world.Carried);
        Assert.Equal(RoomCatalog.CorridorUnderYellowCastle, key.Room);
        Assert.Equal(MapGeometry.WrappedFromBottomY, key.Y);
        Assert.Equal(0x50, key.X);

        world.ResolveCarry(buttonDown: true);

        Assert.Null(world.Carried);
        Assert.Equal(RoomCatalog.CorridorUnderYellowCastle, key.Room);
        Assert.Equal(MapGeometry.WrappedFromBottomY, key.Y);
        Assert.Equal(0x50, key.X);
    }

    [Fact]
    public void ALitPixelAwayFromTheCoordinatePicksUpTheChalice()
    {
        var world = new GameWorld();
        var chalice = Object(world, ObjectIds.Chalice);
        chalice.Room = world.Player.Room;
        chalice.X = 0x40;
        chalice.Y = 0x30;
        world.Player.X = 0x41;
        world.Player.Y = 0x2C;

        world.ResolveCarry(buttonDown: false);

        Assert.Same(chalice, world.Carried);
    }

    [Fact]
    public void TheEmptySideOfTheChaliceDoesNotPickItUp()
    {
        var world = new GameWorld();
        var chalice = Object(world, ObjectIds.Chalice);
        chalice.Room = world.Player.Room;
        chalice.X = 0x40;
        chalice.Y = 0x30;
        world.Player.X = 0x3C;
        world.Player.Y = 0x30;

        world.ResolveCarry(buttonDown: false);

        Assert.Null(world.Carried);
    }

    private static WorldObject Object(GameWorld world, int id) =>
        Assert.Single(world.Objects, item => item.Id == id);

    private static WorldObject PutOnPlayer(GameWorld world, int id)
    {
        var item = Object(world, id);
        item.Room = world.Player.Room;
        item.X = world.Player.X;
        item.Y = world.Player.Y;
        return item;
    }
}
