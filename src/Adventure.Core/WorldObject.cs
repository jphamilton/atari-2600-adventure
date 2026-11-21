namespace Adventure.Core;

public sealed class WorldObject
{
    public WorldObject(int id, ObjectKind kind, int room, int x, int y, byte color, int state = 0, int movement = 0)
    {
        Id = id;
        Kind = kind;
        Room = room;
        X = x;
        Y = y;
        Color = color;
        State = state;
        Movement = movement;
    }

    public int Id { get; }

    public ObjectKind Kind { get; }

    public int Room { get; set; }

    public int X { get; set; }

    public int Y { get; set; }

    public byte Color { get; }

    public int State { get; set; }

    public int Movement { get; set; }

    public bool IsCarriable => ObjectIds.IsCarriable(Id);
}
