using System.Globalization;
using System.Text.Json;

namespace Adventure.Core;

public static class Scenario
{
    private static readonly JsonDocumentOptions JsonOptions = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private static readonly Dictionary<string, int> ObjectNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["yellow_portcullis"] = ObjectIds.YellowPortcullis,
        ["white_portcullis"] = ObjectIds.WhitePortcullis,
        ["black_portcullis"] = ObjectIds.BlackPortcullis,
        ["red_dragon"] = ObjectIds.RedDragon,
        ["yellow_dragon"] = ObjectIds.YellowDragon,
        ["green_dragon"] = ObjectIds.GreenDragon,
        ["sword"] = ObjectIds.Sword,
        ["bridge"] = ObjectIds.Bridge,
        ["yellow_key"] = ObjectIds.YellowKey,
        ["white_key"] = ObjectIds.WhiteKey,
        ["black_key"] = ObjectIds.BlackKey,
        ["bat"] = ObjectIds.Bat,
        ["dot"] = ObjectIds.Dot,
        ["chalice"] = ObjectIds.Chalice,
        ["magnet"] = ObjectIds.Magnet,
    };

    public static GameWorld Create(string json)
    {
        var setup = Parse(json);
        return setup.Ignore ? GameWorld.AtTitle() : GameWorld.ForPlaytest(setup);
    }

    public static PlaytestSetup Parse(string json)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new FormatException(exception.Message, exception);
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new FormatException("The scenario must be a JSON object.");
            }

            var ignore = false;
            var gameIndex = 0;
            var playerRoom = RoomCatalog.YellowCastle;
            var playerX = GameWorld.StartX;
            var playerY = GameWorld.StartY;
            int? carrying = null;
            var dragonsFearSword = false;
            var shortDragonRoar = false;
            var objects = new List<PlaytestObject>();
            foreach (var property in document.RootElement.EnumerateObject())
            {
                switch (property.Name)
                {
                    case "ignore":
                        ignore = Flag(property.Value, property.Name);
                        break;
                    case "game":
                        gameIndex = GameIndex(property.Value);
                        break;
                    case "player":
                        (playerRoom, playerX, playerY) = Player(property.Value, playerRoom, playerX, playerY);
                        break;
                    case "carrying":
                        carrying = Carried(property.Value);
                        break;
                    case "dragons_fear_sword":
                        dragonsFearSword = Flag(property.Value, property.Name);
                        break;
                    case "short_dragon_roar":
                        shortDragonRoar = Flag(property.Value, property.Name);
                        break;
                    case "objects":
                        objects = Objects(property.Value);
                        break;
                    default:
                        throw new FormatException($"Unknown property \"{property.Name}\".");
                }
            }

            if (carrying is int carriedId && objects.Any(item => item.Id == carriedId && (item.Room is not null || item.X is not null || item.Y is not null)))
            {
                throw new FormatException("carrying places that object on the player. Remove its room, x, and y.");
            }

            return new PlaytestSetup
            {
                Ignore = ignore,
                GameIndex = gameIndex,
                PlayerRoom = playerRoom,
                PlayerX = playerX,
                PlayerY = playerY,
                Carrying = carrying,
                DragonsFearSword = dragonsFearSword,
                ShortDragonRoar = shortDragonRoar,
                Objects = objects,
            };
        }
    }

    private static int GameIndex(JsonElement value)
    {
        var game = Integer(value, "game");
        if (game is < 1 or > 3)
        {
            throw new FormatException("game must be 1, 2, or 3.");
        }

        return game - 1;
    }

    private static (int Room, int X, int Y) Player(JsonElement value, int room, int x, int y)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            throw new FormatException("player must be an object.");
        }

        foreach (var property in value.EnumerateObject())
        {
            switch (property.Name)
            {
                case "room":
                    room = Room(property.Value, "player.room");
                    break;
                case "x":
                    x = Coordinate(property.Value, "player.x");
                    break;
                case "y":
                    y = Coordinate(property.Value, "player.y");
                    break;
                default:
                    throw new FormatException($"Unknown property \"player.{property.Name}\".");
            }
        }

        return (room, x, y);
    }

    private static int Carried(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.String)
        {
            throw new FormatException("carrying must be an object name.");
        }

        var id = ObjectId(value.GetString()!, "carrying");
        if (!ObjectIds.IsCarriable(id))
        {
            throw new FormatException($"The player cannot carry {value.GetString()}.");
        }

        return id;
    }

    private static List<PlaytestObject> Objects(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            throw new FormatException("objects must be an object.");
        }

        var objects = new List<PlaytestObject>();
        foreach (var property in value.EnumerateObject())
        {
            objects.Add(ObjectPatch(property.Name, property.Value));
        }

        return objects;
    }

    private static PlaytestObject ObjectPatch(string name, JsonElement value)
    {
        var id = ObjectId(name, "objects");
        if (value.ValueKind != JsonValueKind.Object)
        {
            throw new FormatException($"objects.{name} must be an object.");
        }

        int? room = null;
        int? x = null;
        int? y = null;
        int? state = null;
        var disabled = false;
        foreach (var property in value.EnumerateObject())
        {
            switch (property.Name)
            {
                case "room":
                    room = Room(property.Value, $"objects.{name}.room");
                    break;
                case "x":
                    x = Coordinate(property.Value, $"objects.{name}.x");
                    break;
                case "y":
                    y = Coordinate(property.Value, $"objects.{name}.y");
                    break;
                case "state":
                    state = State(id, name, property.Value);
                    break;
                case "enabled":
                    disabled = !Flag(property.Value, $"objects.{name}.enabled");
                    break;
                default:
                    throw new FormatException($"Unknown property \"objects.{name}.{property.Name}\".");
            }
        }

        if (disabled && !CanDisable(id))
        {
            throw new FormatException($"objects.{name} cannot be disabled. Only the bat, the magnet, and the dragons can.");
        }

        return new PlaytestObject
        {
            Id = id,
            Room = room,
            X = x,
            Y = y,
            State = state,
            Disabled = disabled,
        };
    }

    private static int State(int id, string name, JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.String)
        {
            throw new FormatException($"objects.{name}.state must be a string.");
        }

        var state = value.GetString()!;
        if (IsDragon(id))
        {
            return state.ToLowerInvariant() switch
            {
                "chase" => GameWorld.DragonChase,
                "dead" => GameWorld.DragonDead,
                "eaten" => GameWorld.DragonEaten,
                _ => throw new FormatException($"objects.{name}.state must be \"chase\", \"dead\", or \"eaten\"."),
            };
        }

        if (IsPortcullis(id))
        {
            return state.ToLowerInvariant() switch
            {
                "open" => GameWorld.GateOpen,
                "closed" => GameWorld.GateClosed,
                _ => throw new FormatException($"objects.{name}.state must be \"open\" or \"closed\"."),
            };
        }

        throw new FormatException($"objects.{name} has no state. State is for dragons and portcullises.");
    }

    private static int ObjectId(string name, string label)
    {
        if (ObjectNames.TryGetValue(name, out var id))
        {
            return id;
        }

        throw new FormatException($"Unknown object \"{name}\" in {label}.");
    }

    private static int Room(JsonElement value, string label)
    {
        if (value.ValueKind == JsonValueKind.Number)
        {
            return CheckRoom(Integer(value, label), label);
        }

        if (value.ValueKind != JsonValueKind.String)
        {
            throw new FormatException($"{label} must be a room name or id.");
        }

        var text = value.GetString() ?? "";
        if (LooksLikeId(text))
        {
            return CheckRoom(ParseInt(text, label), label);
        }

        var matches = RoomCatalog.Rooms.Where(room => room.Name.Equals(text, StringComparison.OrdinalIgnoreCase)).ToList();
        if (matches.Count == 1)
        {
            return matches[0].Id;
        }

        if (matches.Count == 0)
        {
            throw new FormatException($"Unknown room \"{text}\".");
        }

        var ids = string.Join(" and ", matches.Select(room => $"0x{room.Id:X2}"));
        throw new FormatException($"\"{text}\" is more than one room ({ids}). Use the hex id.");
    }

    private static int CheckRoom(int id, string label)
    {
        if (id < 0 || id >= RoomCatalog.Rooms.Count)
        {
            throw new FormatException($"{label} room {id} is outside the map.");
        }

        return id;
    }

    private static int Coordinate(JsonElement value, string label)
    {
        var coordinate = value.ValueKind == JsonValueKind.String
            ? ParseInt(value.GetString() ?? "", label)
            : Integer(value, label);
        if (coordinate is < 0 or > 255)
        {
            throw new FormatException($"{label} must be from 0 through 255.");
        }

        return coordinate;
    }

    private static int Integer(JsonElement value, string label)
    {
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out var number))
        {
            throw new FormatException($"{label} must be an integer.");
        }

        return number;
    }

    private static int ParseInt(string text, string label)
    {
        var hex = text.StartsWith("0x", StringComparison.OrdinalIgnoreCase);
        var body = hex ? text[2..] : text;
        var style = hex ? NumberStyles.HexNumber : NumberStyles.Integer;
        if (!int.TryParse(body, style, CultureInfo.InvariantCulture, out var number))
        {
            throw new FormatException($"{label} must be an integer or a room name.");
        }

        return number;
    }

    private static bool LooksLikeId(string text) =>
        text.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
        || (text.Length > 0 && text.All(char.IsAsciiDigit));

    private static bool Flag(JsonElement value, string label)
    {
        if (value.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            return value.GetBoolean();
        }

        throw new FormatException($"{label} must be true or false.");
    }

    private static bool CanDisable(int id) =>
        id is ObjectIds.Bat or ObjectIds.Magnet or ObjectIds.RedDragon or ObjectIds.YellowDragon or ObjectIds.GreenDragon;

    private static bool IsDragon(int id) =>
        id is ObjectIds.RedDragon or ObjectIds.YellowDragon or ObjectIds.GreenDragon;

    private static bool IsPortcullis(int id) =>
        id is ObjectIds.YellowPortcullis or ObjectIds.WhitePortcullis or ObjectIds.BlackPortcullis;
}
