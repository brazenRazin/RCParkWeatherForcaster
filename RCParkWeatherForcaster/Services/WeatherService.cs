using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RCParkWeatherForcaster.Models;

namespace RCParkWeatherForcaster.Services;

public interface IWeatherService
{
    Task<PirateWeatherResponse> GetWeatherAsync(double latitude, double longitude, CancellationToken ct = default);
}

public class WeatherService : IWeatherService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WeatherService> _logger;
    private readonly AppSettings _settings;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public WeatherService(HttpClient httpClient, IOptions<AppSettings> settings, ILogger<WeatherService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _settings = settings.Value;

        // Pirate Weather API URL
        _httpClient.BaseAddress = new Uri("https://api.pirateweather.net");
        if (!string.IsNullOrEmpty(_settings.UserAgent))
        {
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(_settings.UserAgent);
        }
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<PirateWeatherResponse> GetWeatherAsync(double latitude, double longitude, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.PirateWeatherApiKey))
        {
            throw new InvalidOperationException("PirateWeatherApiKey is not configured.");
        }

        var url = $"/forecast/{_settings.PirateWeatherApiKey}/{latitude},{longitude}?exclude=minutely,daily";
        _logger.LogDebug("Fetching weather from Pirate Weather for ({Lat}, {Lon})", latitude, longitude);

        try
        {
            var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<PirateWeatherResponse>(_jsonOptions, ct)
                ?? throw new InvalidOperationException("Empty response from Pirate Weather API endpoint.");
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch weather from Pirate Weather API.");
            throw;
        }
    }
}
