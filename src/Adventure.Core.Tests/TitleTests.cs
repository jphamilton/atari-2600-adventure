using Adventure.Core;
using Xunit;

namespace Adventure.Core.Tests;

public class TitleTests
{
    [Fact]
    public void PowerOnHidesThePlayerInTheNumberRoom()
    {
        var world = GameWorld.AtTitle();

        Assert.Equal(RoomCatalog.NumberRoom, world.Player.Room);
        Assert.Equal(0, world.Player.X);
        Assert.Equal(0, world.Player.Y);
        Assert.Equal(0, world.GameIndex);
        Assert.True(world.Active);
        Assert.Null(world.Carried);
        Assert.Equal(0x1C, Item(world, ObjectIds.Chalice).Room);
    }

    [Fact]
    public void ResetFromTheTitleStartsInTheCastleAndLeavesObjectsWhereTheyAre()
    {
        var world = GameWorld.AtTitle();
        var sword = Item(world, ObjectIds.Sword);
        sword.Room = 0x04;
        var dragon = Item(world, ObjectIds.YellowDragon);
        dragon.State = GameWorld.DragonDead;
        var key = Item(world, ObjectIds.YellowKey);
        key.Room = world.Player.Room;
        key.X = world.Player.X;
        key.Y = world.Player.Y;
        world.ResolveCarry(false);

        world.Reset();

        Assert.Equal(RoomCatalog.YellowCastle, world.Player.Room);
        Assert.Equal(GameWorld.StartX, world.Player.X);
        Assert.Equal(GameWorld.StartY, world.Player.Y);
        Assert.Equal(0x04, sword.Room);
        Assert.Equal(GameWorld.DragonChase, dragon.State);
        Assert.Null(world.Carried);
    }

    [Fact]
    public void ResetClearsANoteThatHasNotPlayedYet()
    {
        var world = new GameWorld();
        var sword = Item(world, ObjectIds.Sword);
        sword.Room = world.Player.Room;
        sword.X = world.Player.X;
        sword.Y = world.Player.Y;
        world.ResolveCarry(false);

        world.Reset();
        world.MovePlayer(Direction.None);

        Assert.Equal(0, world.Audv);
    }

    [Fact]
    public void SelectOnTheTitleCyclesTheThreeGamesAndThenWraps()
    {
        var world = GameWorld.AtTitle();

        world.Select();

        Assert.Equal(1, world.GameIndex);
        Assert.Equal(0x11, Item(world, ObjectIds.Sword).Room);
        Assert.Equal(0, world.Player.Y);

        world.Select();
        Assert.Equal(2, world.GameIndex);
        Assert.Equal(0x13, Item(world, ObjectIds.Chalice).Room);

        world.Select();
        Assert.Equal(0, world.GameIndex);
        Assert.Equal(0x1C, Item(world, ObjectIds.Chalice).Room);
    }

    [Fact]
    public void SelectOutsideTheNumberRoomReloadsTheTitleWithoutChangingTheGame()
    {
        var world = new GameWorld(gameIndex: 1);
        var x = world.Player.X;
        Item(world, ObjectIds.Sword).Room = 0x04;

        world.Select();

        Assert.Equal(1, world.GameIndex);
        Assert.Equal(RoomCatalog.NumberRoom, world.Player.Room);
        Assert.Equal(0, world.Player.Y);
        Assert.Equal(x, world.Player.X);
        Assert.Equal(0x11, Item(world, ObjectIds.Sword).Room);
    }

    [Fact]
    public void ResetAfterAWinReturnsToTheTitle()
    {
        var world = new GameWorld();
        Item(world, ObjectIds.Chalice).Room = 0x12;
        world.MovePlayer(Direction.None);
        Assert.False(world.Active);

        world.Reset();

        Assert.True(world.Active);
        Assert.Equal(RoomCatalog.NumberRoom, world.Player.Room);
        Assert.Equal(0, world.Player.Y);
        Assert.Equal(0x1C, Item(world, ObjectIds.Chalice).Room);
    }

    private static WorldObject Item(GameWorld world, int id) =>
        world.Objects.First(item => item.Id == id);
}
