using System.Text.Json.Serialization;

namespace RCParkWeatherForcaster.Models;

public class AlertsResponse
{
    [JsonPropertyName("features")]
    public List<AlertFeatureWrapper> Features { get; set; } = new();
}

public class AlertFeatureWrapper
{
    [JsonPropertyName("properties")]
    public AlertProperties? Properties { get; set; }
}

public class AlertProperties
{
    [JsonPropertyName("event")]
    public string Event { get; set; } = string.Empty;

    [JsonPropertyName("severity")]
    public string Severity { get; set; } = string.Empty;

    [JsonPropertyName("urgency")]
    public string Urgency { get; set; } = string.Empty;

    [JsonPropertyName("headline")]
    public string? Headline { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("effective")]
    public DateTimeOffset? Effective { get; set; }

    [JsonPropertyName("expires")]
    public DateTimeOffset? Expires { get; set; }

    /// <summary>
    /// Returns the flying impact rating based on NWS severity/urgency.
    /// Warning/Extreme/Severe → No-Go; Watch/Advisory/Moderate → Caution; Minor → Go.
    /// </summary>
    public FlyingRating GetFlyingImpact()
    {
        var sev = Severity?.ToLowerInvariant() ?? "";
        var eventLower = Event?.ToLowerInvariant() ?? "";

        // Direct No-Go: warnings, extreme/severe severity
        if (sev is "extreme" or "severe") return FlyingRating.NoGo;
        if (eventLower.Contains("warning") || eventLower.Contains("emergency"))
            return FlyingRating.NoGo;

        // Caution: watches, advisories, moderate severity
        if (sev is "moderate") return FlyingRating.Caution;
        if (eventLower.Contains("watch") || eventLower.Contains("advisory"))
            return FlyingRating.Caution;

        return FlyingRating.Go;
    }
}
