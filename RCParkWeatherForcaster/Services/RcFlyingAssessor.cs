using Microsoft.Extensions.Options;
using RCParkWeatherForcaster.Models;

namespace RCParkWeatherForcaster.Services;

public interface IRcFlyingAssessor
{
    FlightAssessment Assess(
        PirateWeatherResponse response,
        string locationName,
        string nearestCity,
        AppSettings settings);
}

public class RcFlyingAssessor : IRcFlyingAssessor
{
    // Forecast text keywords that are an outright No-Go
    private static readonly string[] NoGoKeywords =
    {
        "thunder", "lightning", "tornado", "hurricane", "tropical storm",
        "rain", "drizzle", "sleet", "hail", "snow", "blizzard", "ice",
        "fog", "freezing", "smoke", "dust storm"
    };

    // Forecast text keywords that warrant Caution
    private static readonly string[] CautionKeywords =
    {
        "overcast", "cloudy", "mostly cloudy", "haze", "breezy", "windy", "mist"
    };

    public FlightAssessment Assess(
        PirateWeatherResponse weather,
        string locationName,
        string nearestCity,
        AppSettings settings)
    {
        var thresholds = settings.Thresholds;
        var periods = weather.Hourly?.Data ?? new List<PirateWeatherDataPoint>();
        var now = DateTimeOffset.UtcNow;

        // Limit to the configured window (24 or 48 hours)
        var windowEnd = now.AddHours(settings.ForecastHours);
        var relevantPeriods = periods
            .Where(p => DateTimeOffset.FromUnixTimeSeconds(p.Time) >= now && DateTimeOffset.FromUnixTimeSeconds(p.Time) < windowEnd)
            .Take(settings.ForecastHours)  // safety cap
            .ToList();

        // Assess alert impact
        var alertSummaries = new List<string>();
        var alertRating = FlyingRating.Go;
        if (weather.Alerts != null)
        {
            foreach (var alert in weather.Alerts)
            {
                var summary = alert.Title ?? "Alert";
                alertSummaries.Add(summary);
                // We'll consider any alert at least a Caution for now, but in reality 
                // you could parse the severity if provided.
                if (alertRating < FlyingRating.Caution)
                    alertRating = FlyingRating.Caution;
            }
        }

        // Resolve effective wind/gust limits (plane profile + any manual overrides)
        var (goWind, cautionWind, goGust, cautionGust) = settings.GetEffectiveWindLimits();

        // Assess each hourly period
        var hourlyAssessments = new List<HourlyAssessment>();
        foreach (var period in relevantPeriods)
        {
            var assessment = AssessPeriod(period, alertRating, settings.Thresholds,
                goWind, cautionWind, goGust, cautionGust);
            hourlyAssessments.Add(assessment);
        }

        // Window summaries: worst rating in first 24h / full 48h
        var end24h = now.AddHours(24);
        var next24hRating = hourlyAssessments
            .Where(a => a.StartTime < end24h)
            .Select(a => a.Rating)
            .DefaultIfEmpty(FlyingRating.Go)
            .Max();

        var next48hRating = hourlyAssessments
            .Select(a => a.Rating)
            .DefaultIfEmpty(FlyingRating.Go)
            .Max();

        // Recommended max plane size: the smallest (most conservative) recommendation across each window
        // A null in any slot means "no plane" — that trumps everything.
        var rec24h = MinPlaneSize(hourlyAssessments
            .Where(a => a.StartTime < end24h)
            .Select(a => a.RecommendedMaxPlaneSize));

        var rec48h = MinPlaneSize(hourlyAssessments
            .Select(a => a.RecommendedMaxPlaneSize));

        return new FlightAssessment
        {
            LocationName = locationName,
            NearestCity = nearestCity,
            GeneratedAt = now,
            Next24hRating = next24hRating,
            Next48hRating = next48hRating,
            Recommended24hMaxPlaneSize = rec24h,
            Recommended48hMaxPlaneSize = rec48h,
            HourlyAssessments = hourlyAssessments,
            ActiveAlertSummaries = alertSummaries
        };
    }

    /// <summary>Returns the smallest (most conservative) PlaneSize across a sequence of nullable sizes.
    /// Any null in the sequence means no plane is recommended → returns null.</summary>
    private static PlaneSize? MinPlaneSize(IEnumerable<PlaneSize?> sizes)
    {
        PlaneSize? min = PlaneSize.GiantScale;
        foreach (var s in sizes)
        {
            if (s is null) return null;          // even one null kills the window
            if ((int)s.Value < (int)min!.Value)
                min = s;
        }
        return min;
    }

    private static HourlyAssessment AssessPeriod(
        PirateWeatherDataPoint period,
        FlyingRating baseAlertRating,
        FlyingThresholds thresholds,
        double goWindMph,
        double noGoWindMph,
        double goGustMph,
        double noGoGustMph)
    {
        var rating = baseAlertRating;
        var reasons = new List<string>();

        // --- Wind speed
        var windMph = period.WindSpeed;
        var windRating = windMph switch
        {
            var w when w > noGoWindMph  => FlyingRating.NoGo,
            var w when w > goWindMph    => FlyingRating.Caution,
            _ => FlyingRating.Go
        };
        if (windRating == FlyingRating.NoGo)
            reasons.Add($"Wind {windMph:F0} mph (limit: {noGoWindMph:F0} mph)");
        else if (windRating == FlyingRating.Caution)
            reasons.Add($"Wind {windMph:F0} mph (caution above {goWindMph:F0} mph)");
        if (windRating > rating) rating = windRating;

        // --- Gusts
        var gustMph = period.WindGust;
        if (gustMph > 0)
        {
            var gustRating = gustMph switch
            {
                var g when g > noGoGustMph  => FlyingRating.NoGo,
                var g when g > goGustMph    => FlyingRating.Caution,
                _ => FlyingRating.Go
            };
            if (gustRating == FlyingRating.NoGo)
                reasons.Add($"Gusts {gustMph:F0} mph (limit: {noGoGustMph:F0} mph)");
            else if (gustRating == FlyingRating.Caution)
                reasons.Add($"Gusts {gustMph:F0} mph (caution above {goGustMph:F0} mph)");
            if (gustRating > rating) rating = gustRating;
        }

        // --- Precipitation probability
        var precip = period.PrecipProbability * 100.0; // Pirate Weather gives 0 to 1
        var precipRating = precip switch
        {
            var p when p > thresholds.CautionPrecipProbabilityPercent => FlyingRating.NoGo,
            var p when p > thresholds.MaxPrecipProbabilityPercent => FlyingRating.Caution,
            _ => FlyingRating.Go
        };
        if (precipRating == FlyingRating.NoGo)
            reasons.Add($"Precip {precip:F0}% (limit: {thresholds.CautionPrecipProbabilityPercent}%)");
        else if (precipRating == FlyingRating.Caution)
            reasons.Add($"Precip {precip:F0}% (caution above {thresholds.MaxPrecipProbabilityPercent}%)");
        if (precipRating > rating) rating = precipRating;

        // --- Temperature
        var tempF = period.Temperature;
        FlyingRating tempRating;
        if (tempF < thresholds.CautionMinTempF || tempF > thresholds.CautionMaxTempF)
        {
            tempRating = FlyingRating.NoGo;
            reasons.Add($"Temp {tempF:F0}°F (limits: {thresholds.CautionMinTempF}–{thresholds.CautionMaxTempF}°F)");
        }
        else if (tempF < thresholds.MinTempF || tempF > thresholds.MaxTempF)
        {
            tempRating = FlyingRating.Caution;
            reasons.Add($"Temp {tempF:F0}°F (ideal: {thresholds.MinTempF}–{thresholds.MaxTempF}°F)");
        }
        else
        {
            tempRating = FlyingRating.Go;
        }
        if (tempRating > rating) rating = tempRating;

        // --- Forecast text analysis
        var forecast = (period.Summary ?? string.Empty).ToLowerInvariant();
        var iconStr = (period.Icon ?? string.Empty).ToLowerInvariant();

        foreach (var kw in NoGoKeywords)
        {
            if (forecast.Contains(kw) || iconStr.Contains(kw))
            {
                if (FlyingRating.NoGo > rating) rating = FlyingRating.NoGo;
                reasons.Add($"Forecast: \"{period.Summary}\"");
                break;
            }
        }

        if (rating < FlyingRating.NoGo)
        {
            foreach (var kw in CautionKeywords)
            {
                if (forecast.Contains(kw) || iconStr.Contains(kw))
                {
                    if (FlyingRating.Caution > rating) rating = FlyingRating.Caution;
                    reasons.Add($"Forecast: \"{period.Summary}\"");
                    break;
                }
            }
        }

        if (reasons.Count == 0 && rating == FlyingRating.Go)
            reasons.Add("All conditions nominal");

        // Determine largest plane that gets GO for wind/gusts (independent of other factors)
        var recommendedSize = PlaneSizeProfile.GetLargestGoSize(period.WindSpeed, period.WindGust);

        var startTime = DateTimeOffset.FromUnixTimeSeconds(period.Time);
        return new HourlyAssessment
        {
            StartTime = startTime,
            EndTime = startTime.AddHours(1),
            Rating = rating,
            Reasons = reasons,
            Period = period,
            RecommendedMaxPlaneSize = recommendedSize
        };
    }
}
