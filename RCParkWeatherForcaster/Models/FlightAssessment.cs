namespace RCParkWeatherForcaster.Models;

public enum FlyingRating
{
    Go = 0,
    Caution = 1,
    NoGo = 2
}

public static class FlyingRatingExtensions
{
    public static string ToSymbol(this FlyingRating rating) => rating switch
    {
        FlyingRating.Go => "✅ GO",
        FlyingRating.Caution => "⚠️  CAUTION",
        FlyingRating.NoGo => "❌ NO-GO",
        _ => "?"
    };

    public static ConsoleColor ToColor(this FlyingRating rating) => rating switch
    {
        FlyingRating.Go => ConsoleColor.Green,
        FlyingRating.Caution => ConsoleColor.Yellow,
        FlyingRating.NoGo => ConsoleColor.Red,
        _ => ConsoleColor.White
    };
}

public class HourlyAssessment
{
    public DateTimeOffset StartTime { get; init; }
    public DateTimeOffset EndTime { get; init; }
    public FlyingRating Rating { get; init; }
    public List<string> Reasons { get; init; } = new();
    public PirateWeatherDataPoint Period { get; init; } = new();
    /// <summary>Largest plane that gets a GO for wind/gusts this hour. Null = no plane should fly.</summary>
    public PlaneSize? RecommendedMaxPlaneSize { get; init; }
}

public class FlightAssessment
{
    public string LocationName { get; init; } = string.Empty;
    public string NearestCity { get; init; } = string.Empty;
    public DateTimeOffset GeneratedAt { get; init; }
    public FlyingRating Next24hRating { get; init; }
    public FlyingRating Next48hRating { get; init; }
    /// <summary>Smallest recommended plane across the 24h window (conservative / worst-case).</summary>
    public PlaneSize? Recommended24hMaxPlaneSize { get; init; }
    /// <summary>Smallest recommended plane across the 48h window.</summary>
    public PlaneSize? Recommended48hMaxPlaneSize { get; init; }
    public List<HourlyAssessment> HourlyAssessments { get; init; } = new();
    public List<string> ActiveAlertSummaries { get; init; } = new();
}
