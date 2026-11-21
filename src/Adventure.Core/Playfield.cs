namespace Adventure.Core;

public static class Playfield
{
    public static bool[,] Decode(ReadOnlySpan<byte> graphics, bool reflect)
    {
        if (graphics.Length != MapGeometry.Rows * 3)
        {
            throw new ArgumentException("A room graphic is 7 rows of PF0, PF1, and PF2.", nameof(graphics));
        }

        var walls = new bool[MapGeometry.Rows, MapGeometry.Columns];
        Span<bool> left = stackalloc bool[20];
        for (var row = 0; row < MapGeometry.Rows; row++)
        {
            var offset = row * 3;
            DecodeLeft(graphics[offset], graphics[offset + 1], graphics[offset + 2], left);
            for (var col = 0; col < 20; col++)
            {
                walls[row, col] = left[col];
                walls[row, col + 20] = reflect ? left[19 - col] : left[col];
            }
        }

        return walls;
    }

    private static void DecodeLeft(byte pf0, byte pf1, byte pf2, Span<bool> left)
    {
        left[0] = (pf0 & 0x10) != 0;
        left[1] = (pf0 & 0x20) != 0;
        left[2] = (pf0 & 0x40) != 0;
        left[3] = (pf0 & 0x80) != 0;
        for (var i = 0; i < 8; i++)
        {
            left[4 + i] = (pf1 & (0x80 >> i)) != 0;
            left[12 + i] = (pf2 & (1 << i)) != 0;
        }
    }
}
