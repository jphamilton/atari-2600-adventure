using Adventure.Core;
using Xunit;

namespace Adventure.Core.Tests;

public class SoundTests
{
    [Fact]
    public void APickupWritesTheFirstNoteOfTypeFive()
    {
        var world = new GameWorld();
        PlaceOnPlayer(world, ObjectIds.Sword);

        world.ResolveCarry(false);
        world.MovePlayer(Direction.None);

        Assert.Equal(0x06, world.Audc);
        Assert.Equal(0x05, world.Audv);
        Assert.Equal(0x03, world.Audf);
    }

    [Fact]
    public void ADropWritesTheFirstNoteOfTypeFour()
    {
        var world = new GameWorld();
        var sword = PlaceOnPlayer(world, ObjectIds.Sword);
        world.ResolveCarry(false);
        sword.X = world.Player.X + 20;

        world.ResolveCarry(true);
        world.MovePlayer(Direction.None);

        Assert.Equal(0x06, world.Audc);
        Assert.Equal(0x05, world.Audv);
        Assert.Equal(0x00, world.Audf);
    }

    [Fact]
    public void ARoarWritesTheFirstNoteOfTypeOne()
    {
        var world = TouchingYellowDragon(out _);

        HearDragon(world);

        Assert.Equal(0x03, world.Audc);
        Assert.Equal(0x0F, world.Audv);
        Assert.Equal(0x1F, world.Audf);
    }

    [Fact]
    public void ASwallowWritesTheFirstNoteOfTypeTwo()
    {
        var world = TouchingYellowDragon(out var dragon);
        dragon.State = 0xFB;

        HearDragon(world);

        Assert.Equal(0x06, world.Audc);
        Assert.Equal(0x0F, world.Audv);
        Assert.Equal(0x00, world.Audf);
    }

    [Fact]
    public void ASwordWritesTheFirstNoteOfTypeThree()
    {
        var world = TouchingYellowDragon(out var dragon);
        var sword = Item(world, ObjectIds.Sword);
        sword.Room = dragon.Room;
        sword.X = world.Player.X;
        sword.Y = world.Player.Y;

        HearDragon(world);

        Assert.Equal(0x04, world.Audc);
        Assert.Equal(0x0F, world.Audv);
        Assert.Equal(0x10, world.Audf);
    }

    [Fact]
    public void APickupReplacesARoar()
    {
        var world = TouchingYellowDragon(out _);
        world.MovePlayer(Direction.None);
        world.MovePlayer(Direction.None);
        PlaceOnPlayer(world, ObjectIds.Chalice);
        world.ResolveCarry(false);

        world.MovePlayer(Direction.None);
        world.MovePlayer(Direction.None);

        Assert.Equal(0x06, world.Audc);
        Assert.Equal(0x05, world.Audv);
        Assert.Equal(0x03, world.Audf);
    }

    [Fact]
    public void AFinishedCountSilencesTheVoice()
    {
        var world = new GameWorld();
        PlaceOnPlayer(world, ObjectIds.Sword);
        world.ResolveCarry(false);

        for (var i = 0; i < 10; i++)
        {
            world.MovePlayer(Direction.None);
        }

        Assert.Equal(0x06, world.Audc);
        Assert.Equal(0x05, world.Audv);
        Assert.Equal(0x00, world.Audf);

        world.MovePlayer(Direction.None);
        world.MovePlayer(Direction.None);
        world.MovePlayer(Direction.None);

        Assert.Equal(0x06, world.Audc);
        Assert.Equal(0x00, world.Audv);
        Assert.Equal(0x00, world.Audf);
    }

    [Fact]
    public void TheChaliceInTheEntryStartsTheFanfareAndThenFreezesMovement()
    {
        var world = TouchingYellowDragon(out var dragon);
        var dragonX = dragon.X;
        Item(world, ObjectIds.Chalice).Room = 0x12;

        world.MovePlayer(Direction.None);

        Assert.False(world.Active);
        Assert.Equal(0, world.Audv);
        Assert.Equal(dragonX, dragon.X);

        world.MovePlayer(Direction.None);

        Assert.Equal(0xFE, world.Audc);
        Assert.Equal(0x7F, world.Audv);
        Assert.Equal(0x1F, world.Audf);
        Assert.Equal((byte)0xFE, world.FanfareColor);
        Assert.Equal(dragonX, dragon.X);
    }

    private static void HearDragon(GameWorld world)
    {
        world.MovePlayer(Direction.None);
        world.MovePlayer(Direction.None);
        world.MovePlayer(Direction.None);
        world.MovePlayer(Direction.None);
    }

    private static GameWorld TouchingYellowDragon(out WorldObject dragon)
    {
        var world = new GameWorld();
        dragon = Item(world, ObjectIds.YellowDragon);
        dragon.Room = world.Player.Room;
        dragon.X = 0x40;
        dragon.Y = 0x2C;
        world.Player.X = 0x40;
        world.Player.Y = 0x20;
        var key = Item(world, ObjectIds.YellowKey);
        if (key.Room == dragon.Room)
        {
            key.Room = 0;
        }

        return world;
    }

    private static WorldObject PlaceOnPlayer(GameWorld world, int id)
    {
        var item = Item(world, id);
        item.Room = world.Player.Room;
        item.X = world.Player.X;
        item.Y = world.Player.Y;
        return item;
    }

    private static WorldObject Item(GameWorld world, int id)
    {
        return world.Objects.First(item => item.Id == id);
    }
}
