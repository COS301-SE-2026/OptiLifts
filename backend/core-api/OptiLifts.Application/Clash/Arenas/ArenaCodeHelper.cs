namespace OptiLifts.Application.Clash.Arenas;

public static class ArenaCodeHelper
{
    private const string AllowedChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    public static string GenerateCode(int length = 6)
    {
        return new string(Random.Shared.GetItems(AllowedChars.AsSpan(), length));
    }

    public static string NormalizeCode(string? code)
    {
        return (code ?? string.Empty).Trim().ToUpperInvariant();
    }
}
