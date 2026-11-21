using Adventure.Core;
using Xunit;

namespace Adventure.Core.Tests;

public class BatTests
{
    [Fact]
    public void TheBatHuntsTheChaliceAheadOfTheSword()
    {
        var world = new GameWorld();
        var bat = Place(world, ObjectIds.Bat, 0x40, 0x20);
        var chalice = Place(world, ObjectIds.Chalice, 0x50, 0x20, bat.Room);
        Place(world, ObjectIds.Sword, 0x30, 0x20, bat.Room);

        BatTurns(world, 1);

        Assert.Equal(0x43, bat.X);
        Assert.Equal(chalice.Room, bat.Room);
        Assert.Equal(1, bat.State);
    }

    [Fact]
    public void TheBatPicksUpAnObjectWithinReachAndStealsItFromThePlayer()
    {
        var world = new GameWorld();
        var bat = Place(world, ObjectIds.Bat, 0x40, 0x20);
        var chalice = Place(world, ObjectIds.Chalice, 0x43, 0x20, bat.Room);
        world.Player.Room = bat.Room;
        world.Player.X = 0x44;
        world.Player.Y = 0x18;
        world.ResolveCarry(buttonDown: false);
        Assert.Same(chalice, world.Carried);

        BatTurns(world, 1);

        Assert.Equal(ObjectIds.Chalice, world.BatCargo);
        Assert.Equal(0x10, world.BatFedUp);
        Assert.Null(world.Carried);
        Assert.Equal(bat.Room, chalice.Room);
        Assert.Equal((bat.X + 0x08) & 0xFF, chalice.X);
        Assert.Equal(bat.Y, chalice.Y);
    }

    [Fact]
    public void AnObjectStillFourPixelsAheadIsNotPickedUp()
    {
        var world = new GameWorld();
        var bat = Place(world, ObjectIds.Bat, 0x40, 0x20);
        Place(world, ObjectIds.Chalice, 0x47, 0x20, bat.Room);

        BatTurns(world, 1);

        Assert.Equal(0x43, bat.X);
        Assert.Equal(ObjectIds.None, world.BatCargo);
        Assert.Equal(0, world.BatFedUp);
    }

    [Fact]
    public void TheBatDoesNotPickUpTheDot()
    {
        var world = new GameWorld();
        var bat = Place(world, ObjectIds.Bat, 0x40, 0x20);
        var dot = Place(world, ObjectIds.Dot, 0x40, 0x20, bat.Room);

        BatTurns(world, 1);

        Assert.Equal(ObjectIds.None, world.BatCargo);
        Assert.Equal(0x40, dot.X);
        Assert.Equal(0x40, bat.X);
    }

    [Fact]
    public void WhileCarryingTheBatKeepsItsHeadingAndTheObject()
    {
        var world = new GameWorld();
        var bat = Place(world, ObjectIds.Bat, 0x40, 0x20);
        var sword = Place(world, ObjectIds.Sword, 0x30, 0x20, bat.Room);
        bat.Movement = 0x7F;
        world.BatFedUp = 0x10;
        world.BatCargo = sword.Id;

        BatTurns(world, 1);

        Assert.Equal(0x43, bat.X);
        Assert.Equal(0x20, bat.Y);
        Assert.Equal(0x11, world.BatFedUp);
        Assert.Equal(ObjectIds.Sword, world.BatCargo);
        Assert.Equal((bat.X + 0x08) & 0xFF, sword.X);
        Assert.Equal(bat.Y, sword.Y);
    }

    [Fact]
    public void AFullFedUpCounterHuntsAgainOnTheNextTurn()
    {
        var world = new GameWorld();
        var bat = Place(world, ObjectIds.Bat, 0x40, 0x20);
        var sword = Place(world, ObjectIds.Sword, 0x80, 0x20, bat.Room);
        Place(world, ObjectIds.Chalice, 0x20, 0x20, bat.Room);
        bat.Movement = 0x7F;
        world.BatFedUp = 0xFF;
        world.BatCargo = sword.Id;

        BatTurns(world, 1);

        Assert.Equal(0, world.BatFedUp);
        Assert.Equal(0x43, bat.X);
        Assert.Equal(ObjectIds.Sword, world.BatCargo);

        BatTurns(world, 1);

        Assert.Equal(0x40, bat.X);
        Assert.Equal(ObjectIds.Sword, world.BatCargo);
    }

    [Fact]
    public void Game2BatDriftsLeftAndDownUntilItSeesAQuarry()
    {
        var world = new GameWorld(gameIndex: 1);
        var bat = Item(world, ObjectIds.Bat);

        BatTurns(world, 1);

        Assert.Equal(0x1D, bat.X);
        Assert.Equal(0x1D, bat.Y);
        Assert.Equal(ObjectIds.None, world.BatCargo);
    }

    [Fact]
    public void TheFlapReturnsToZeroAfterEightTurns()
    {
        var world = new GameWorld();
        var bat = Item(world, ObjectIds.Bat);

        BatTurns(world, 8);

        Assert.Equal(0, bat.State);
    }

    private static WorldObject Item(GameWorld world, int id) =>
        Assert.Single(world.Objects, item => item.Id == id);

    private static WorldObject Place(GameWorld world, int id, int x, int y, int? room = null)
    {
        var item = Item(world, id);
        item.X = x;
        item.Y = y;
        if (room is int destination)
        {
            item.Room = destination;
        }

        return item;
    }

    private static void BatTurns(GameWorld world, int turns)
    {
        for (var turn = 0; turn < turns; turn++)
        {
            for (var i = 0; i < 3; i++)
            {
                world.MovePlayer(Direction.None);
            }
        }
    }
}
