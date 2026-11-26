using Adventure.Core;
using Xunit;

namespace Adventure.Core.Tests;

public class ColorTests
{
    [Fact]
    public void TheYellowCastleIsTheNtscChartYellow()
    {
        var yellow = AtariPalette.Rgb(0x1A);

        Assert.Equal((byte)0xD0, yellow.R);
        Assert.Equal((byte)0xD0, yellow.G);
        Assert.Equal((byte)0x50, yellow.B);
        Assert.Equal(yellow, AtariPalette.Rgb(0x1B));
    }

    [Fact]
    public void TheChaliceColorStepsWithTheFrame()
    {
        var world = new GameWorld();
        world.FrameSeed = 0x10;

        Assert.Equal(0x20, world.DisplayColor(0xCB));

        world.CountFrame();

        Assert.Equal(0x22, world.DisplayColor(0xCB));
        Assert.NotEqual(0x20, world.DisplayColor(0xCB));
    }

    [Fact]
    public void AnEvenColorStaysUntilTheIdleMerge()
    {
        var world = new GameWorld();
        world.FrameSeed = 0x10;

        Assert.Equal(0x1A, world.DisplayColor(0x1A));
    }

    [Fact]
    public void IdleMergesTheFrameBeforeTheShift()
    {
        Assert.Equal(0x00, GameWorld.FlashColor(0xCB, 0x04, 0x80));
    }

    [Fact]
    public void AHeldDirectionClearsTheIdleCount()
    {
        var world = new GameWorld();
        world.FrameSeed = 0xFF;
        world.CountFrame();

        Assert.Equal(1, world.IdleCount);

        world.MovePlayer(Direction.Left);

        Assert.Equal(0, world.IdleCount);
    }
}
