namespace Flightfront.Domain.Models;

public class MetarData
{
    public string RawMetar { get; set; } = string.Empty;
    public string IcaoCode { get; set; } = string.Empty;
    public DateTime ObservationTime { get; set; }
    public WindInfo? Wind { get; set; }
    public VisibilityInfo? Visibility { get; set; }
    public WeatherInfo? Weather { get; set; }
    public List<CloudInfo> Clouds { get; set; } = new();
    public TemperatureInfo? Temperature { get; set; }
    public PressureInfo? Pressure { get; set; }
}

public class WindInfo
{
    public int Direction { get; set; }
    public int Speed { get; set; }
    public int? Gust { get; set; }
    public string Unit { get; set; } = "KT";
}

public class VisibilityInfo
{
    public string Value { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
}

public class WeatherInfo
{
    public List<string> Phenomena { get; set; } = new();
    public string IconCode { get; set; } = string.Empty;
}

public class CloudInfo
{
    public string Coverage { get; set; } = string.Empty;
    public int? Altitude { get; set; }
    public string? Type { get; set; }
}

public class TemperatureInfo
{
    public int Celsius { get; set; }
    public int? Dewpoint { get; set; }
}

public class PressureInfo
{
    public decimal Value { get; set; }
    public string Unit { get; set; } = "hPa";
}
