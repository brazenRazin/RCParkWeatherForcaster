using Microsoft.Extensions.Options;
using RCParkWeatherForcaster.Models;
using RCParkWeatherForcaster.Services;

// ── Build web application ─────────────────────────────────────────────────────
var builder = WebApplication.CreateBuilder(args);

// Configuration: appsettings.json + environment variables
builder.Configuration
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddEnvironmentVariables();

builder.Services.Configure<AppSettings>(builder.Configuration);
builder.Services.AddSingleton<AppSettings>(sp =>
    sp.GetRequiredService<IOptions<AppSettings>>().Value);

builder.Services.AddHttpClient<IWeatherService, WeatherService>();
builder.Services.AddSingleton<IRcFlyingAssessor, RcFlyingAssessor>();

builder.Logging.SetMinimumLevel(LogLevel.Warning);

// ── Build app ─────────────────────────────────────────────────────────────────
var app = builder.Build();

// Serve static files from wwwroot (the dashboard HTML)
app.UseDefaultFiles();
app.UseStaticFiles();

// GET /api/assessment — live RC park weather assessment as JSON
app.MapGet("/api/assessment", async (
    AppSettings settings,
    IWeatherService weatherService,
    IRcFlyingAssessor assessor) =>
{
    using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
    try
    {
        var weather = await weatherService.GetWeatherAsync(
            settings.Location.Latitude,
            settings.Location.Longitude,
            cts.Token);
            
        var nearestCity = settings.Location.Name;

        var assessment = assessor.Assess(weather,
            settings.Location.Name, nearestCity, settings);

        var profile = PlaneSizeProfile.For(settings.PlaneSize);
        var (goWind, cautionWind, goGust, cautionGust) = settings.GetEffectiveWindLimits();

        return Results.Ok(new AssessmentResponse
        {
            GeneratedAt    = assessment.GeneratedAt,
            LocationName   = assessment.LocationName,
            NearestCity    = assessment.NearestCity,
            PlaneProfile   = new PlaneProfileDto
            {
                DisplayName    = profile.DisplayName,
                Description    = profile.Description,
                GoWindMph      = goWind,
                CautionWindMph = cautionWind,
                GoGustMph      = goGust,
                CautionGustMph = cautionGust
            },
            Next24hRating              = assessment.Next24hRating.ToString(),
            Next48hRating              = assessment.Next48hRating.ToString(),
            Recommended24hMaxPlaneSize = FormatPlaneRec(assessment.Recommended24hMaxPlaneSize),
            Recommended48hMaxPlaneSize = FormatPlaneRec(assessment.Recommended48hMaxPlaneSize),
            ActiveAlerts               = assessment.ActiveAlertSummaries,
            HourlyForecasts            = assessment.HourlyAssessments.Select(h => new HourlyForecastDto
            {
                StartTime          = h.StartTime,
                Rating             = h.Rating.ToString(),
                WindMph            = h.Period.WindSpeed,
                GustMph            = h.Period.WindGust,
                TempF              = h.Period.Temperature,
                PrecipPercent      = h.Period.PrecipProbability * 100.0,
                ShortForecast      = h.Period.Summary,
                Reasons            = h.Reasons,
                SmallestSafePlane  = FormatPlaneRec(PlaneSizeProfile.GetSmallestGoSize(h.Period.WindSpeed, h.Period.WindGust)),
                PlaneRatings       = ComputePlaneRatings(h.Period.WindSpeed, h.Period.WindGust)
            }).ToList()
        });
    }
    catch (OperationCanceledException)
    {
        return Results.Problem("Request timed out — Pirate Weather API did not respond in time.", statusCode: 504);
    }
    catch (HttpRequestException ex)
    {
        return Results.Problem($"Network error: {ex.Message}", statusCode: 502);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error: {ex.Message}", statusCode: 500);
    }
});

// GET /healthz — lightweight liveness probe
app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));

app.Run();

// —— Helpers ———————————————————————————————————————————————————

static string FormatPlaneRec(PlaneSize? size) => size switch
{
    PlaneSize.GiantScale => "Giant Scale (80\"+)",
    PlaneSize.Large      => "Large Sport (55–80\")",
    PlaneSize.ParkFlyer  => "Park Flyer (30–55\")",
    PlaneSize.Mini       => "Mini (20–30\")",
    PlaneSize.Micro      => "Micro (<20\")",
    null                 => "None — too windy for any RC plane",
    _                    => size.ToString() ?? "Unknown"
};

/// <summary>Compute wind-only GO/Caution/NoGo for each of the 5 plane sizes.</summary>
static Dictionary<string, string> ComputePlaneRatings(double windMph, double gustMph)
{
    var order = new[] { PlaneSize.Micro, PlaneSize.Mini, PlaneSize.ParkFlyer, PlaneSize.Large, PlaneSize.GiantScale };
    return order.ToDictionary(
        size => PlaneSizeProfile.For(size).DisplayName,
        size => PlaneSizeProfile.For(size).GetWindRating(windMph, gustMph).ToString());
}

// ── Response DTOs ─────────────────────────────────────────────────────────────

public record AssessmentResponse
{
    public DateTimeOffset GeneratedAt { get; init; }
    public string LocationName { get; init; } = "";
    public string NearestCity { get; init; } = "";
    public PlaneProfileDto PlaneProfile { get; init; } = new();
    public string Next24hRating { get; init; } = "";
    public string Next48hRating { get; init; } = "";
    public string Recommended24hMaxPlaneSize { get; init; } = "";
    public string Recommended48hMaxPlaneSize { get; init; } = "";
    public List<string> ActiveAlerts { get; init; } = new();
    public List<HourlyForecastDto> HourlyForecasts { get; init; } = new();
}

public record PlaneProfileDto
{
    public string DisplayName { get; init; } = "";
    public string Description { get; init; } = "";
    public double GoWindMph { get; init; }
    public double CautionWindMph { get; init; }
    public double GoGustMph { get; init; }
    public double CautionGustMph { get; init; }
}

public record HourlyForecastDto
{
    public DateTimeOffset StartTime { get; init; }
    public string Rating { get; init; } = "";
    public double WindMph { get; init; }
    public double GustMph { get; init; }
    public double TempF { get; init; }
    public double PrecipPercent { get; init; }
    public string ShortForecast { get; init; } = "";
    public List<string> Reasons { get; init; } = new();
    public string SmallestSafePlane { get; init; } = "";
    /// <summary>Wind-only GO/Caution/NoGo for each plane size. Key = display name, value = "Go"|"Caution"|"NoGo".</summary>
    public Dictionary<string, string> PlaneRatings { get; init; } = new();
}
