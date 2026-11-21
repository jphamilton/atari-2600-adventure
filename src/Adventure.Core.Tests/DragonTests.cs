using Adventure.Core;
using Xunit;

namespace Adventure.Core.Tests;

public class DragonTests
{
    [Fact]
    public void YellowDragonChasesThePlayerByTwo()
    {
        var world = new GameWorld();
        var dragon = PlaceWithPlayer(world, ObjectIds.YellowDragon, x: 0x40, y: 0x20);
        world.Player.X = 0x50;
        world.Player.Y = 0x30;

        YellowTurns(world, 1);

        Assert.Equal(0x42, dragon.X);
        Assert.Equal(0x22, dragon.Y);
        Assert.Equal(GameWorld.DragonChase, dragon.State);
    }

    [Fact]
    public void RedDragonChasesThePlayerByThree()
    {
        var world = new GameWorld();
        var dragon = PlaceWithPlayer(world, ObjectIds.RedDragon, x: 0x40, y: 0x20);
        world.Player.X = 0x50;

        RedTurns(world, 1);

        Assert.Equal(0x43, dragon.X);
        Assert.Equal(0x20, dragon.Y);
    }

    [Fact]
    public void AmateurDragonIgnoresTheSwordAndChases()
    {
        var world = new GameWorld();
        var dragon = PlaceWithPlayer(world, ObjectIds.YellowDragon, x: 0x50, y: 0x20);
        world.Player.X = 0x30;
        var sword = Item(world, ObjectIds.Sword);
        sword.Room = dragon.Room;
        sword.X = 0x40;
        sword.Y = 0x20;

        YellowTurns(world, 1);

        Assert.Equal(0x4E, dragon.X);
    }

    [Fact]
    public void ProDragonRunsFromTheSword()
    {
        var world = new GameWorld { DragonsFearSword = true };
        var dragon = PlaceWithPlayer(world, ObjectIds.YellowDragon, x: 0x50, y: 0x20);
        world.Player.X = 0x30;
        var sword = Item(world, ObjectIds.Sword);
        sword.Room = dragon.Room;
        sword.X = 0x40;
        sword.Y = 0x20;

        YellowTurns(world, 1);

        Assert.Equal(0x52, dragon.X);
    }

    [Fact]
    public void YellowDragonRunsFromItsKeyAheadOfThePlayer()
    {
        var world = new GameWorld();
        var dragon = PlaceWithPlayer(world, ObjectIds.YellowDragon, x: 0x50, y: 0x20);
        world.Player.X = 0x30;
        var key = Item(world, ObjectIds.YellowKey);
        key.Room = dragon.Room;
        key.X = 0x40;
        key.Y = 0x20;

        YellowTurns(world, 1);

        Assert.Equal(0x52, dragon.X);
    }

    [Fact]
    public void GreenDragonChasesTheBridgeWhenThePlayerIsAway()
    {
        var world = new GameWorld();
        var dragon = Item(world, ObjectIds.GreenDragon);
        dragon.X = 0x40;
        dragon.Y = 0x20;
        var bridge = Item(world, ObjectIds.Bridge);
        bridge.Room = dragon.Room;
        bridge.X = 0x50;
        bridge.Y = 0x20;

        YellowTurns(world, 1);

        Assert.Equal(0x42, dragon.X);
        Assert.Equal(dragon.Room, bridge.Room);
    }

    [Fact]
    public void ADragonKeepsItsDriftAndLeavesThroughTheRightEdge()
    {
        var world = new GameWorld();
        var dragon = Item(world, ObjectIds.RedDragon);
        var room = dragon.Room;
        dragon.Room = RoomCatalog.YellowCastle;
        dragon.X = 0x99;
        dragon.Y = 0x20;
        dragon.Movement = 0x7F;
        world.Player.Room = 0;
        var right = RoomCatalog.ResolveExit(RoomCatalog.Rooms[RoomCatalog.YellowCastle].Right, world.GameIndex);

        RedTurns(world, 1);

        Assert.Equal(MapGeometry.WrappedFromRightX, dragon.X);
        Assert.Equal(right, dragon.Room);
        Assert.NotEqual(room, dragon.Room);
    }

    [Fact]
    public void TouchingThePlayerStartsTheLongRoarAndSnapsTheDragon()
    {
        var world = new GameWorld();
        var dragon = PlaceWithPlayer(world, ObjectIds.YellowDragon, x: 0x40, y: 0x2C);
        world.Player.X = 0x40;
        world.Player.Y = 0x20;
        Assert.True(Math.Abs(dragon.Y - world.Player.Y) > 4);

        YellowTurns(world, 1);

        Assert.Equal(0xD0, dragon.State);
        Assert.Equal(world.Player.X, dragon.X);
        Assert.Equal(world.Player.Y, dragon.Y);
    }

    [Fact]
    public void AShortRoarUsesTheProColumn()
    {
        var world = new GameWorld { ShortDragonRoar = true };
        var dragon = PlaceWithPlayer(world, ObjectIds.YellowDragon, x: 0x40, y: 0x2C);
        world.Player.X = 0x40;
        world.Player.Y = 0x20;

        YellowTurns(world, 1);

        Assert.Equal(0xE8, dragon.State);
    }

    [Fact]
    public void Game2StartsTheShorterAmateurRoar()
    {
        var world = new GameWorld(gameIndex: 1);
        var dragon = PlaceWithPlayer(world, ObjectIds.YellowDragon, x: 0x40, y: 0x2C);
        world.Player.X = 0x40;
        world.Player.Y = 0x20;

        YellowTurns(world, 1);

        Assert.Equal(0xF0, dragon.State);
    }

    [Fact]
    public void TheSwallowWaitsUntilTheNextTurnToPinThePlayer()
    {
        var world = new GameWorld();
        var dragon = PlaceWithPlayer(world, ObjectIds.YellowDragon, x: 0x50, y: 0x30);
        dragon.State = 0xFB;
        dragon.X = 0xA0;
        dragon.Y = 0x10;
        world.Player.X = dragon.X;
        world.Player.Y = dragon.Y;

        YellowTurns(world, 1);

        Assert.Equal(GameWorld.DragonEaten, dragon.State);
        Assert.Equal(0x9B, dragon.X);
        Assert.Equal(0x17, dragon.Y);
        Assert.NotEqual(dragon.X, world.Player.X);

        YellowTurns(world, 1);

        Assert.Equal(dragon.Room, world.Player.Room);
        Assert.Equal((0x9B + 0x03) & 0xFF, world.Player.X);
        Assert.Equal((0x17 - 0x0A) & 0xFF, world.Player.Y);
        Assert.Equal(0x9B, dragon.X);
    }

    [Fact]
    public void LeavingDuringTheRoarReturnsTheDragonToTheChase()
    {
        var world = new GameWorld();
        var dragon = PlaceWithPlayer(world, ObjectIds.YellowDragon, x: 0x50, y: 0x20);
        dragon.State = 0xFB;
        world.Player.Room = dragon.Room + 1;
        var x = dragon.X;

        YellowTurns(world, 5);

        Assert.Equal(GameWorld.DragonChase, dragon.State);
        Assert.Equal(x, dragon.X);
    }

    [Fact]
    public void TheSwordKillsADragonItOverlaps()
    {
        var world = new GameWorld();
        var dragon = Item(world, ObjectIds.YellowDragon);
        var sword = Item(world, ObjectIds.Sword);
        sword.Room = dragon.Room;
        sword.X = dragon.X;
        sword.Y = dragon.Y;
        var x = dragon.X;

        YellowTurns(world, 1);

        Assert.Equal(GameWorld.DragonDead, dragon.State);
        Assert.Equal(x, dragon.X);
    }

    [Fact]
    public void TheSwordWinsWhenItAndThePlayerBothOverlap()
    {
        var world = new GameWorld();
        var dragon = PlaceWithPlayer(world, ObjectIds.YellowDragon, x: 0x50, y: 0x20);
        var sword = Item(world, ObjectIds.Sword);
        sword.Room = world.Player.Room;
        sword.X = world.Player.X;
        sword.Y = world.Player.Y;

        YellowTurns(world, 1);

        Assert.Equal(GameWorld.DragonDead, dragon.State);
    }

    [Fact]
    public void ADeadDragonDoesNotMove()
    {
        var world = new GameWorld();
        var dragon = PlaceWithPlayer(world, ObjectIds.YellowDragon, x: 0x40, y: 0x20);
        world.Player.X = 0x50;
        dragon.State = GameWorld.DragonDead;

        YellowTurns(world, 1);

        Assert.Equal(0x40, dragon.X);
        Assert.Equal(GameWorld.DragonDead, dragon.State);
    }

    [Fact]
    public void TheEmptySideOfTheDragonDoesNotBite()
    {
        var world = new GameWorld();
        var dragon = PlaceWithPlayer(world, ObjectIds.YellowDragon, x: 0x50, y: 0x20);
        world.Player.X = 0x40;
        world.Player.Y = 0x20;

        YellowTurns(world, 1);

        Assert.Equal(GameWorld.DragonChase, dragon.State);
        Assert.Equal(0x4E, dragon.X);
    }

    [Fact]
    public void TheSwordKillsThroughTheDragonsBody()
    {
        var world = new GameWorld();
        var dragon = Item(world, ObjectIds.YellowDragon);
        var sword = Item(world, ObjectIds.Sword);
        sword.Room = dragon.Room;
        sword.X = 0x50;
        sword.Y = 0x28;
        dragon.X = 0x50;
        dragon.Y = 0x30;
        Assert.True(Math.Abs(sword.Y - dragon.Y) > 4);

        YellowTurns(world, 1);

        Assert.Equal(GameWorld.DragonDead, dragon.State);
        Assert.Equal(0x50, dragon.X);
        Assert.Equal(0x30, dragon.Y);
    }

    [Fact]
    public void CloseCornersWithNoSharedPixelDoNotKill()
    {
        var world = new GameWorld();
        var dragon = Item(world, ObjectIds.YellowDragon);
        var sword = Item(world, ObjectIds.Sword);
        sword.Room = dragon.Room;
        sword.X = 0x4D;
        sword.Y = 0x33;
        dragon.X = 0x50;
        dragon.Y = 0x30;

        YellowTurns(world, 1);

        Assert.Equal(GameWorld.DragonChase, dragon.State);
        Assert.Equal(0x50, dragon.X);
        Assert.Equal(0x30, dragon.Y);
    }

    private static WorldObject Item(GameWorld world, int id) =>
        Assert.Single(world.Objects, item => item.Id == id);

    private static WorldObject PlaceWithPlayer(GameWorld world, int id, int x, int y)
    {
        var dragon = Item(world, id);
        dragon.Room = world.Player.Room;
        dragon.X = x;
        dragon.Y = y;
        world.Player.X = x;
        world.Player.Y = y;
        var key = Item(world, ObjectIds.YellowKey);
        if (key.Room == dragon.Room)
        {
            key.Room = 0;
        }

        return dragon;
    }

    private static void YellowTurns(GameWorld world, int turns)
    {
        for (var turn = 0; turn < turns; turn++)
        {
            for (var i = 0; i < 3; i++)
            {
                world.MovePlayer(Direction.None);
            }
        }
    }

    private static void RedTurns(GameWorld world, int turns)
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
