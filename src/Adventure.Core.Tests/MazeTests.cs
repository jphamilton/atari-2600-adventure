using Adventure.Core;
using Xunit;

namespace Adventure.Core.Tests;

public class MazeTests
{
    [Fact]
    public void YellowCastleGateIsOpenAndWallsAreReflected()
    {
        var castle = RoomCatalog.Rooms[RoomCatalog.YellowCastle];

        Assert.False(castle.IsWall(6, 20));
        Assert.True(castle.IsWall(6, 0));
        Assert.False(castle.IsWall(5, 20));
        Assert.True(castle.IsWall(5, 39));
    }

    [Fact]
    public void RepeatedPlayfieldCopiesTheLeftHalf()
    {
        var maze = RoomCatalog.Rooms[0x14];

        Assert.False(maze.LeftThinWall);
        Assert.False(maze.IsWall(5, 0));
        Assert.False(maze.IsWall(5, 20));
        Assert.True(maze.IsWall(5, 19));
    }

    [Fact]
    public void WalkingDownFromYellowCastleEntersTheCorridor()
    {
        var world = new GameWorld();

        for (var i = 0; i < 40 && world.Player.Room == RoomCatalog.YellowCastle; i++)
        {
            world.MovePlayer(Direction.Down, pixels: 1);
        }

        Assert.Equal(RoomCatalog.CorridorUnderYellowCastle, world.Player.Room);
        Assert.Equal(MapGeometry.WrappedFromBottomY, world.Player.Y);
        Assert.Equal(GameWorld.StartX, world.Player.X);
    }

    [Fact]
    public void WalkingDownStopsAboveTheSouthWall()
    {
        var world = new GameWorld();
        world.Player.Room = 0x12;
        world.Player.X = 0x28;
        world.Player.Y = 0x20;

        for (var i = 0; i < 40; i++)
        {
            world.MovePlayer(Direction.Down, pixels: 1);
        }

        Assert.Equal(0x12, world.Player.Room);
        Assert.Equal(0x15, world.Player.Y);
        Assert.False(world.Blocks(0x12, world.Player.X, world.Player.Y));
        Assert.True(world.Blocks(0x12, world.Player.X, world.Player.Y - 1));
    }

    [Fact]
    public void TheYellowCastleCornerKeepsTheCartridgeStandoff()
    {
        var left = new GameWorld();
        for (var i = 0; i < 90; i++)
        {
            left.MovePlayer(Direction.Left);
        }

        var freeLeft = new List<int>();
        var overlappedLeft = false;
        for (var i = 0; i < 3; i++)
        {
            left.MovePlayer(Direction.Left);
            if (left.Blocks(left.Player.Room, left.Player.X, left.Player.Y))
            {
                overlappedLeft = true;
            }
            else
            {
                freeLeft.Add(left.Player.X);
            }
        }

        Assert.True(overlappedLeft);
        Assert.NotEmpty(freeLeft);
        Assert.All(freeLeft, x => Assert.Equal(0x0B, x));

        var down = new GameWorld();
        down.Player.X = 0x28;
        for (var i = 0; i < 30; i++)
        {
            down.MovePlayer(Direction.Down);
        }

        var freeDown = new List<int>();
        var overlappedDown = false;
        for (var i = 0; i < 3; i++)
        {
            down.MovePlayer(Direction.Down);
            if (down.Blocks(down.Player.Room, down.Player.X, down.Player.Y))
            {
                overlappedDown = true;
            }
            else
            {
                freeDown.Add(down.Player.Y);
            }
        }

        Assert.True(overlappedDown);
        Assert.NotEmpty(freeDown);
        Assert.All(freeDown, y => Assert.Equal(0x17, y));
        Assert.Equal(RoomCatalog.YellowCastle, down.Player.Room);
    }

    [Fact]
    public void AOnePixelWalkStopsFlushWithTheWall()
    {
        var world = new GameWorld();
        world.Player.Y = 0x18;

        for (var i = 0; i < 80; i++)
        {
            world.MovePlayer(Direction.Left, pixels: 1);
        }

        Assert.Equal(0x0A, world.Player.X);
        Assert.False(world.Blocks(world.Player.Room, world.Player.X, world.Player.Y));
        Assert.True(world.Blocks(world.Player.Room, world.Player.X - 1, world.Player.Y));
    }

    [Fact]
    public void WalkingIntoACastleWallDoesNotLeaveTheRoom()
    {
        var world = new GameWorld();
        world.Player.Y = 0x18;
        var startX = world.Player.X;

        for (var i = 0; i < 80; i++)
        {
            world.MovePlayer(Direction.Left, pixels: 1);
        }

        Assert.Equal(RoomCatalog.YellowCastle, world.Player.Room);
        Assert.True(world.Player.X < startX);
        Assert.True(world.Player.X >= MapGeometry.LeftEdge);
        Assert.True(world.Blocks(world.Player.Room, world.Player.X - 1, world.Player.Y));
    }

    [Fact]
    public void TheRedMazeBottomExitBouncesOffTheWallAbove()
    {
        var world = new GameWorld();
        world.Player.Room = 0x19;
        world.Player.X = 0x50;
        world.Player.Y = MapGeometry.BottomEdge;

        world.MovePlayer(Direction.Down, 1);

        Assert.Equal(0x19, world.Player.Room);
        Assert.Equal(MapGeometry.BottomEdge, world.Player.Y);
        Assert.False(world.Blocks(0x19, world.Player.X, world.Player.Y));
    }

    [Fact]
    public void HoldingDownAtTheRedMazeBottomStaysInTheRoom()
    {
        var world = new GameWorld();
        world.Player.Room = 0x19;
        world.Player.X = 0x50;
        world.Player.Y = MapGeometry.BottomEdge;

        for (var i = 0; i < 6; i++)
        {
            world.MovePlayer(Direction.Down);
        }

        Assert.Equal(0x19, world.Player.Room);
        Assert.Equal(MapGeometry.BottomEdge, world.Player.Y);
        Assert.False(world.Blocks(0x19, world.Player.X, world.Player.Y));
    }

    [Fact]
    public void TheNameRoomExitBouncesOffTheWallBelow()
    {
        var world = new GameWorld();
        world.Player.Room = 0x1E;
        world.Player.X = 0x50;
        world.Player.Y = MapGeometry.TopEdge - 1;

        world.MovePlayer(Direction.Up, 1);

        Assert.Equal(0x1E, world.Player.Room);
        Assert.Equal(MapGeometry.TopEdge - 1, world.Player.Y);
        Assert.False(world.Blocks(0x1E, world.Player.X, world.Player.Y));
    }

    [Fact]
    public void TheRedMazeBottomStillEntersRedMaze1ThroughTheTop()
    {
        var world = new GameWorld();
        world.Player.Room = 0x19;
        world.Player.X = 0x44;
        world.Player.Y = MapGeometry.TopEdge - 1;

        world.MovePlayer(Direction.Up, 1);

        Assert.Equal(0x17, world.Player.Room);
        Assert.Equal(MapGeometry.WrappedFromTopY, world.Player.Y);
    }

    [Fact]
    public void EveryExitLandsInARealRoom()
    {
        Assert.Equal(31, RoomCatalog.Rooms.Count);
        foreach (var room in RoomCatalog.Rooms)
        {
            Assert.NotNull(room);
            for (var game = 0; game < 3; game++)
            {
                Assert.InRange(RoomCatalog.ResolveExit(room.Up, game), 0, 30);
                Assert.InRange(RoomCatalog.ResolveExit(room.Right, game), 0, 30);
                Assert.InRange(RoomCatalog.ResolveExit(room.Down, game), 0, 30);
                Assert.InRange(RoomCatalog.ResolveExit(room.Left, game), 0, 30);
            }
        }
    }

    [Fact]
    public void HeldDirectionMovesOnTheFirstOfEveryThreeFrames()
    {
        var right = new GameWorld();
        right.Player.Y = 0x18;
        for (var i = 0; i < 3; i++)
        {
            right.MovePlayer(Direction.Right);
        }

        Assert.Equal(RoomCatalog.YellowCastle, right.Player.Room);
        Assert.Equal(GameWorld.StartX + 3, right.Player.X);
        Assert.Equal(0x18, right.Player.Y);

        var up = new GameWorld();
        up.Player.Y = 0x18;
        for (var i = 0; i < 3; i++)
        {
            up.MovePlayer(Direction.Up);
        }

        Assert.Equal(RoomCatalog.YellowCastle, up.Player.Room);
        Assert.Equal(GameWorld.StartX, up.Player.X);
        Assert.Equal(0x1B, up.Player.Y);

        var diagonal = new GameWorld();
        diagonal.Player.Y = 0x18;
        for (var i = 0; i < 3; i++)
        {
            diagonal.MovePlayer(Direction.Right | Direction.Up);
        }

        Assert.Equal(RoomCatalog.YellowCastle, diagonal.Player.Room);
        Assert.Equal(GameWorld.StartX + 3, diagonal.Player.X);
        Assert.Equal(0x1B, diagonal.Player.Y);
    }

    [Fact]
    public void ABlockedStepSlidesAlongTheWallOnTheLaterFields()
    {
        var world = new GameWorld();
        world.Player.Y = 0x18;
        for (var i = 0; i < 80; i++)
        {
            world.MovePlayer(Direction.Left, pixels: 1);
        }

        var x = world.Player.X;
        var y = world.Player.Y;
        Assert.True(world.Blocks(world.Player.Room, x - 1, y));

        for (var i = 0; i < 3; i++)
        {
            world.MovePlayer(Direction.Left | Direction.Up);
        }

        Assert.Equal(RoomCatalog.YellowCastle, world.Player.Room);
        Assert.Equal(x, world.Player.X);
        Assert.Equal(y + 3, world.Player.Y);
    }

    [Fact]
    public void TheInputCountAndTheGatesAdvanceOnTheirOwnFields()
    {
        var world = new GameWorld();
        var counts = 0;
        var portals = 0;
        for (var i = 0; i < 6; i++)
        {
            world.MovePlayer(Direction.None);
            if (world.CountsInput)
            {
                counts++;
                world.CountFrame();
            }

            if (world.RunsPortals)
            {
                portals++;
            }
        }

        Assert.Equal(2, counts);
        Assert.Equal(2, world.FrameSeed);
        Assert.Equal(2, portals);

        world.Objects.Single(item => item.Id == ObjectIds.Chalice).Room = 0x12;
        world.MovePlayer(Direction.None);
        Assert.False(world.Active);
        Assert.True(world.CountsInput);
        Assert.False(world.RunsPortals);

        world.MovePlayer(Direction.None);
        world.MovePlayer(Direction.None);
        Assert.True(world.CountsInput);
        Assert.False(world.RunsPortals);
    }

    [Fact]
    public void LeavingTheRightSideCorridorToTheLeftReturnsToTheMaze()
    {
        var corridor = RoomCatalog.Rooms[0x0C];
        Assert.Equal(0x0B, corridor.Left);
        Assert.Equal(0x0D, corridor.Right);

        var world = new GameWorld(gameIndex: 1);
        world.Player.Room = corridor.Id;
        world.Player.X = 0x20;
        world.Player.Y = 0x55;

        for (var i = 0; i < 40 && world.Player.Room == corridor.Id; i++)
        {
            world.MovePlayer(Direction.Left, pixels: 1);
        }

        Assert.Equal(0x0B, world.Player.Room);
        Assert.Equal(MapGeometry.WrappedFromLeftX, world.Player.X);
    }

    [Fact]
    public void Game1ResolvesTheLevelExitUnderTheTopAccess()
    {
        Assert.Equal(0x10, RoomCatalog.ResolveExit(0x80, gameIndex: 0));
        Assert.Equal(0x0F, RoomCatalog.ResolveExit(0x80, gameIndex: 1));
    }
}
