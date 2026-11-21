namespace Adventure.Core;

public static class ObjectLayout
{
    public static IReadOnlyList<WorldObject> ForGame(int gameIndex)
    {
        var game1 = gameIndex == 0;
        var objects = new List<WorldObject>
        {
            Portcullis(ObjectIds.YellowPortcullis, room: 0x11),
            Portcullis(ObjectIds.WhitePortcullis, room: 0x0F),
            Portcullis(ObjectIds.BlackPortcullis, room: 0x10),
            Dragon(ObjectIds.RedDragon, game1 ? 0x0E : 0x14, 0x36, game1 ? 0x00 : 0xA0),
            Dragon(ObjectIds.YellowDragon, game1 ? 0x01 : 0x19, 0x1A, game1 ? 0x00 : 0xA0),
            Dragon(ObjectIds.GreenDragon, game1 ? 0x1D : 0x04, 0xC8, game1 ? 0x00 : 0xA0),
            Item(ObjectIds.Sword, ObjectKind.Sword, game1 ? 0x12 : 0x11, 0x20, 0x20, 0x1A),
            Item(ObjectIds.Bridge, ObjectKind.Bridge, game1 ? 0x04 : 0x0B, game1 ? 0x29 : 0x40, game1 ? 0x37 : 0x40, 0x66),
            Item(ObjectIds.YellowKey, ObjectKind.Key, game1 ? 0x11 : 0x09, 0x20, 0x40, 0x1A),
            Item(ObjectIds.WhiteKey, ObjectKind.Key, game1 ? 0x0E : 0x06, 0x20, 0x40, 0x0E),
            Item(ObjectIds.BlackKey, ObjectKind.Key, game1 ? 0x1D : 0x19, 0x20, 0x40, 0x00),
            new WorldObject(ObjectIds.Bat, ObjectKind.Bat, game1 ? 0x1A : 0x02, 0x20, 0x20, 0x00, movement: game1 ? 0x00 : 0x90),
            Item(ObjectIds.Dot, ObjectKind.Dot, 0x15, 0x51, 0x12, 0x08),
            Item(ObjectIds.Chalice, ObjectKind.Chalice, game1 ? 0x1C : 0x14, 0x30, 0x20, 0xCB),
            Item(ObjectIds.Magnet, ObjectKind.Magnet, game1 ? 0x1B : 0x0E, 0x80, 0x20, 0x00),
        };

        objects.Sort((left, right) => left.Id.CompareTo(right.Id));
        return objects;
    }

    private static WorldObject Portcullis(int id, int room) =>
        new(id, ObjectKind.Portcullis, room, 0x4D, 0x31, 0x00, state: 0x1C);

    private static WorldObject Dragon(int id, int room, byte color, int movement) =>
        new(id, ObjectKind.Dragon, room, 0x50, 0x20, color, movement: movement);

    private static WorldObject Item(int id, ObjectKind kind, int room, int x, int y, byte color) =>
        new(id, kind, room, x, y, color);
}
