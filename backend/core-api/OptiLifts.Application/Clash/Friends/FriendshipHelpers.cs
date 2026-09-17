//my attempt at avoiding duplication cus ive learnt my lesson
namespace OptiLifts.Application.Clash.Friends;

public static class FriendshipHelpers{
    public const string statusPending = "Pending";
    public const string statusAccepted = "Accepted";
    public const string statusRejected = "Rejected";

    public static (Guid UserId1, Guid UserId2) toCanonOrder(Guid a, Guid b){
        return a.CompareTo(b) < 0 ? (a,b): (b,a);
    }
    public static string extractInitials(string? displayName){
        if (string.IsNullOrWhiteSpace(displayName)){
            return "??";
        }
        var parts = displayName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch{
            0 => "??",
            1 => parts[0].Length >= 2 ? parts[0][..2].ToUpperInvariant() : parts[0].ToUpperInvariant(),
            _ => $"{char.ToUpperInvariant(parts[0][0])}{char.ToUpperInvariant(parts[^1][0])}"
        };
    }
}