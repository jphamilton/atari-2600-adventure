namespace Adventure.Core;

public sealed class Room
{
    public Room(
        int id,
        string name,
        byte color,
        byte control,
        int up,
        int right,
        int down,
        int left,
        bool[,] walls)
    {
        Id = id;
        Name = name;
        Color = color;
        Up = up;
        Right = right;
        Down = down;
        Left = left;
        Walls = walls;
        LeftThinWall = (control & 0x80) != 0;
        RightThinWall = (control & 0x40) != 0;
    }

    public int Id { get; }

    public string Name { get; }

    public byte Color { get; }

    public int Up { get; }

    public int Right { get; }

    public int Down { get; }

    public int Left { get; }

    public bool LeftThinWall { get; }

    public bool RightThinWall { get; }

    public bool[,] Walls { get; }

    public bool IsWall(int row, int column) => Walls[row, column];
}
