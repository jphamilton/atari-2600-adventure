using Adventure.Core;
using Xunit;

namespace Adventure.Core.Tests;

public class MagnetTests
{
    [Fact]
    public void TheMagnetPullsTheSwordOnePixelTowardItsPole()
    {
        var world = new GameWorld();
        var magnet = Item(world, ObjectIds.Magnet);
        var sword = Place(world, ObjectIds.Sword, magnet.Room, 0x40, 0x18);
        var magnetX = magnet.X;
        var magnetY = magnet.Y;

        HorizontalTurns(world, 1);

        Assert.Equal(0x41, sword.X);
        Assert.Equal(0x18, sword.Y);
        Assert.Equal(magnetX, magnet.X);
        Assert.Equal(magnetY, magnet.Y);
        Assert.Equal(0, sword.Movement);
    }

    [Fact]
    public void ThePoleSitsEightPixelsBelowTheMagnet()
    {
        var world = new GameWorld();
        var magnet = Item(world, ObjectIds.Magnet);
        magnet.X = 0x50;
        magnet.Y = 0x20;
        var sword = Place(world, ObjectIds.Sword, magnet.Room, 0x50, 0x30);

        HorizontalTurns(world, 1);

        Assert.Equal(0x2F, sword.Y);
        Assert.Equal(0x50, sword.X);
    }

    [Fact]
    public void TheYellowKeyIsPulledAheadOfTheSword()
    {
        var world = new GameWorld();
        var magnet = Item(world, ObjectIds.Magnet);
        var key = Place(world, ObjectIds.YellowKey, magnet.Room, 0x40, magnet.Y - 8);
        var sword = Place(world, ObjectIds.Sword, magnet.Room, 0x40, magnet.Y - 8);

        HorizontalTurns(world, 1);

        Assert.Equal(0x41, key.X);
        Assert.Equal(0x40, sword.X);
    }

    [Fact]
    public void AnObjectInAnotherRoomStaysPut()
    {
        var world = new GameWorld();
        var sword = Item(world, ObjectIds.Sword);
        var x = sword.X;
        var room = sword.Room;

        HorizontalTurns(world, 1);

        Assert.Equal(x, sword.X);
        Assert.Equal(room, sword.Room);
    }

    [Fact]
    public void APullAcrossTheRightEdgeChangesRoom()
    {
        var world = new GameWorld();
        var magnet = Item(world, ObjectIds.Magnet);
        magnet.X = 0x9C;
        var sword = Place(world, ObjectIds.Sword, magnet.Room, 0x9A, (magnet.Y - 8) & 0xFF);
        var right = RoomCatalog.ResolveExit(RoomCatalog.Rooms[magnet.Room].Right, world.GameIndex);

        HorizontalTurns(world, 1);

        Assert.Equal(MapGeometry.WrappedFromRightX, sword.X);
        Assert.Equal(right, sword.Room);
        Assert.Equal(0x9C, magnet.X);
    }

    private static WorldObject Item(GameWorld world, int id) =>
        Assert.Single(world.Objects, item => item.Id == id);

    private static WorldObject Place(GameWorld world, int id, int room, int x, int y)
    {
        var item = Item(world, id);
        item.Room = room;
        item.X = x;
        item.Y = y;
        return item;
    }

    private static void HorizontalTurns(GameWorld world, int turns)
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
