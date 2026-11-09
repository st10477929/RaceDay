namespace RaceDay.Web.Services;

public static class SessionKeys
{
    public const string UserId = "UserId";
    public const string Role = "Role";
    public const string FullName = "FullName";
    public const string ApiCookie = "ApiCookie";
    public const string ApiExpired = "ApiExpired";
}

public static class RoleNames
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";
}

public static class UiHelpers
{
    public static string Ordinal(int n)
    {
        var suffix = (n % 100 is >= 11 and <= 13) ? "th" : (n % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" };
        return n + suffix;
    }

    /// <summary>Colour-coded enrolment status: green Confirmed, amber Pending, red Cancelled.</summary>
    public static string StatusBadge(string? status) => (status ?? "").ToLowerInvariant() switch
    {
        "confirmed" => "text-bg-success",
        "pending" => "text-bg-warning",
        "cancelled" or "canceled" => "text-bg-danger",
        _ => "text-bg-secondary"
    };

    public static string Time(TimeSpan? t) => t is null ? "DNF" : t.Value.ToString(@"hh\:mm\:ss");
}
