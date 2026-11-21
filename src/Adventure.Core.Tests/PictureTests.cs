using Adventure.Core;
using Xunit;

namespace Adventure.Core.Tests;

public class PictureTests
{
    [Fact]
    public void DragonsUseTheChaseDeadAndRoarPictures()
    {
        Assert.Equal(0x06, ObjectSprites.Dragon(GameWorld.DragonChase)[0]);
        Assert.Equal(0x06, ObjectSprites.Dragon(GameWorld.DragonEaten)[0]);
        Assert.Equal(0x0C, ObjectSprites.Dragon(GameWorld.DragonDead)[0]);
        Assert.Equal(0x80, ObjectSprites.Dragon(0xD0)[0]);
    }

    [Fact]
    public void TheBatFlapsBetweenTwoPictures()
    {
        Assert.Equal(0x81, ObjectSprites.Bat(0)[0]);
        Assert.Equal(0x81, ObjectSprites.Bat(3)[0]);
        Assert.Equal(0x01, ObjectSprites.Bat(4)[0]);
        Assert.Equal(0x01, ObjectSprites.Bat(7)[0]);
    }

    [Fact]
    public void AClosedGateIsTheFullGrateAndAnOpeningGateStartsLower()
    {
        Assert.Equal(16, ObjectSprites.Gate(GameWorld.GateClosed).Length);
        Assert.Equal(14, ObjectSprites.Gate(0x20).Length);
        Assert.Equal(6, ObjectSprites.Gate(0x30).Length);
        Assert.Equal(4, ObjectSprites.Gate(0xFF).Length);
    }

    [Fact]
    public void TheNumberRoomShowsTheGameAndTheSecretRoomHasTheAuthor()
    {
        Assert.Equal(0x04, ObjectSprites.Number(0)[0]);
        Assert.Equal(0x02, ObjectSprites.Number(1)[3]);
        Assert.Equal(0x06, ObjectSprites.Number(2)[3]);
        Assert.Equal(95, ObjectSprites.Author.Length);
        Assert.Equal(0xF0, ObjectSprites.Author[0]);
    }
}
