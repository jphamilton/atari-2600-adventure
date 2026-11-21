using Adventure.Core;
using Xunit;

namespace Adventure.Core.Tests;

public class ScatterTests
{
    [Fact]
    public void AZeroSeedPlacesGame3AndSkipsRoomsOutsideTheBounds()
    {
        var world = GameWorld.AtTitle();
        world.FrameSeed = 0;
        world.Select();
        world.Select();

        Assert.Equal(2, world.GameIndex);
        Assert.Equal(0x01, Item(world, ObjectIds.Magnet).Room);
        Assert.Equal(0x80, Item(world, ObjectIds.Magnet).X);
        Assert.Equal(0x02, Item(world, ObjectIds.Bat).Room);
        Assert.Equal(0x03, Item(world, ObjectIds.BlackKey).Room);
        Assert.Equal(0x04, Item(world, ObjectIds.WhiteKey).Room);
        Assert.Equal(0x05, Item(world, ObjectIds.YellowKey).Room);
        Assert.Equal(0x06, Item(world, ObjectIds.Bridge).Room);
        Assert.Equal(0x07, Item(world, ObjectIds.Sword).Room);
        Assert.Equal(0x08, Item(world, ObjectIds.GreenDragon).Room);
        Assert.Equal(0x09, Item(world, ObjectIds.YellowDragon).Room);
        Assert.Equal(0x0A, Item(world, ObjectIds.RedDragon).Room);
        Assert.Equal(0x13, Item(world, ObjectIds.Chalice).Room);
        Assert.Equal(0x15, Item(world, ObjectIds.Dot).Room);
        Assert.Equal(0x11, Item(world, ObjectIds.YellowPortcullis).Room);
        Assert.Equal(0x13, world.FrameSeed);
    }

    [Fact]
    public void GamesOneAndTwoKeepTheFixedLayout()
    {
        var world = GameWorld.AtTitle();
        world.FrameSeed = 0;

        world.Select();

        Assert.Equal(1, world.GameIndex);
        Assert.Equal(0x14, Item(world, ObjectIds.RedDragon).Room);
        Assert.Equal(0, world.FrameSeed);
    }

    private static WorldObject Item(GameWorld world, int id) =>
        world.Objects.First(item => item.Id == id);
}
