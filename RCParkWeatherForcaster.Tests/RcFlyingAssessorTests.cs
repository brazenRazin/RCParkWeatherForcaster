using RCParkWeatherForcaster.Models;
using RCParkWeatherForcaster.Services;

namespace RCParkWeatherForcaster.Tests;

public class RcFlyingAssessorTests
{
    private readonly RcFlyingAssessor _assessor = new();
    private readonly AppSettings _defaultSettings = new()
    {
        Location = new LocationSettings { Name = "Test Field", Latitude = 38.0, Longitude = -77.0 },
        PlaneSize = PlaneSize.ParkFlyer,   // GoWind=12, CautionWind=20, GoGust=16, CautionGust=25
        ForecastHours = 48,
        Thresholds = new FlyingThresholds
        {
            // -1 = use PlaneSize profile for wind/gust
            MaxWindSpeedMph              = -1,
            CautionWindSpeedMph          = -1,
            MaxGustMph                   = -1,
            CautionGustMph               = -1,
            MaxPrecipProbabilityPercent  = 20,
            CautionPrecipProbabilityPercent = 40,
            MinTempF       = 40,
            MaxTempF       = 95,
            CautionMinTempF = 32,
            CautionMaxTempF = 104
        }
    };

    // Helper: build a single forecast period starting from now
    private static PirateWeatherDataPoint MakePeriod(
        int offsetHours = 1,
        double wind = 5.0,
        double gust = 0.0,
        double temp = 72.0,
        double precip = 0.1, // 10%
        string summary = "Sunny")
    {
        return new PirateWeatherDataPoint
        {
            Time = DateTimeOffset.UtcNow.AddHours(offsetHours).ToUnixTimeSeconds(),
            Temperature = temp,
            WindSpeed = wind,
            WindGust = gust,
            Summary = summary,
            PrecipProbability = precip
        };
    }

    private PirateWeatherResponse MakeForecast(params PirateWeatherDataPoint[] periods)
    {
        return new PirateWeatherResponse
        {
            Hourly = new PirateWeatherHourlyData
            {
                Data = periods.ToList()
            }
        };
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Assess_CalmConditions_ReturnsGo()
    {
        var forecast = MakeForecast(MakePeriod(wind: 5, temp: 72, precip: 0.05, summary: "Sunny"));
        var result = _assessor.Assess(forecast, "Test", "City, ST", _defaultSettings);

        Assert.Equal(FlyingRating.Go, result.Next24hRating);
        Assert.Single(result.HourlyAssessments);
        Assert.Equal(FlyingRating.Go, result.HourlyAssessments[0].Rating);
    }

    [Fact]
    public void Assess_HighWind_ReturnsNoGo()
    {
        var forecast = MakeForecast(MakePeriod(wind: 30, temp: 72, precip: 0.05));
        var result = _assessor.Assess(forecast, "Test", "City, ST", _defaultSettings);

        Assert.Equal(FlyingRating.NoGo, result.Next24hRating);
        Assert.Equal(FlyingRating.NoGo, result.HourlyAssessments[0].Rating);
        Assert.Contains(result.HourlyAssessments[0].Reasons, r => r.Contains("Wind") && r.Contains("30"));
    }

    [Fact]
    public void Assess_ModerateWind_ReturnsCaution()
    {
        var forecast = MakeForecast(MakePeriod(wind: 20, temp: 72, precip: 0.05));
        var result = _assessor.Assess(forecast, "Test", "City, ST", _defaultSettings);

        Assert.Equal(FlyingRating.Caution, result.Next24hRating);
        Assert.Equal(FlyingRating.Caution, result.HourlyAssessments[0].Rating);
    }

    [Fact]
    public void Assess_ThunderstormForecast_ReturnsNoGo()
    {
        var forecast = MakeForecast(MakePeriod(wind: 5, summary: "Thunderstorms likely"));
        var result = _assessor.Assess(forecast, "Test", "City, ST", _defaultSettings);

        Assert.Equal(FlyingRating.NoGo, result.HourlyAssessments[0].Rating);
        Assert.Contains(result.HourlyAssessments[0].Reasons, r => r.Contains("Thunder", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Assess_FreezingTemp_ReturnsNoGo()
    {
        var forecast = MakeForecast(MakePeriod(wind: 5, temp: 28, precip: 0.05)); // 28°F < 32°F limit
        var result = _assessor.Assess(forecast, "Test", "City, ST", _defaultSettings);

        Assert.Equal(FlyingRating.NoGo, result.HourlyAssessments[0].Rating);
        Assert.Contains(result.HourlyAssessments[0].Reasons, r => r.Contains("28"));
    }

    [Fact]
    public void Assess_ColdButAboveFreezing_ReturnsCaution()
    {
        var forecast = MakeForecast(MakePeriod(wind: 5, temp: 35, precip: 0.05)); // 35°F: caution range
        var result = _assessor.Assess(forecast, "Test", "City, ST", _defaultSettings);

        Assert.Equal(FlyingRating.Caution, result.HourlyAssessments[0].Rating);
    }

    [Fact]
    public void Assess_ActiveWarningAlert_ReturnsCaution()
    {
        var forecast = MakeForecast(MakePeriod(wind: 5, temp: 72, precip: 0.05));
        forecast.Alerts = new List<PirateWeatherAlert>
        {
            new PirateWeatherAlert { Title = "Severe Thunderstorm Warning" }
        };

        var result = _assessor.Assess(forecast, "Test", "City, ST", _defaultSettings);

        Assert.Equal(FlyingRating.Caution, result.HourlyAssessments[0].Rating);
        Assert.Single(result.ActiveAlertSummaries);
    }

    [Fact]
    public void Assess_24hWindowAggregation_WorstRatingPropagates()
    {
        var goA = MakePeriod(offsetHours: 1, wind: 5, temp: 72, precip: 0.05, summary: "Sunny");
        var noGoB = MakePeriod(offsetHours: 2, wind: 35, temp: 72, precip: 0.05);
        var goC = MakePeriod(offsetHours: 25, wind: 5, temp: 72, precip: 0.05, summary: "Sunny");

        var forecast = MakeForecast(goA, noGoB, goC);
        var result = _assessor.Assess(forecast, "Test", "City, ST", _defaultSettings);

        Assert.Equal(FlyingRating.NoGo, result.Next24hRating);
        Assert.Equal(FlyingRating.NoGo, result.Next48hRating);
    }

    [Fact]
    public void Assess_HighPrecip_ReturnsNoGo()
    {
        var forecast = MakeForecast(MakePeriod(wind: 5, temp: 72, precip: 0.60)); // 60% > 40% limit
        var result = _assessor.Assess(forecast, "Test", "City, ST", _defaultSettings);

        Assert.Equal(FlyingRating.NoGo, result.HourlyAssessments[0].Rating);
    }

    [Fact]
    public void Assess_HighGusts_ReturnsNoGo()
    {
        var forecast = MakeForecast(MakePeriod(wind: 10, gust: 35, temp: 72, precip: 0.05));
        var result = _assessor.Assess(forecast, "Test", "City, ST", _defaultSettings);

        Assert.Equal(FlyingRating.NoGo, result.HourlyAssessments[0].Rating);
        Assert.Contains(result.HourlyAssessments[0].Reasons, r => r.Contains("Gust"));
    }

    [Fact]
    public void Assess_MicroProfile_RejectsModerateWind()
    {
        var microSettings = new AppSettings
        {
            Location = new LocationSettings { Name = "Test", Latitude = 38.0, Longitude = -77.0 },
            PlaneSize = PlaneSize.Micro,
            ForecastHours = 48,
            Thresholds = new FlyingThresholds { MaxWindSpeedMph = -1, CautionWindSpeedMph = -1, MaxGustMph = -1, CautionGustMph = -1 }
        };
        var forecast = MakeForecast(MakePeriod(wind: 10, temp: 72, precip: 0.05));
        var result = _assessor.Assess(forecast, "Test", "City, ST", microSettings);

        Assert.Equal(FlyingRating.NoGo, result.HourlyAssessments[0].Rating);
    }

    [Fact]
    public void Assess_GiantScaleProfile_AllowsModerateWind()
    {
        var giantSettings = new AppSettings
        {
            Location = new LocationSettings { Name = "Test", Latitude = 38.0, Longitude = -77.0 },
            PlaneSize = PlaneSize.GiantScale,
            ForecastHours = 48,
            Thresholds = new FlyingThresholds { MaxWindSpeedMph = -1, CautionWindSpeedMph = -1, MaxGustMph = -1, CautionGustMph = -1 }
        };
        var forecast = MakeForecast(MakePeriod(wind: 25, temp: 72, precip: 0.05));
        var result = _assessor.Assess(forecast, "Test", "City, ST", giantSettings);

        Assert.Equal(FlyingRating.Caution, result.HourlyAssessments[0].Rating);
    }

    [Fact]
    public void Assess_ManualOverride_TakesPrecedenceOverProfile()
    {
        var overrideSettings = new AppSettings
        {
            Location = new LocationSettings { Name = "Test", Latitude = 38.0, Longitude = -77.0 },
            PlaneSize = PlaneSize.ParkFlyer,
            ForecastHours = 48,
            Thresholds = new FlyingThresholds
            {
                MaxWindSpeedMph = 20,      // manual override
                CautionWindSpeedMph = 30,  // manual override
                MaxGustMph = -1,
                CautionGustMph = -1
            }
        };
        var forecast = MakeForecast(MakePeriod(wind: 18, temp: 72, precip: 0.05, summary: "Sunny"));
        var result = _assessor.Assess(forecast, "Test", "City, ST", overrideSettings);

        Assert.Equal(FlyingRating.Go, result.HourlyAssessments[0].Rating);
    }
}
