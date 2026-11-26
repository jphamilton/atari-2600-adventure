namespace Adventure.Core;

public static class ObjectSprites
{
    public const int BridgePixelScale = 4;

    public static ReadOnlySpan<byte> Bridge => BridgeGraphic;

    public static ReadOnlySpan<byte> Surround => SurroundGraphic;

    public static ReadOnlySpan<byte> Author => AuthorGraphic;

    public static ReadOnlySpan<byte> Number(int gameIndex) => gameIndex switch
    {
        0 => Number1,
        1 => Number2,
        _ => Number3,
    };

    public static ReadOnlySpan<byte> Gate(int state)
    {
        var start = state switch
        {
            < 0x08 => 12,
            < 0x0C => 10,
            < 0x10 => 8,
            < 0x14 => 6,
            < 0x18 => 4,
            < 0x1C => 2,
            < 0x20 => 0,
            < 0x24 => 2,
            < 0x28 => 4,
            < 0x2C => 6,
            < 0x30 => 8,
            < 0x34 => 10,
            _ => 12,
        };
        return GateGraphic.AsSpan(start);
    }

    public static ReadOnlySpan<byte> Dragon(int state) => state switch
    {
        0 or 2 => DragonNormal,
        1 => DragonDead,
        _ => DragonRoar,
    };

    public static ReadOnlySpan<byte> Bat(int flap) => flap <= 3 ? BatWingsUp : BatWingsDown;

    public static ReadOnlySpan<byte> For(ObjectKind kind, int state) => kind switch
    {
        ObjectKind.Dragon => Dragon(state),
        ObjectKind.Bat => Bat(state),
        ObjectKind.Sword => Sword,
        ObjectKind.Key => Key,
        ObjectKind.Dot => Dot,
        ObjectKind.Chalice => Chalice,
        ObjectKind.Magnet => Magnet,
        ObjectKind.Bridge => BridgeGraphic,
        ObjectKind.Portcullis => Gate(state),
        _ => [],
    };

    public static bool BridgeOverlaps(int bridgeX, int bridgeY, int playerX, int playerY) =>
        MarkerOverlaps(BridgeGraphic, bridgeX, bridgeY, BridgePixelScale, playerX, playerY);

    public static bool MarkerOverlaps(
        ReadOnlySpan<byte> graphic,
        int objectX,
        int objectY,
        int pixelScale,
        int playerX,
        int playerY)
    {
        var markerLeft = playerX;
        var markerRight = playerX + 4;
        var markerBottom = playerY - 4;
        var markerTop = playerY;

        for (var row = 0; row < graphic.Length; row++)
        {
            var pixelY = objectY - 1 - row;
            if (pixelY < markerBottom || pixelY >= markerTop)
            {
                continue;
            }

            if (RowHits(graphic[row], objectX, pixelScale, markerLeft, markerRight))
            {
                return true;
            }
        }

        return false;
    }

    public static bool SpritesOverlap(
        ReadOnlySpan<byte> left,
        int leftX,
        int leftY,
        int leftScale,
        ReadOnlySpan<byte> right,
        int rightX,
        int rightY,
        int rightScale)
    {
        for (var row = 0; row < left.Length; row++)
        {
            var bits = left[row];
            if (bits == 0)
            {
                continue;
            }

            var pixelY = leftY - 1 - row;
            var rightRow = rightY - 1 - pixelY;
            if (rightRow < 0 || rightRow >= right.Length)
            {
                continue;
            }

            for (var bit = 0; bit < 8; bit++)
            {
                if ((bits & (0x80 >> bit)) == 0)
                {
                    continue;
                }

                var pixelLeft = leftX + bit * leftScale;
                if (RowHits(right[rightRow], rightX, rightScale, pixelLeft, pixelLeft + leftScale))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool RowHits(byte bits, int objectX, int pixelScale, int spanLeft, int spanRight)
    {
        for (var bit = 0; bit < 8; bit++)
        {
            if ((bits & (0x80 >> bit)) == 0)
            {
                continue;
            }

            var pixelLeft = objectX + bit * pixelScale;
            if (pixelLeft < spanRight && pixelLeft + pixelScale > spanLeft)
            {
                return true;
            }
        }

        return false;
    }

    private static readonly byte[] BridgeGraphic =
    [
        0xC3, 0xC3, 0xC3, 0xC3,
        0x42, 0x42, 0x42, 0x42, 0x42, 0x42, 0x42, 0x42,
        0x42, 0x42, 0x42, 0x42, 0x42, 0x42, 0x42, 0x42,
        0xC3, 0xC3, 0xC3, 0xC3,
    ];

    private static readonly byte[] SurroundGraphic =
    [
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
    ];

    private static readonly byte[] GateGraphic =
    [
        0xFE, 0xAA, 0xFE, 0xAA, 0xFE, 0xAA, 0xFE, 0xAA,
        0xFE, 0xAA, 0xFE, 0xAA, 0xFE, 0xAA, 0xFE, 0xAA,
    ];

    private static readonly byte[] Key = [0x07, 0xFD, 0xA7];

    private static readonly byte[] Sword = [0x20, 0x40, 0xFF, 0x40, 0x20];

    private static readonly byte[] Dot = [0x80];

    private static readonly byte[] Chalice = [0x81, 0x81, 0xC3, 0x7E, 0x7E, 0x3C, 0x18, 0x18, 0x7E];

    private static readonly byte[] Magnet = [0x3C, 0x7E, 0xE7, 0xC3, 0xC3, 0xC3, 0xC3, 0xC3];

    private static readonly byte[] BatWingsUp = [0x81, 0x81, 0xC3, 0xC3, 0xFF, 0x5A, 0x66];

    private static readonly byte[] BatWingsDown = [0x01, 0x80, 0x01, 0x80, 0x3C, 0x5A, 0x66, 0xC3, 0x81, 0x81, 0x81];

    private static readonly byte[] DragonNormal =
    [
        0x06, 0x0F, 0xF3, 0xFE, 0x0E, 0x04, 0x04, 0x1E,
        0x3F, 0x7F, 0xE3, 0xC3, 0xC3, 0xC7, 0xFF, 0x3C,
        0x08, 0x8F, 0xE1, 0x3F,
    ];

    private static readonly byte[] DragonRoar =
    [
        0x80, 0x40, 0x26, 0x1F, 0x0B, 0x0E, 0x1E, 0x24,
        0x44, 0x8E, 0x1E, 0x3F, 0x7F, 0x7F, 0x7F, 0x7F,
        0x3E, 0x1C, 0x08, 0xF8, 0x80, 0xE0,
    ];

    private static readonly byte[] DragonDead =
    [
        0x0C, 0x0C, 0x0C, 0x0E, 0x1B, 0x7F, 0xCE, 0x80,
        0xFC, 0xFE, 0xFE, 0x7E, 0x78, 0x20, 0x6E, 0x42, 0x7E,
    ];

    private static readonly byte[] Number1 = [0x04, 0x0C, 0x04, 0x04, 0x04, 0x04, 0x0E];

    private static readonly byte[] Number2 = [0x0E, 0x11, 0x01, 0x02, 0x04, 0x08, 0x1F];

    private static readonly byte[] Number3 = [0x0E, 0x11, 0x01, 0x06, 0x01, 0x11, 0x0E];

    private static readonly byte[] AuthorGraphic =
    [
        0xF0, 0x80, 0x80, 0x80, 0xF4, 0x04, 0x87, 0xE5,
        0x87, 0x80, 0x05, 0xE5, 0xA7, 0xE1, 0x87, 0xE0,
        0x01, 0xE0, 0xA0, 0xF0, 0x01, 0x40, 0xE0, 0x40,
        0x40, 0x40, 0x01, 0xE0, 0xA0, 0xE0, 0x80, 0xE0,
        0x01, 0x20, 0x20, 0xE0, 0xA0, 0xE0, 0x01, 0x01,
        0x01, 0x88, 0xA8, 0xA8, 0xA8, 0xF8, 0x01, 0xE0,
        0xA0, 0xF0, 0x01, 0x80, 0xE0, 0x8F, 0x89, 0x0F,
        0x8A, 0xE9, 0x80, 0x8E, 0x0A, 0xEE, 0xA0, 0xE8,
        0x88, 0xEE, 0x0A, 0x8E, 0xE0, 0xA4, 0xA4, 0x04,
        0x80, 0x08, 0x0E, 0x0A, 0x0A, 0x80, 0x0E, 0x0A,
        0x0E, 0x08, 0x0E, 0x80, 0x04, 0x0E, 0x04, 0x04,
        0x04, 0x80, 0x04, 0x0E, 0x04, 0x04, 0x04,
    ];
}
