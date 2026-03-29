namespace RCParkWeatherForcaster.Models;

/// <summary>
/// Standard RC plane size categories with wind tolerance profiles
/// based on typical hobbyist and community guidelines.
/// </summary>
public enum PlaneSize
{
    /// <summary>Wingspan &lt; 20", weight &lt; 100g. Indoor/no-wind only. e.g. Blade Nano, tiny foam models.</summary>
    Micro,

    /// <summary>Wingspan 20–30", weight 100g–500g. e.g. HobbyZone Champ, E-flite UMX.</summary>
    Mini,

    /// <summary>Wingspan 30–55", weight 500g–2kg. e.g. HobbyZone Sport Cub, E-flite Apprentice. Best for beginners.</summary>
    ParkFlyer,

    /// <summary>Wingspan 55–80", weight 2–5kg. e.g. E-flite Turbo Timber, Hangar 9 trainers. Handles wind well.</summary>
    Large,

    /// <summary>Wingspan 80"+, weight 5kg+. e.g. 1/4-scale or larger warbirds. Most wind-tolerant.</summary>
    GiantScale
}

/// <summary>Wind and gust thresholds (mph) for a given plane size, based on community consensus.</summary>
public class PlaneSizeProfile
{
    public string DisplayName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;

    /// <summary>Sustained wind ≤ this value = GO.</summary>
    public double GoWindMph { get; init; }

    /// <summary>Sustained wind between GoWindMph and this value = CAUTION. Above = NO-GO.</summary>
    public double CautionWindMph { get; init; }

    /// <summary>Gust ≤ this value = GO.</summary>
    public double GoGustMph { get; init; }

    /// <summary>Gust between GoGustMph and this value = CAUTION. Above = NO-GO.</summary>
    public double CautionGustMph { get; init; }

    /// <summary>Returns the built-in profile for a given plane size.</summary>
    public static PlaneSizeProfile For(PlaneSize size) => size switch
    {
        PlaneSize.Micro => new PlaneSizeProfile
        {
            DisplayName = "Micro",
            Description = "Wingspan < 20\", < 100g — best indoors or in dead-calm air",
            GoWindMph      =  4,   CautionWindMph  =  7,
            GoGustMph      =  6,   CautionGustMph  = 10
        },
        PlaneSize.Mini => new PlaneSizeProfile
        {
            DisplayName = "Mini",
            Description = "Wingspan 20–30\", 100–500g — calm outdoor conditions",
            GoWindMph      =  7,   CautionWindMph  = 12,
            GoGustMph      = 10,   CautionGustMph  = 16
        },
        PlaneSize.ParkFlyer => new PlaneSizeProfile
        {
            DisplayName = "Park Flyer",
            Description = "Wingspan 30–55\", 500g–2kg — most common beginner/sport size",
            GoWindMph      = 12,   CautionWindMph  = 20,
            GoGustMph      = 16,   CautionGustMph  = 25
        },
        PlaneSize.Large => new PlaneSizeProfile
        {
            DisplayName = "Large Sport/Trainer",
            Description = "Wingspan 55–80\", 2–5kg — handles moderate wind well",
            GoWindMph      = 18,   CautionWindMph  = 28,
            GoGustMph      = 22,   CautionGustMph  = 35
        },
        PlaneSize.GiantScale => new PlaneSizeProfile
        {
            DisplayName = "Giant Scale",
            Description = "Wingspan 80\"+, 5kg+ — most wind-tolerant RC planes",
            GoWindMph      = 22,   CautionWindMph  = 32,
            GoGustMph      = 28,   CautionGustMph  = 40
        },
        _ => For(PlaneSize.ParkFlyer)
    };

    /// <summary>
    /// Returns the largest PlaneSize whose Go thresholds are not exceeded by the given wind and gust.
    /// Returns null if conditions are too severe even for a Micro.
    /// </summary>
    public static PlaneSize? GetLargestGoSize(double windMph, double gustMph)
    {
        // Iterate from largest to smallest — return the first that qualifies
        foreach (var size in new[] { PlaneSize.GiantScale, PlaneSize.Large, PlaneSize.ParkFlyer, PlaneSize.Mini, PlaneSize.Micro })
        {
            var p = For(size);
            bool windOk = windMph <= p.GoWindMph;
            bool gustOk = gustMph <= 0 || gustMph <= p.GoGustMph;
            if (windOk && gustOk)
                return size;
        }
        return null; // no plane should fly
    }

    /// <summary>
    /// Returns the smallest PlaneSize whose Go thresholds are not exceeded by the given wind and gust.
    /// This is the minimum plane size a pilot needs to safely fly in these conditions.
    /// Returns null if no plane size gets a GO (all are NO-GO).
    /// </summary>
    public static PlaneSize? GetSmallestGoSize(double windMph, double gustMph)
    {
        // Iterate from smallest to largest — return the first that qualifies
        foreach (var size in new[] { PlaneSize.Micro, PlaneSize.Mini, PlaneSize.ParkFlyer, PlaneSize.Large, PlaneSize.GiantScale })
        {
            var p = For(size);
            bool windOk = windMph <= p.GoWindMph;
            bool gustOk = gustMph <= 0 || gustMph <= p.GoGustMph;
            if (windOk && gustOk)
                return size;
        }
        return null; // no plane gets a GO
    }

    /// <summary>
    /// Returns GO/Caution/NoGo rating for this plane size given the wind and gust speed.
    /// </summary>
    public FlyingRating GetWindRating(double windMph, double gustMph)
    {
        var windRating = windMph switch
        {
            var w when w > CautionWindMph => FlyingRating.NoGo,
            var w when w > GoWindMph      => FlyingRating.Caution,
            _                             => FlyingRating.Go
        };
        FlyingRating gustRating = FlyingRating.Go;
        if (gustMph > 0)
        {
            gustRating = gustMph switch
            {
                var g when g > CautionGustMph => FlyingRating.NoGo,
                var g when g > GoGustMph      => FlyingRating.Caution,
                _                             => FlyingRating.Go
            };
        }
        return windRating > gustRating ? windRating : gustRating;
    }
}

public class AppSettings
{
    public LocationSettings Location { get; set; } = new();

    /// <summary>
    /// Select the plane size profile. This sets wind/gust thresholds automatically.
    /// Valid values: Micro, Mini, ParkFlyer, Large, GiantScale.
    /// Individual wind/gust thresholds in the Thresholds section override the profile defaults.
    /// </summary>
    public PlaneSize PlaneSize { get; set; } = PlaneSize.ParkFlyer;

    /// <summary>
    /// Optional manual overrides. Leave at -1 (or omit) to use the PlaneSize profile defaults.
    /// Precip, temperature, and other non-wind thresholds are always taken from here.
    /// </summary>
    public FlyingThresholds Thresholds { get; set; } = new();
    public int ForecastHours { get; set; } = 48;
    public string UserAgent { get; set; } = "RCParkWeatherForcaster/1.0";
    public string PirateWeatherApiKey { get; set; } = string.Empty;

    /// <summary>Returns the effective wind/gust limits, applying any manual overrides on top of the profile.</summary>
    public (double goWind, double cautionWind, double goGust, double cautionGust) GetEffectiveWindLimits()
    {
        var profile = PlaneSizeProfile.For(PlaneSize);
        return (
            Thresholds.MaxWindSpeedMph   >= 0 ? Thresholds.MaxWindSpeedMph   : profile.GoWindMph,
            Thresholds.CautionWindSpeedMph >= 0 ? Thresholds.CautionWindSpeedMph : profile.CautionWindMph,
            Thresholds.MaxGustMph        >= 0 ? Thresholds.MaxGustMph        : profile.GoGustMph,
            Thresholds.CautionGustMph    >= 0 ? Thresholds.CautionGustMph    : profile.CautionGustMph
        );
    }
}

public class LocationSettings
{
    public string Name { get; set; } = "Unknown Location";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class FlyingThresholds
{
    // Wind/gust: set to -1 (default) = use the PlaneSize profile instead.

    /// <summary>Max sustained wind (mph) for GO. Set to -1 to use PlaneSize profile default.</summary>
    public double MaxWindSpeedMph { get; set; } = -1;

    /// <summary>Max sustained wind (mph) for CAUTION (above = NO-GO). Set to -1 to use PlaneSize profile default.</summary>
    public double CautionWindSpeedMph { get; set; } = -1;

    /// <summary>Max gust (mph) for GO. Set to -1 to use PlaneSize profile default.</summary>
    public double MaxGustMph { get; set; } = -1;

    /// <summary>Max gust (mph) for CAUTION (above = NO-GO). Set to -1 to use PlaneSize profile default.</summary>
    public double CautionGustMph { get; set; } = -1;

    // Precip and temp are never derived from plane size — always explicit.

    /// <summary>Max precipitation probability % for GO.</summary>
    public double MaxPrecipProbabilityPercent { get; set; } = 20;

    /// <summary>Max precipitation probability % for CAUTION (above = NO-GO).</summary>
    public double CautionPrecipProbabilityPercent { get; set; } = 40;

    /// <summary>Minimum temperature (°F) for GO.</summary>
    public double MinTempF { get; set; } = 40;

    /// <summary>Maximum temperature (°F) for GO.</summary>
    public double MaxTempF { get; set; } = 95;

    /// <summary>Minimum temperature (°F) for CAUTION (below = NO-GO).</summary>
    public double CautionMinTempF { get; set; } = 32;

    /// <summary>Maximum temperature (°F) for CAUTION (above = NO-GO).</summary>
    public double CautionMaxTempF { get; set; } = 104;
}
