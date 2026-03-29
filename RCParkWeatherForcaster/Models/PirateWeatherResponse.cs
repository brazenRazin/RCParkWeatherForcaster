using System.Text.Json.Serialization;

namespace RCParkWeatherForcaster.Models;

public class PirateWeatherResponse
{
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    [JsonPropertyName("timezone")]
    public string Timezone { get; set; } = string.Empty;

    [JsonPropertyName("offset")]
    public double Offset { get; set; }

    [JsonPropertyName("hourly")]
    public PirateWeatherHourlyData Hourly { get; set; } = new();

    [JsonPropertyName("alerts")]
    public List<PirateWeatherAlert> Alerts { get; set; } = new();
}

public class PirateWeatherHourlyData
{
    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    [JsonPropertyName("icon")]
    public string Icon { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public List<PirateWeatherDataPoint> Data { get; set; } = new();
}

public class PirateWeatherDataPoint
{
    [JsonPropertyName("time")]
    public long Time { get; set; } // Unix timestamp

    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    [JsonPropertyName("icon")]
    public string Icon { get; set; } = string.Empty;

    [JsonPropertyName("precipProbability")]
    public double PrecipProbability { get; set; } // Note: Pirate Weather provides 0.0 to 1.0, not 0 to 100

    [JsonPropertyName("temperature")]
    public double Temperature { get; set; }

    [JsonPropertyName("apparentTemperature")]
    public double ApparentTemperature { get; set; }

    [JsonPropertyName("dewPoint")]
    public double DewPoint { get; set; }

    [JsonPropertyName("humidity")]
    public double Humidity { get; set; }

    [JsonPropertyName("windSpeed")]
    public double WindSpeed { get; set; }

    [JsonPropertyName("windGust")]
    public double WindGust { get; set; }

    [JsonPropertyName("windBearing")]
    public double WindBearing { get; set; }

    [JsonPropertyName("uvIndex")]
    public double UvIndex { get; set; }

    [JsonPropertyName("visibility")]
    public double Visibility { get; set; }
}

public class PirateWeatherAlert
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("time")]
    public long Time { get; set; } // Unix timestamp

    [JsonPropertyName("expires")]
    public long Expires { get; set; } // Unix timestamp

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("uri")]
    public string Uri { get; set; } = string.Empty;
}
