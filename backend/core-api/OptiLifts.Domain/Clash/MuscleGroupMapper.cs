namespace OptiLifts.Domain.Clash;

public static class MuscleGroupMapper
{
    public const string Chest = "Chest";
    public const string Core = "Core";
    public const string Shoulders = "Shoulders";
    public const string Arms = "Arms";
    public const string Legs = "Legs";
    public const string Back = "Back";

    public static readonly IReadOnlyList<string> RadarGroups = new[] { Chest, Core, Shoulders, Arms, Legs, Back };

    private static readonly Dictionary<string, string> MuscleToGroup = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Chest"] = Chest,
        ["Abdominals"] = Core,
        ["Obliques"] = Core,
        ["Shoulders"] = Shoulders,
        ["Front Deltoid"] = Shoulders,
        ["Middle Deltoid"] = Shoulders,
        ["Rear Deltoid"] = Shoulders,
        ["Biceps"] = Arms,
        ["Triceps"] = Arms,
        ["Forearms"] = Arms,
        ["Quadriceps"] = Legs,
        ["Hamstrings"] = Legs,
        ["Glutes"] = Legs,
        ["Calves"] = Legs,
        ["Abductors"] = Legs,
        ["Adductors"] = Legs,
        ["Lats"] = Back,
        ["Lower Back"] = Back,
        ["Middle Back"] = Back,
        ["Upper Back"] = Back,
        ["Trapezius"] = Back
    };

    public static string? GetRadarGroup(string muscleName)
    {
        return MuscleToGroup.TryGetValue(muscleName, out var group) ? group : null;
    }
}
