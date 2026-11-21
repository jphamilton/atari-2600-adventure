namespace Adventure.Core;

public sealed class Actor
{
    public Actor(ObjectKind kind, int room, int x, int y)
    {
        Kind = kind;
        Room = room;
        X = x;
        Y = y;
    }

    public ObjectKind Kind { get; }

    public int Room { get; set; }

    public int X { get; set; }

    public int Y { get; set; }
}
