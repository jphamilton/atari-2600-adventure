namespace Adventure.Core;

public sealed class GameWorld
{
    public const int PlayerStep = 3;
    public const int StartX = 0x50;
    public const int StartY = 0x20;
    public const int PickupNudge = 6;
    public const int GateClosed = 0x1C;
    public const int GateOpen = 0x01;
    public const int GateEntryY = 0x10;
    public const int GateInteriorX = 0x50;
    public const int GateInteriorY = 0x2C;
    public const int DragonChase = 0x00;
    public const int DragonDead = 0x01;
    public const int DragonEaten = 0x02;
    public const int RedDragonStep = 3;
    public const int DragonStep = 2;

    private const int GateAnimatedEnd = 0x38;

    private readonly IReadOnlyList<Room> _rooms;
    private readonly List<WorldObject> _objects;
    private int _movementPhase;
    private Direction _heldDirection;
    private Direction _stick;
    private bool _stepBlocked;
    private int _savedRoom;
    private int _savedX;
    private int _savedY;
    private bool _buttonDown;
    private int _carryOffsetX;
    private int _carryOffsetY;
    private WorldObject? _carried;
    private int _noiseType;
    private int _noiseCount;
    private PlaytestSetup? _playtest;
    private readonly HashSet<int> _disabled = [];

    public GameWorld(int gameIndex = 0, IReadOnlyList<Room>? rooms = null)
    {
        if (gameIndex is < 0 or > 2)
        {
            throw new ArgumentOutOfRangeException(nameof(gameIndex), "Game index is 0, 1, or 2.");
        }

        GameIndex = gameIndex;
        _rooms = rooms ?? RoomCatalog.Rooms;
        _objects = [.. ObjectLayout.ForGame(gameIndex)];
        Player = new Actor(ObjectKind.Player, RoomCatalog.YellowCastle, StartX, StartY);
    }

    public static GameWorld AtTitle()
    {
        var world = new GameWorld();
        world.Player.X = 0;
        world.LoadTitle();
        return world;
    }

    public static GameWorld ForPlaytest(PlaytestSetup setup)
    {
        ArgumentNullException.ThrowIfNull(setup);
        var world = new GameWorld(setup.GameIndex);
        world._playtest = setup;
        world.ApplyPlaytest();
        return world;
    }

    public int GameIndex { get; private set; }

    public int FrameSeed { get; set; }

    public int IdleCount { get; private set; }

    public bool CountsInput { get; private set; }

    public bool RunsPortals { get; private set; }

    public bool DragonsFearSword { get; set; }

    public bool ShortDragonRoar { get; set; }

    public int BatFedUp { get; set; }

    public int BatCargo { get; set; } = ObjectIds.None;

    public bool Active { get; private set; } = true;

    public int Audc { get; private set; }

    public int Audf { get; private set; }

    public int Audv { get; private set; }

    public byte? FanfareColor { get; private set; }

    public bool HasSurround { get; private set; }

    public int SurroundX { get; private set; }

    public int SurroundY { get; private set; }

    public Actor Player { get; }

    public IReadOnlyList<WorldObject> Objects => _objects;

    public WorldObject? Carried => _carried;

    public Room CurrentRoom => _rooms[Player.Room];

    public byte DisplayColor(byte color) => FlashColor(color, FrameSeed, IdleCount);

    public static byte FlashColor(byte color, int frame, int idle)
    {
        var value = (color & 1) == 0 ? color >> 1 : frame;
        if (idle >= 0x80)
        {
            value = (value ^ idle) & 0xFB;
        }

        return (byte)((value << 1) & 0xFF);
    }

    public void Reset()
    {
        IdleCount = 0;
        if (!Active)
        {
            LoadTitle();
            return;
        }

        if (_playtest is not null)
        {
            _noiseCount = 0;
            LoadTitle();
            return;
        }

        _carried = null;
        Player.Room = RoomCatalog.YellowCastle;
        Player.X = StartX;
        Player.Y = StartY;
        Find(ObjectIds.RedDragon).State = DragonChase;
        Find(ObjectIds.YellowDragon).State = DragonChase;
        Find(ObjectIds.GreenDragon).State = DragonChase;
        _noiseCount = 0;
    }

    public void CountFrame()
    {
        FrameSeed = (FrameSeed + 1) & 0xFF;
        if (FrameSeed == 0)
        {
            IdleCount = (IdleCount + 1) & 0xFF;
            if (IdleCount == 0)
            {
                IdleCount = 0x80;
            }
        }

        if (_heldDirection != Direction.None)
        {
            IdleCount = 0;
        }
    }

    public void Select()
    {
        IdleCount = 0;
        if (Player.Room == RoomCatalog.NumberRoom)
        {
            GameIndex = (GameIndex + 1) % 3;
        }

        LoadTitle();
    }

    public void MovePlayer(Direction direction)
    {
        if (direction != Direction.None)
        {
            IdleCount = 0;
        }

        _heldDirection = direction;
        if (!Active)
        {
            CountsInput = true;
            RunsPortals = false;
            AdvanceSound();
            return;
        }

        var phase = _movementPhase;
        _movementPhase = (phase + 1) % 3;
        CountsInput = phase == 0;
        RunsPortals = phase == 1;
        if (phase == 0)
        {
            AdvanceSound();
            CheckWin();
        }

        StepBall(phase, direction);
        if (phase == 1)
        {
            PlaceSurround();
            AdvanceBat();
            AdvanceDragon(ObjectIds.GreenDragon);
            AdvanceDragon(ObjectIds.YellowDragon);
        }
        else if (phase == 2)
        {
            AdvanceDragon(ObjectIds.RedDragon);
            AdvanceMagnet();
        }

        FollowCarried();
        if (ReferenceEquals(_carried, Find(ObjectIds.Bat)))
        {
            HoldCargo(Find(ObjectIds.Bat));
        }
    }

    public void MovePlayer(Direction direction, int pixels)
    {
        if (pixels < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pixels));
        }

        TryStep(direction, pixels);
    }

    private void StepBall(int phase, Direction direction)
    {
        if (!_stepBlocked)
        {
            _stick = Direction.None;
            SaveBall();
        }

        if (phase == 0)
        {
            _stick = direction;
        }

        var allowed = phase switch
        {
            0 => _stick,
            1 => _stick & (Direction.Up | Direction.Down),
            _ => _stick & (Direction.Left | Direction.Right),
        };
        if (allowed == Direction.None)
        {
            return;
        }

        RestoreBall();
        _stepBlocked = TryStep(allowed, PlayerStep);
    }

    private void SaveBall()
    {
        _savedRoom = Player.Room;
        _savedX = Player.X;
        _savedY = Player.Y;
    }

    private void RestoreBall()
    {
        Player.Room = _savedRoom;
        Player.X = _savedX;
        Player.Y = _savedY;
        FollowCarried();
    }

    private bool TryStep(Direction direction, int pixels)
    {
        var dx = 0;
        var dy = 0;
        if (direction.HasFlag(Direction.Left))
        {
            dx--;
        }

        if (direction.HasFlag(Direction.Right))
        {
            dx++;
        }

        if (direction.HasFlag(Direction.Up))
        {
            dy++;
        }

        if (direction.HasFlag(Direction.Down))
        {
            dy--;
        }

        if (dx == 0 && dy == 0)
        {
            return false;
        }

        for (var step = 0; step < pixels; step++)
        {
            var nextX = Player.X + dx;
            var nextY = Player.Y + dy;
            if (Blocks(Player.Room, nextX, nextY) || ClosedGateBlocks(Player.Room, nextX, nextY))
            {
                return true;
            }

            Player.X = nextX;
            Player.Y = nextY;
            if (CrossEdge())
            {
                FollowCarried();
                return false;
            }

            FollowCarried();
        }

        return false;
    }

    public void ResolveCarry(bool buttonDown)
    {
        if (buttonDown && !_buttonDown && _carried is not null)
        {
            _carried = null;
            StartNoise(NoiseDrop, NoiseShort);
        }

        _buttonDown = buttonDown;

        foreach (var item in _objects)
        {
            if (!item.IsCarriable || !Touches(item))
            {
                continue;
            }

            if (_movementPhase == 1)
            {
                Nudge(item, _heldDirection);
            }

            var fresh = !ReferenceEquals(_carried, item);
            _carryOffsetX = item.X - Player.X;
            _carryOffsetY = item.Y - Player.Y;
            _carried = item;
            if (fresh)
            {
                StartNoise(NoisePickup, NoiseShort);
            }

            return;
        }
    }

    public bool Touches(WorldObject item)
    {
        if (item.Room != Player.Room)
        {
            return false;
        }

        return ObjectSprites.MarkerOverlaps(Picture(item), item.X, item.Y, PixelScale(item), Player.X, Player.Y);
    }

    private void FollowCarried()
    {
        if (_carried is null)
        {
            return;
        }

        _carried.Room = Player.Room;
        _carried.X = (Player.X + _carryOffsetX) & 0xFF;
        _carried.Y = (Player.Y + _carryOffsetY) & 0xFF;
        if (CarryEntersCastle(_carried))
        {
            return;
        }

        WrapRoaming(_carried);
    }

    private bool CarryEntersCastle(WorldObject item)
    {
        foreach (var link in GateLinks)
        {
            if (item.Room != link.EntryRoom || item.Y >= MapGeometry.BottomEdge)
            {
                continue;
            }

            var gate = Find(link.GateId);
            if (gate.State == GateClosed)
            {
                continue;
            }

            item.Room = link.CastleRoom;
            item.X = GateInteriorX;
            item.Y = GateInteriorY;
            gate.State = GateOpen;
            return true;
        }

        return false;
    }

    private static void Nudge(WorldObject item, Direction direction)
    {
        var dx = 0;
        var dy = 0;
        if (direction.HasFlag(Direction.Left))
        {
            dx--;
        }

        if (direction.HasFlag(Direction.Right))
        {
            dx++;
        }

        if (direction.HasFlag(Direction.Up))
        {
            dy++;
        }

        if (direction.HasFlag(Direction.Down))
        {
            dy--;
        }

        item.X += dx * PickupNudge;
        item.Y += dy * PickupNudge;
    }

    public bool Blocks(int roomId, int x, int y)
    {
        if (BridgeOpens(roomId, x, y))
        {
            return false;
        }

        var room = _rooms[roomId];
        if (room.LeftThinWall && x == MapGeometry.LeftThinX)
        {
            return true;
        }

        if (room.RightThinWall && x == MapGeometry.RightThinX && !DotSharesRoom(roomId))
        {
            return true;
        }

        var column = MapGeometry.ColumnForX(x);
        if (column < 0 || column >= MapGeometry.Columns)
        {
            return false;
        }

        for (var scanline = y - 4; scanline < y; scanline++)
        {
            var row = MapGeometry.RowForScanline(scanline);
            if (row >= 0 && room.IsWall(row, column))
            {
                return true;
            }
        }

        return false;
    }

    private bool BridgeOpens(int roomId, int x, int y)
    {
        var bridge = _objects.FirstOrDefault(item => item.Id == ObjectIds.Bridge);
        if (bridge is null || bridge.Room != roomId || ReferenceEquals(bridge, _carried))
        {
            return false;
        }

        var dx = x - bridge.X;
        if (dx is < 0x0A or > 0x16)
        {
            return false;
        }

        var dy = (bridge.Y - y) & 0xFF;
        return dy <= 0x18 || dy >= 0xFC;
    }

    private bool CrossEdge()
    {
        var room = CurrentRoom;
        if (Player.Y >= MapGeometry.TopEdge)
        {
            Player.Y = MapGeometry.WrappedFromTopY;
            Player.Room = RoomCatalog.ResolveExit(room.Up, GameIndex);
            return true;
        }

        if (Player.X < MapGeometry.LeftEdge)
        {
            Player.X = MapGeometry.WrappedFromLeftX;
            Player.Room = RoomCatalog.ResolveExit(room.Left, GameIndex);
            return true;
        }

        if (Player.Y < MapGeometry.BottomEdge)
        {
            if (EnterOpenCastle(Player.Room))
            {
                return true;
            }

            Player.Y = MapGeometry.WrappedFromBottomY;
            Player.Room = RoomCatalog.ResolveExit(room.Down, GameIndex);
            return true;
        }

        if (Player.X >= MapGeometry.RightEdge)
        {
            if (Player.Room == RoomLeftOfName && Find(ObjectIds.Dot).Room != DotHome)
            {
                Player.Room = SecretRoom;
                Player.X = MapGeometry.WrappedFromRightX;
                return true;
            }

            Player.X = MapGeometry.WrappedFromRightX;
            Player.Room = RoomCatalog.ResolveExit(room.Right, GameIndex);
            return true;
        }

        return false;
    }

    public void ResolveGates()
    {
        foreach (var link in GateLinks)
        {
            PullIntoCastle(link);
        }

        foreach (var link in GateLinks.Reverse())
        {
            var gate = Find(link.GateId);
            var key = _objects.FirstOrDefault(item => item.Id == link.KeyId && Overlaps(item, gate));
            if (key is not null)
            {
                gate.State++;
            }

            if (gate.State != GateClosed)
            {
                if (Touches(gate))
                {
                    gate.State = GateOpen;
                    Player.Room = link.EntryRoom;
                    Player.Y = GateEntryY;
                    FollowCarried();
                }
                else
                {
                    var hitter = _objects.FirstOrDefault(item => item.Id != gate.Id && Overlaps(item, gate));
                    if (hitter is not null)
                    {
                        hitter.Room = link.EntryRoom;
                        hitter.Y = GateEntryY;
                    }
                }
            }

            if (gate.State is not GateOpen and not GateClosed)
            {
                gate.State++;
                if (gate.State == GateAnimatedEnd)
                {
                    gate.State = GateOpen;
                }
            }
        }
    }

    private bool ClosedGateBlocks(int room, int x, int y)
    {
        foreach (var gate in Gates())
        {
            if (gate.State != GateClosed || gate.Room != room)
            {
                continue;
            }

            if (ObjectSprites.MarkerOverlaps(Picture(gate), gate.X, gate.Y, PixelScale(gate), x, y))
            {
                return true;
            }
        }

        return false;
    }

    private bool EnterOpenCastle(int room)
    {
        foreach (var link in GateLinks)
        {
            if (link.EntryRoom != room)
            {
                continue;
            }

            var gate = Find(link.GateId);
            if (gate.State == GateClosed)
            {
                return false;
            }

            Player.Room = link.CastleRoom;
            Player.X = GateInteriorX;
            Player.Y = GateInteriorY;
            gate.State = GateOpen;
            return true;
        }

        return false;
    }

    private void PullIntoCastle(GateLink link)
    {
        var gate = Find(link.GateId);
        if (gate.State == GateClosed)
        {
            return;
        }

        var entered = false;
        if (Player.Room == link.EntryRoom && Player.Y < MapGeometry.BottomEdge)
        {
            Player.Room = link.CastleRoom;
            Player.X = GateInteriorX;
            Player.Y = GateInteriorY;
            FollowCarried();
            entered = true;
        }

        foreach (var item in _objects)
        {
            if (item.Room == link.EntryRoom && item.Y < MapGeometry.BottomEdge)
            {
                item.Room = link.CastleRoom;
                item.X = GateInteriorX;
                item.Y = GateInteriorY;
                entered = true;
            }
        }

        if (entered)
        {
            gate.State = GateOpen;
        }
    }

    private IEnumerable<WorldObject> Gates()
    {
        foreach (var link in GateLinks)
        {
            yield return Find(link.GateId);
        }
    }

    private WorldObject Find(int id) => _objects.First(item => item.Id == id);

    private static bool Overlaps(WorldObject left, WorldObject right) =>
        left.Room == right.Room
        && ObjectSprites.SpritesOverlap(
            Picture(left),
            left.X,
            left.Y,
            PixelScale(left),
            Picture(right),
            right.X,
            right.Y,
            PixelScale(right));

    private static ReadOnlySpan<byte> Picture(WorldObject item) =>
        ObjectSprites.For(item.Kind, item.State);

    private static int PixelScale(WorldObject item) =>
        item.Kind == ObjectKind.Bridge ? ObjectSprites.BridgePixelScale : 1;

    private const int Ball = -1;
    private const int DragonSwallow = 0xFC;
    private const int DragonMaxX = 0x9B;
    private const int DragonMinY = 0x17;
    private const int EatenOffsetX = 0x03;
    private const int EatenOffsetY = 0x0A;
    private const int RoamingLeftX = 0x9A;
    private const int NoiseGameOver = 0;
    private const int NoiseRoar = 1;
    private const int NoiseEaten = 2;
    private const int NoiseDie = 3;
    private const int NoiseDrop = 4;
    private const int NoisePickup = 5;
    private const int NoiseShort = 4;
    private const int NoiseDragon = 0x10;
    private const int NoiseWin = 0xFF;
    private const int YellowCastleEntry = 0x12;
    private const int RoomLeftOfName = 0x03;
    private const int DotHome = 0x15;
    private const int SecretRoom = 0x1E;
    private const int InvisibleRoom = 0x08;

    private bool DotSharesRoom(int roomId) => Find(ObjectIds.Dot).Room == roomId;

    private void PlaceSurround()
    {
        if (CurrentRoom.Color != InvisibleRoom)
        {
            HasSurround = false;
            return;
        }

        var x = (Player.X - 0x0E) & 0xFF;
        if (x >= 0xF0)
        {
            x = 0x01;
        }
        else if (x >= 0x82)
        {
            x = 0x81;
        }

        HasSurround = true;
        SurroundX = x;
        SurroundY = (Player.Y + 0x0E) & 0xFF;
    }

    private void LoadTitle()
    {
        _carried = null;
        if (_playtest is not null)
        {
            GameIndex = _playtest.GameIndex;
        }

        Player.Room = RoomCatalog.NumberRoom;
        Player.Y = 0;
        Active = true;
        BatFedUp = 0;
        BatCargo = ObjectIds.None;
        _objects.Clear();
        _objects.AddRange(ObjectLayout.ForGame(GameIndex));
        if (_playtest is not null)
        {
            ApplyPlaytest();
            return;
        }

        if (GameIndex == 2)
        {
            ScatterGame3();
        }
    }

    private void ApplyPlaytest()
    {
        var setup = _playtest ?? throw new InvalidOperationException("No playtest scenario is loaded.");
        GameIndex = setup.GameIndex;
        DragonsFearSword = setup.DragonsFearSword;
        ShortDragonRoar = setup.ShortDragonRoar;
        _disabled.Clear();
        Player.Room = setup.PlayerRoom;
        Player.X = setup.PlayerX;
        Player.Y = setup.PlayerY;
        Active = true;
        foreach (var patch in setup.Objects)
        {
            var item = Find(patch.Id);
            if (patch.Room is int room)
            {
                item.Room = room;
            }

            if (patch.X is int x)
            {
                item.X = x;
            }

            if (patch.Y is int y)
            {
                item.Y = y;
            }

            if (patch.State is int state)
            {
                item.State = state;
            }

            if (patch.Disabled)
            {
                _disabled.Add(patch.Id);
            }
        }

        _carried = null;
        _carryOffsetX = 0;
        _carryOffsetY = 0;
        if (setup.Carrying is not int carriedId)
        {
            return;
        }

        var carried = Find(carriedId);
        carried.Room = Player.Room;
        carried.X = Player.X;
        carried.Y = Player.Y;
        _carried = carried;
    }

    private void ScatterGame3()
    {
        var seed = FrameSeed;
        foreach (var (id, low, high) in Game3Rooms)
        {
            int room;
            do
            {
                seed = ((seed >> 5) + seed + 1) & 0xFF;
                room = seed & 0x1F;
            }
            while (room < low || room > high);

            Find(id).Room = room;
        }

        FrameSeed = seed;
    }

    private static readonly (int Id, int Low, int High)[] Game3Rooms =
    [
        (ObjectIds.Magnet, 0x01, 0x1D),
        (ObjectIds.Bat, 0x01, 0x1D),
        (ObjectIds.BlackKey, 0x01, 0x12),
        (ObjectIds.WhiteKey, 0x01, 0x16),
        (ObjectIds.YellowKey, 0x01, 0x1D),
        (ObjectIds.Bridge, 0x01, 0x1D),
        (ObjectIds.Sword, 0x01, 0x1D),
        (ObjectIds.GreenDragon, 0x01, 0x1D),
        (ObjectIds.YellowDragon, 0x01, 0x1D),
        (ObjectIds.RedDragon, 0x01, 0x1D),
        (ObjectIds.Chalice, 0x13, 0x1A),
    ];

    private void CheckWin()
    {
        if (Find(ObjectIds.Chalice).Room != YellowCastleEntry)
        {
            return;
        }

        Active = false;
        StartNoise(NoiseGameOver, NoiseWin);
    }

    private void AdvanceSound()
    {
        if (_noiseCount == 0)
        {
            Audv = 0;
            return;
        }

        _noiseCount = (_noiseCount - 1) & 0xFF;
        var count = _noiseCount;
        switch (_noiseType)
        {
            case NoiseGameOver:
                FanfareColor = (byte)count;
                Audc = count;
                Audv = count >> 1;
                Audf = count >> 3;
                break;
            case NoiseRoar:
                Audc = (count & 1) == 1 ? 0x03 : 0x08;
                Audv = count;
                Audf = (count >> 2) + 0x1C;
                break;
            case NoiseEaten:
                Audc = 0x06;
                Audf = count ^ 0x0F;
                Audv = (count >> 1) + 0x08;
                break;
            case NoiseDie:
                Audc = 0x04;
                Audv = count;
                Audf = count ^ 0x1F;
                break;
            case NoiseDrop:
                Audc = 0x06;
                Audv = 0x05;
                Audf = count ^ 0x03;
                break;
            case NoisePickup:
                Audc = 0x06;
                Audv = 0x05;
                Audf = count;
                break;
        }
    }

    private void StartNoise(int type, int count)
    {
        _noiseType = type;
        _noiseCount = count;
    }

    private void AdvanceDragon(int id)
    {
        if (_disabled.Contains(id))
        {
            return;
        }

        var dragon = Find(id);
        if (dragon.State == DragonChase)
        {
            StepDragon(dragon, id == ObjectIds.RedDragon ? RedDragonStep : DragonStep);
            if (Touches(dragon))
            {
                dragon.State = RoarState();
                dragon.X = Player.X;
                dragon.Y = Player.Y;
                StartNoise(NoiseRoar, NoiseDragon);
            }

            var sword = Find(ObjectIds.Sword);
            if (Overlaps(dragon, sword))
            {
                dragon.State = DragonDead;
                StartNoise(NoiseDie, NoiseDragon);
            }

            return;
        }

        if (dragon.State is DragonDead or DragonEaten)
        {
            if (dragon.State == DragonEaten)
            {
                PinEatenPlayer(dragon);
            }

            return;
        }

        dragon.State = (dragon.State + 1) & 0xFF;
        if (dragon.State < DragonSwallow || !Touches(dragon))
        {
            return;
        }

        dragon.State = DragonEaten;
        StartNoise(NoiseEaten, NoiseDragon);
        if (dragon.X > DragonMaxX)
        {
            dragon.X = DragonMaxX;
        }

        if (dragon.Y <= DragonMinY)
        {
            dragon.Y = DragonMinY;
        }
    }

    private int RoarState()
    {
        var index = GameIndex * 2 + (ShortDragonRoar ? 1 : 0);
        return RoarStarts[index];
    }

    private const int BatStep = 3;
    private const int BatFedUpStart = 0x10;
    private const int BatCarryOffsetX = 0x08;
    private const int NoSkip = int.MinValue;

    private void AdvanceBat()
    {
        if (_disabled.Contains(ObjectIds.Bat))
        {
            return;
        }

        var bat = Find(ObjectIds.Bat);
        bat.State = (bat.State + 1) & 0xFF;
        if (bat.State == 8)
        {
            bat.State = 0;
        }

        var found = ObjectIds.None;
        if (BatFedUp == 0)
        {
            StepRoaming(bat, BatStep, BatQuarry, BatCargo, out found);
            if (found != ObjectIds.None && BatReaches(bat, Find(found)))
            {
                BatCargo = found;
                BatFedUp = BatFedUpStart;
            }
        }
        else
        {
            BatFedUp = (BatFedUp + 1) & 0xFF;
            StepRoaming(bat, BatStep, quarry: null, NoSkip, out _);
        }

        HoldCargo(bat);
    }

    private void AdvanceMagnet()
    {
        if (_disabled.Contains(ObjectIds.Magnet))
        {
            return;
        }

        var magnet = Find(ObjectIds.Magnet);
        var poleY = (magnet.Y - 8) & 0xFF;
        foreach (var id in MagnetQuarry)
        {
            var item = Find(id);
            if (item.Room != magnet.Room)
            {
                continue;
            }

            var saved = item.Movement;
            item.Movement = Toward(item.X, item.Y, magnet.X, poleY);
            StepRoaming(item, 1, quarry: null, NoSkip, out _);
            item.Movement = saved;
            return;
        }
    }

    private static bool BatReaches(WorldObject bat, WorldObject item) =>
        item.Room == bat.Room && WithinBat(item.X, bat.X) && WithinBat(item.Y, bat.Y);

    private static bool WithinBat(int objectCoordinate, int batCoordinate) =>
        (((objectCoordinate - batCoordinate + 4) & 0xFF) & 0xF8) == 0;

    private void HoldCargo(WorldObject bat)
    {
        if (BatCargo == ObjectIds.None)
        {
            return;
        }

        var item = Find(BatCargo);
        item.Room = bat.Room;
        item.X = (bat.X + BatCarryOffsetX) & 0xFF;
        item.Y = bat.Y;
        if (_carried?.Id == item.Id)
        {
            _carried = null;
        }
    }

    private void StepDragon(WorldObject dragon, int delta)
    {
        var skip = DragonsFearSword ? NoSkip : ObjectIds.Sword;
        StepRoaming(dragon, delta, Quarry(dragon.Id), skip, out _);
    }

    private void StepRoaming(
        WorldObject actor,
        int delta,
        IReadOnlyList<(int First, int Second)>? quarry,
        int skipId,
        out int foundId)
    {
        foundId = ObjectIds.None;
        var movement = actor.Movement;
        if (quarry is not null && TrySeek(quarry, skipId, out var sought, out foundId))
        {
            movement = sought;
            actor.Movement = sought;
        }

        var x = actor.X;
        var y = actor.Y;
        for (var step = 0; step < delta; step++)
        {
            if ((movement & 0x80) == 0)
            {
                x = (x + 1) & 0xFF;
            }

            if ((movement & 0x40) == 0)
            {
                x = (x - 1) & 0xFF;
            }

            if ((movement & 0x10) == 0)
            {
                y = (y + 1) & 0xFF;
            }

            if ((movement & 0x20) == 0)
            {
                y = (y - 1) & 0xFF;
            }
        }

        actor.X = x;
        actor.Y = y;
        WrapRoaming(actor);
    }

    private bool TrySeek(
        IReadOnlyList<(int First, int Second)> quarry,
        int skipId,
        out int movement,
        out int foundId)
    {
        foreach (var (first, second) in quarry)
        {
            if (first == skipId || second == skipId)
            {
                continue;
            }

            var from = Locate(first);
            var to = Locate(second);
            if (from.Room != to.Room)
            {
                continue;
            }

            movement = Toward(from.X, from.Y, to.X, to.Y);
            foundId = second;
            return true;
        }

        movement = 0;
        foundId = ObjectIds.None;
        return false;
    }

    private (int Room, int X, int Y) Locate(int id)
    {
        if (id == Ball)
        {
            return (Player.Room, Player.X, Player.Y);
        }

        var item = Find(id);
        return (item.Room, item.X, item.Y);
    }

    private static int Toward(int fromX, int fromY, int toX, int toY)
    {
        var movement = 0xFF;
        if (toX > fromX)
        {
            movement &= 0x7F;
        }
        else if (toX < fromX)
        {
            movement &= 0xBF;
        }

        if (toY > fromY)
        {
            movement &= 0xEF;
        }
        else if (toY < fromY)
        {
            movement &= 0xDF;
        }

        return movement;
    }

    private void WrapRoaming(WorldObject actor)
    {
        var room = _rooms[actor.Room];
        if (actor.Y >= MapGeometry.TopEdge)
        {
            actor.Y = MapGeometry.WrappedFromTopY;
            actor.Room = RoomCatalog.ResolveExit(room.Up, GameIndex);
            return;
        }

        if (actor.X < MapGeometry.LeftEdge || actor.X >= 0xF0)
        {
            actor.X = RoamingLeftX;
            actor.Room = RoomCatalog.ResolveExit(room.Left, GameIndex);
            return;
        }

        if (actor.Y < MapGeometry.BottomEdge)
        {
            actor.Y = MapGeometry.WrappedFromBottomY;
            actor.Room = RoomCatalog.ResolveExit(room.Down, GameIndex);
            return;
        }

        if (actor.X >= DragonMaxX)
        {
            actor.X = MapGeometry.WrappedFromRightX;
            actor.Room = RoomCatalog.ResolveExit(room.Right, GameIndex);
        }
    }

    private void PinEatenPlayer(WorldObject dragon)
    {
        Player.Room = dragon.Room;
        Player.X = (dragon.X + EatenOffsetX) & 0xFF;
        Player.Y = (dragon.Y - EatenOffsetY) & 0xFF;
        FollowCarried();
    }

    private static IReadOnlyList<(int First, int Second)> Quarry(int id) => id switch
    {
        ObjectIds.RedDragon => RedQuarry,
        ObjectIds.YellowDragon => YellowQuarry,
        _ => GreenQuarry,
    };

    private static readonly int[] RoarStarts = [0xD0, 0xE8, 0xF0, 0xF6, 0xF0, 0xF6];

    private static readonly (int First, int Second)[] RedQuarry =
    [
        (ObjectIds.Sword, ObjectIds.RedDragon),
        (ObjectIds.RedDragon, Ball),
        (ObjectIds.RedDragon, ObjectIds.Chalice),
        (ObjectIds.RedDragon, ObjectIds.WhiteKey),
    ];

    private static readonly (int First, int Second)[] YellowQuarry =
    [
        (ObjectIds.Sword, ObjectIds.YellowDragon),
        (ObjectIds.YellowKey, ObjectIds.YellowDragon),
        (ObjectIds.YellowDragon, Ball),
        (ObjectIds.YellowDragon, ObjectIds.Chalice),
    ];

    private static readonly (int First, int Second)[] GreenQuarry =
    [
        (ObjectIds.Sword, ObjectIds.GreenDragon),
        (ObjectIds.GreenDragon, Ball),
        (ObjectIds.GreenDragon, ObjectIds.Chalice),
        (ObjectIds.GreenDragon, ObjectIds.Bridge),
        (ObjectIds.GreenDragon, ObjectIds.Magnet),
        (ObjectIds.GreenDragon, ObjectIds.BlackKey),
    ];

    private static readonly (int First, int Second)[] BatQuarry =
    [
        (ObjectIds.Bat, ObjectIds.Chalice),
        (ObjectIds.Bat, ObjectIds.Sword),
        (ObjectIds.Bat, ObjectIds.Bridge),
        (ObjectIds.Bat, ObjectIds.YellowKey),
        (ObjectIds.Bat, ObjectIds.WhiteKey),
        (ObjectIds.Bat, ObjectIds.BlackKey),
        (ObjectIds.Bat, ObjectIds.RedDragon),
        (ObjectIds.Bat, ObjectIds.YellowDragon),
        (ObjectIds.Bat, ObjectIds.GreenDragon),
        (ObjectIds.Bat, ObjectIds.Magnet),
    ];

    private static readonly int[] MagnetQuarry =
    [
        ObjectIds.YellowKey,
        ObjectIds.WhiteKey,
        ObjectIds.BlackKey,
        ObjectIds.Sword,
        ObjectIds.Bridge,
        ObjectIds.Chalice,
    ];

    private readonly record struct GateLink(int GateId, int KeyId, int CastleRoom, int EntryRoom);

    private static readonly GateLink[] GateLinks =
    [
        new(ObjectIds.YellowPortcullis, ObjectIds.YellowKey, 0x11, 0x12),
        new(ObjectIds.WhitePortcullis, ObjectIds.WhiteKey, 0x0F, 0x1A),
        new(ObjectIds.BlackPortcullis, ObjectIds.BlackKey, 0x10, 0x1B),
    ];
}
