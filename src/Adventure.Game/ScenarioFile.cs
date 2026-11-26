using Adventure.Core;

namespace Adventure.Game;

public static class ScenarioFile
{
    public const string FileName = "scenario.json";

    public static GameWorld LoadWorld()
    {
        var path = Locate();
        if (path is null)
        {
            return GameWorld.AtTitle();
        }

        try
        {
            return Scenario.Create(File.ReadAllText(path));
        }
        catch (FormatException exception)
        {
            throw new FormatException($"{FileName} at {path} is invalid: {exception.Message}", exception);
        }
    }

    private static string? Locate()
    {
        var fromCurrent = Path.Combine(Directory.GetCurrentDirectory(), FileName);
        if (File.Exists(fromCurrent))
        {
            return fromCurrent;
        }

        var fromExecutable = Path.Combine(AppContext.BaseDirectory, FileName);
        if (File.Exists(fromExecutable))
        {
            return fromExecutable;
        }

        return null;
    }
}
