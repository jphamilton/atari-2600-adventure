namespace Adventure.Core;

public sealed class PlaytestSetup
{
    public bool Ignore { get; init; }

    public required int GameIndex { get; init; }

    public required int PlayerRoom { get; init; }

    public required int PlayerX { get; init; }

    public required int PlayerY { get; init; }

    public int? Carrying { get; init; }

    public bool DragonsFearSword { get; init; }

    public bool ShortDragonRoar { get; init; }

    public IReadOnlyList<PlaytestObject> Objects { get; init; } = [];
}

public sealed class PlaytestObject
{
    public required int Id { get; init; }

    public int? Room { get; init; }

    public int? X { get; init; }

    public int? Y { get; init; }

    public int? State { get; init; }

    public bool Disabled { get; init; }
}
