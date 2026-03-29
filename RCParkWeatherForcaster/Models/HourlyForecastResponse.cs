using System.Text.Json.Serialization;

namespace RCParkWeatherForcaster.Models;

public class HourlyForecastResponse
{
    [JsonPropertyName("properties")]
    public HourlyForecastProperties? Properties { get; set; }
}

public class HourlyForecastProperties
{
    [JsonPropertyName("periods")]
    public List<ForecastPeriod> Periods { get; set; } = new();
}

public class ForecastPeriod
{
    [JsonPropertyName("number")]
    public int Number { get; set; }

    [JsonPropertyName("startTime")]
    public DateTimeOffset StartTime { get; set; }

    [JsonPropertyName("endTime")]
    public DateTimeOffset EndTime { get; set; }

    [JsonPropertyName("isDaytime")]
    public bool IsDaytime { get; set; }

    [JsonPropertyName("temperature")]
    public int Temperature { get; set; }

    [JsonPropertyName("temperatureUnit")]
    public string TemperatureUnit { get; set; } = "F";

    [JsonPropertyName("windSpeed")]
    public string WindSpeed { get; set; } = "0 mph";

    [JsonPropertyName("windGust")]
    public string? WindGust { get; set; }

    [JsonPropertyName("windDirection")]
    public string WindDirection { get; set; } = string.Empty;

    [JsonPropertyName("shortForecast")]
    public string ShortForecast { get; set; } = string.Empty;

    [JsonPropertyName("detailedForecast")]
    public string DetailedForecast { get; set; } = string.Empty;

    [JsonPropertyName("probabilityOfPrecipitation")]
    public QuantitativeValue? ProbabilityOfPrecipitation { get; set; }

    [JsonPropertyName("relativeHumidity")]
    public QuantitativeValue? RelativeHumidity { get; set; }

    [JsonPropertyName("dewpoint")]
    public QuantitativeValue? Dewpoint { get; set; }

    /// <summary>Parses "12 mph" or "12 to 17 mph" into the highest numeric value in mph.</summary>
    public double GetWindSpeedMph()
    {
        return ParseSpeedMph(WindSpeed);
    }

    /// <summary>Parses gust string; returns 0 if null/missing.</summary>
    public double GetWindGustMph()
    {
        return string.IsNullOrWhiteSpace(WindGust) ? 0 : ParseSpeedMph(WindGust);
    }

    private static double ParseSpeedMph(string speedStr)
    {
        if (string.IsNullOrWhiteSpace(speedStr)) return 0;
        // Strip out "mph", "km/h" etc and find the max number
        var cleaned = speedStr.Replace("mph", "").Replace("km/h", "").Trim();
        var parts = cleaned.Split(new[] { " to ", " ", "-" }, StringSplitOptions.RemoveEmptyEntries);
        double max = 0;
        foreach (var p in parts)
        {
            if (double.TryParse(p, out double val) && val > max)
                max = val;
        }
        return max;
    }

    /// <summary>Temperature in Fahrenheit.</summary>
    public double GetTemperatureF()
    {
        if (TemperatureUnit == "C")
            return Temperature * 9.0 / 5.0 + 32;
        return Temperature;
    }

    /// <summary>Precipitation probability 0–100, or 0 if not available.</summary>
    public double GetPrecipProbability()
    {
        return ProbabilityOfPrecipitation?.Value ?? 0;
    }
}

public class QuantitativeValue
{
    [JsonPropertyName("value")]
    public double? Value { get; set; }

    [JsonPropertyName("unitCode")]
    public string? UnitCode { get; set; }
}
