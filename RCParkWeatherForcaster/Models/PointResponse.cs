using System.Text.Json.Serialization;

namespace RCParkWeatherForcaster.Models;

public class PointResponse
{
    [JsonPropertyName("properties")]
    public PointProperties? Properties { get; set; }
}

public class PointProperties
{
    [JsonPropertyName("gridId")]
    public string GridId { get; set; } = string.Empty;

    [JsonPropertyName("gridX")]
    public int GridX { get; set; }

    [JsonPropertyName("gridY")]
    public int GridY { get; set; }

    [JsonPropertyName("forecastHourly")]
    public string ForecastHourly { get; set; } = string.Empty;

    [JsonPropertyName("timeZone")]
    public string TimeZone { get; set; } = string.Empty;

    [JsonPropertyName("relativeLocation")]
    public RelativeLocationWrapper? RelativeLocation { get; set; }
}

public class RelativeLocationWrapper
{
    [JsonPropertyName("properties")]
    public RelativeLocationProperties? Properties { get; set; }
}

public class RelativeLocationProperties
{
    [JsonPropertyName("city")]
    public string City { get; set; } = string.Empty;

    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;
}
