namespace Adventure.Core;

public static class MapGeometry
{
    public const int Columns = 40;
    public const int Rows = 7;
    public const int CellWidth = 4;
    public const int CellHeight = 16;
    public const int PixelWidth = Columns * CellWidth;
    public const int PixelHeight = Rows * CellHeight;

    public const int ScanlineTop = 0x68;
    public const int ScanlineBottom = 0x08;
    public const int VisibleHeight = ScanlineTop - ScanlineBottom + 1;

    public const int ScreenTop = 0x70;
    public const int LeftThinX = 0x0D;
    public const int RightThinX = 0x96;

    public const int TopEdge = 0x6A;
    public const int BottomEdge = 0x0D;
    public const int LeftEdge = 0x03;
    public const int RightEdge = 0x9F;

    public const int WrappedFromTopY = 0x0D;
    public const int WrappedFromBottomY = 0x69;
    public const int WrappedFromLeftX = 0x9E;
    public const int WrappedFromRightX = 0x03;

    public static int RowForY(int y) => (ScreenTop - y) / CellHeight;

    public static int RowForScanline(int scanline)
    {
        if (scanline > ScanlineTop || scanline < ScanlineBottom)
        {
            return -1;
        }

        if (scanline >= 0x61)
        {
            return 0;
        }

        return (0x60 - scanline) / CellHeight + 1;
    }

    public static int ColumnForX(int x) => x / CellWidth;

    public static int ScreenRow(int scanline) => ScanlineTop - scanline;

    public static (int Top, int Height) RowSpan(int row)
    {
        var scanlineTop = row == 0 ? ScanlineTop : 0x60 - (row - 1) * CellHeight;
        var scanlineBottom = row == Rows - 1 ? ScanlineBottom : scanlineTop - (row == 0 ? 7 : CellHeight - 1);
        return (ScreenRow(scanlineTop), scanlineTop - scanlineBottom + 1);
    }
}
