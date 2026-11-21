namespace Adventure.Core;

public static class ObjectIds
{
    public const int Surround = 0x00;
    public const int YellowPortcullis = 0x09;
    public const int WhitePortcullis = 0x12;
    public const int BlackPortcullis = 0x1B;
    public const int RedDragon = 0x36;
    public const int YellowDragon = 0x3F;
    public const int GreenDragon = 0x48;
    public const int Sword = 0x51;
    public const int Bridge = 0x5A;
    public const int YellowKey = 0x63;
    public const int WhiteKey = 0x6C;
    public const int BlackKey = 0x75;
    public const int Bat = 0x7E;
    public const int Dot = 0x87;
    public const int Chalice = 0x90;
    public const int Magnet = 0x99;
    public const int None = 0xA2;

    public static bool IsCarriable(int id) => id >= Sword && id != None;
}
