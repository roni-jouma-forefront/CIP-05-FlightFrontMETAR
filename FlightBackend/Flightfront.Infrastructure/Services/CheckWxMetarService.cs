using System.Net.Http.Headers;
using System.Text.Json;
using Flightfront.Application.Interfaces;
using Flightfront.Domain.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Flightfront.Infrastructure.Services;

public class CheckWxMetarService : IMetarService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger<CheckWxMetarService> _logger;

    public CheckWxMetarService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<CheckWxMetarService> logger
    )
    {
        _httpClient = httpClient;
        _apiKey =
            configuration["CheckWxApi:ApiKey"]
            ?? throw new InvalidOperationException("CheckWX API key not configured");
        _logger = logger;

        _httpClient.BaseAddress = new Uri("https://api.checkwx.com/");
        _httpClient.DefaultRequestHeaders.Add("X-API-Key", _apiKey);
    }

    public async Task<MetarData?> GetMetarByIcaoAsync(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var trimmedInput = input.Trim();
        _logger.LogInformation("Processing input: {Input}", trimmedInput);

        var parts = trimmedInput.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Extract the first 4-letter code
        var firstPart = parts.Length > 0 ? parts[0] : "";

        // Remove "METAR" or "SPECI" prefix if present
        if (
            firstPart.Equals("METAR", StringComparison.OrdinalIgnoreCase)
            || firstPart.Equals("SPECI", StringComparison.OrdinalIgnoreCase)
        )
        {
            firstPart = parts.Length > 1 ? parts[1] : "";
        }

        // Get first 4 letters from the first part
        var icaoCode = new string(firstPart.TakeWhile(char.IsLetter).Take(4).ToArray());
        _logger.LogInformation("Extracted ICAO code: {IcaoCode}", icaoCode);

        // If we have exactly 4 letters
        if (icaoCode.Length == 4)
        {
            // Check if this is a full METAR string by looking at the second relevant part
            var relevantParts = parts
                .Where(p =>
                    !p.Equals("METAR", StringComparison.OrdinalIgnoreCase)
                    && !p.Equals("SPECI", StringComparison.OrdinalIgnoreCase)
                )
                .ToArray();

            _logger.LogInformation(
                "Relevant parts count: {Count}, Second part: {SecondPart}",
                relevantParts.Length,
                relevantParts.Length > 1 ? relevantParts[1] : "N/A"
            );

            // If second part exists and looks like a date/time group (ends with Z and has 6 digits)
            // then it's a full METAR string (e.g., "ESSB 021220Z 11005KT...")
            if (
                relevantParts.Length > 1
                && relevantParts[1].EndsWith("Z", StringComparison.OrdinalIgnoreCase)
                && relevantParts[1].Length >= 6
                && relevantParts[1].TrimEnd('Z', 'z').All(char.IsDigit)
            )
            {
                _logger.LogInformation("Detected as full METAR string - parsing directly");
                return ParseMetarString(trimmedInput);
            }

            // Otherwise, treat as ICAO code (e.g., "ESSB" or "ESSB Stockholm-Bromma Airport")
            _logger.LogInformation("Detected as ICAO code - fetching from API");
            try
            {
                var response = await _httpClient.GetAsync($"metar/{icaoCode.ToUpper()}");

                if (!response.IsSuccessStatusCode)
                    return null;

                var content = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(content);

                if (jsonDoc.RootElement.GetProperty("results").GetInt32() == 0)
                    return null;

                var data = jsonDoc.RootElement.GetProperty("data");
                var rawMetar = data[0].GetString() ?? string.Empty;

                return ParseMetarString(rawMetar);
            }
            catch
            {
                return null;
            }
        }

        // If no valid ICAO code found, try to parse as raw METAR anyway
        return ParseMetarString(trimmedInput);
    }

    public MetarData? ParseMetarString(string metarString)
    {
        if (string.IsNullOrWhiteSpace(metarString))
            return null;

        var remarkIndex = metarString.IndexOf("RMK", StringComparison.OrdinalIgnoreCase);
        if (remarkIndex > 0)
        {
            metarString = metarString[..remarkIndex].Trim();
        }

        var forecastKeywords = new[] { "TEMPO", "BECMG", "NOSIG", "PROB" };
        foreach (var keyword in forecastKeywords)
        {
            var forecastIndex = metarString.IndexOf(
                $" {keyword} ",
                StringComparison.OrdinalIgnoreCase
            );
            if (forecastIndex > 0)
            {
                metarString = metarString[..forecastIndex].Trim();
                break;
            }
        }

        if (metarString.StartsWith("METAR ", StringComparison.OrdinalIgnoreCase))
        {
            metarString = metarString[6..].Trim();
        }
        else if (metarString.StartsWith("SPECI ", StringComparison.OrdinalIgnoreCase))
        {
            metarString = metarString[6..].Trim();
        }

        var parts = metarString.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var metarData = new MetarData
        {
            RawMetar = metarString,
            IcaoCode = parts.Length > 0 ? parts[0] : string.Empty,
        };

        // (format: DDHHMMz)
        if (parts.Length > 1 && parts[1].EndsWith("Z"))
        {
            var timePart = parts[1].TrimEnd('Z');
            if (timePart.Length == 6 && int.TryParse(timePart, out _))
            {
                var day = int.Parse(timePart[..2]);
                var hour = int.Parse(timePart[2..4]);
                var minute = int.Parse(timePart[4..6]);

                var now = DateTime.UtcNow;
                var obsTime = new DateTime(
                    now.Year,
                    now.Month,
                    day,
                    hour,
                    minute,
                    0,
                    DateTimeKind.Utc
                );

                if (obsTime > now.AddDays(1))
                {
                    obsTime = obsTime.AddMonths(-1);
                }

                metarData.ObservationTime = obsTime;
            }
        }

        // Parse components
        ParseWind(parts, metarData);
        ParseVisibility(parts, metarData);
        ParseWeather(parts, metarData);
        ParseClouds(parts, metarData);
        ParseTemperature(parts, metarData);
        ParsePressure(parts, metarData);

        metarData.Weather ??= new WeatherInfo();
        metarData.Weather.IconCode = DetermineWeatherIcon(metarData);

        return metarData;
    }

    private void ParseWind(string[] parts, MetarData metarData)
    {
        foreach (var part in parts)
        {
            if (part.Contains("KT") || part.Contains("MPS"))
            {
                var windPart = part.Replace("KT", "").Replace("MPS", "");

                // Handle variable wind (VRB)
                if (windPart.StartsWith("VRB"))
                {
                    windPart = windPart.Replace("VRB", "000");
                }

                if (windPart.Length >= 5)
                {
                    metarData.Wind = new WindInfo
                    {
                        Direction = int.TryParse(windPart[..3], out var dir) ? dir : 0,
                        Speed = int.TryParse(windPart[3..5], out var spd) ? spd : 0,
                        Unit = part.Contains("KT") ? "KT" : "MPS",
                    };

                    if (windPart.Contains("G"))
                    {
                        var gustPart = windPart.Split('G')[1];
                        metarData.Wind.Gust = int.TryParse(gustPart, out var gust) ? gust : null;
                    }
                }
                break;
            }
        }
    }

    private void ParseVisibility(string[] parts, MetarData metarData)
    {
        for (int i = 0; i < parts.Length; i++)
        {
            var part = parts[i];

            //miles (e.g., "10SM" or "1/2SM")
            if (part.EndsWith("SM"))
            {
                metarData.Visibility = new VisibilityInfo
                {
                    Value = part.Replace("SM", ""),
                    Unit = "SM",
                };
                break;
            }
            // Meters (4-digit number, e.g., "9999")
            else if (part.Length == 4 && int.TryParse(part, out var meters))
            {
                metarData.Visibility = new VisibilityInfo { Value = meters.ToString(), Unit = "M" };
                break;
            }
            // CAVOK
            else if (part == "CAVOK")
            {
                metarData.Visibility = new VisibilityInfo { Value = "10000+", Unit = "M" };
                break;
            }
        }
    }

    private void ParseWeather(string[] parts, MetarData metarData)
    {
        var weatherPhenomena = new List<string>();
        var weatherCodes = new[]
        {
            "RA",
            "SN",
            "FG",
            "BR",
            "DZ",
            "TS",
            "SH",
            "GR",
            "GS",
            "PL",
            "IC",
            "FU",
            "VA",
            "DU",
            "SA",
            "HZ",
        };

        foreach (var part in parts)
        {
            // Skip if it looks like other METAR components
            if (part.Length == 4 && int.TryParse(part, out _))
                continue;
            if (part.EndsWith("SM") || part.EndsWith("KT") || part.EndsWith("MPS"))
                continue;
            if (part.StartsWith("Q") || part.StartsWith("A"))
                continue;
            if (part.Contains("/"))
                continue;
            // Skip forecast indicators and NOSIG
            if (part == "NOSIG" || part == "TEMPO" || part == "BECMG" || part == "PROB")
                continue;
            // Skip cloud coverage codes
            if (
                part.StartsWith("SKC")
                || part.StartsWith("CLR")
                || part.StartsWith("NSC")
                || part.StartsWith("NCD")
                || part.StartsWith("FEW")
                || part.StartsWith("SCT")
                || part.StartsWith("BKN")
                || part.StartsWith("OVC")
                || part.StartsWith("VV")
            )
                continue;

            foreach (var code in weatherCodes)
            {
                if (part.Contains(code))
                {
                    weatherPhenomena.Add(part);
                    break;
                }
            }
        }

        if (weatherPhenomena.Any())
        {
            metarData.Weather = new WeatherInfo
            {
                Phenomena = weatherPhenomena.Select(TranslateWeatherCode).ToList(),
            };
        }
    }

    private string TranslateWeatherCode(string code)
    {
        var intensity = "";
        var actualCode = code;

        if (code.StartsWith("-"))
        {
            intensity = "Light ";
            actualCode = code[1..];
        }
        else if (code.StartsWith("+"))
        {
            intensity = "Heavy ";
            actualCode = code[1..];
        }

        var weatherTranslations = new Dictionary<string, string>
        {
            { "RA", "Rain" },
            { "SN", "Snow" },
            { "FG", "Fog" },
            { "BR", "Mist" },
            { "DZ", "Drizzle" },
            { "TS", "Thunderstorm" },
            { "SH", "Showers" },
            { "GR", "Hail" },
            { "GS", "Small Hail" },
            { "PL", "Ice Pellets" },
            { "IC", "Ice Crystals" },
            { "FU", "Smoke" },
            { "VA", "Volcanic Ash" },
            { "DU", "Dust" },
            { "SA", "Sand" },
            { "HZ", "Haze" },
            { "SHRA", "Rain Showers" },
            { "SHSN", "Snow Showers" },
            { "TSRA", "Thunderstorm with Rain" },
            { "TSGR", "Thunderstorm with Hail" },
        };

        foreach (var translation in weatherTranslations.OrderByDescending(t => t.Key.Length))
        {
            if (actualCode.Contains(translation.Key))
            {
                return intensity + translation.Value;
            }
        }

        return code;
    }

    private void ParseClouds(string[] parts, MetarData metarData)
    {
        var cloudCoverage = new[] { "SKC", "CLR", "NSC", "NCD", "FEW", "SCT", "BKN", "OVC", "VV" };

        foreach (var part in parts)
        {
            foreach (var coverage in cloudCoverage)
            {
                if (part.StartsWith(coverage))
                {
                    if (coverage is "SKC" or "CLR" or "NSC" or "NCD")
                    {
                        metarData.Clouds.Add(
                            new CloudInfo
                            {
                                Coverage = TranslateCloudCoverage(coverage),
                                Altitude = null,
                                Type = null,
                            }
                        );
                        return;
                    }

                    var remainingPart = part[coverage.Length..];
                    var altitudePart = new string(remainingPart.TakeWhile(char.IsDigit).ToArray());

                    string? cloudType = null;
                    if (remainingPart.Length > altitudePart.Length)
                    {
                        cloudType = remainingPart[altitudePart.Length..];
                        if (!string.IsNullOrWhiteSpace(cloudType))
                        {
                            cloudType = TranslateCloudType(cloudType);
                        }
                        else
                        {
                            cloudType = null;
                        }
                    }

                    metarData.Clouds.Add(
                        new CloudInfo
                        {
                            Coverage = TranslateCloudCoverage(coverage),
                            Altitude = int.TryParse(altitudePart, out var alt) ? alt * 100 : null,
                            Type = cloudType,
                        }
                    );
                    break;
                }
            }
        }
    }

    private string TranslateCloudCoverage(string code)
    {
        return code switch
        {
            "SKC" => "Sky Clear",
            "CLR" => "Clear",
            "NSC" => "No Significant Cloud",
            "NCD" => "No Cloud Detected",
            "FEW" => "Few",
            "SCT" => "Scattered",
            "BKN" => "Broken",
            "OVC" => "Overcast",
            "VV" => "Vertical Visibility",
            _ => code,
        };
    }

    private string TranslateCloudType(string code)
    {
        return code switch
        {
            "CB" => "Cumulonimbus",
            "TCU" => "Towering Cumulus",
            "CU" => "Cumulus",
            "CI" => "Cirrus",
            _ => code,
        };
    }

    private void ParseTemperature(string[] parts, MetarData metarData)
    {
        foreach (var part in parts)
        {
            if (part.Contains("/") && !part.Contains("Q") && !part.Contains("SM"))
            {
                var temps = part.Split('/');
                if (temps.Length == 2)
                {
                    var temp = temps[0].Replace("M", "-");
                    var dewpoint = temps[1].Replace("M", "-");

                    metarData.Temperature = new TemperatureInfo
                    {
                        Celsius = int.TryParse(temp, out var t) ? t : 0,
                        Dewpoint = int.TryParse(dewpoint, out var d) ? d : null,
                    };
                }
                break;
            }
        }
    }

    private void ParsePressure(string[] parts, MetarData metarData)
    {
        foreach (var part in parts)
        {
            // QNH in hectopascals (e.g., Q1013)
            if (part.StartsWith("Q") && part.Length == 5)
            {
                var pressureValue = part.Replace("Q", "");
                if (decimal.TryParse(pressureValue, out var pressure))
                {
                    metarData.Pressure = new PressureInfo { Value = pressure, Unit = "hPa" };
                }
                break;
            }
            // Altimeter in inches of mercury (e.g., A2992)
            else if (part.StartsWith("A") && part.Length == 5)
            {
                var pressureValue = part.Replace("A", "");
                if (decimal.TryParse(pressureValue, out var pressure))
                {
                    metarData.Pressure = new PressureInfo
                    {
                        Value = pressure / 100m,
                        Unit = "inHg",
                    };
                }
                break;
            }
        }
    }

    private string DetermineWeatherIcon(MetarData metarData)
    {
        // Check for severe weather first
        if (metarData.Weather?.Phenomena.Any(p => p.Contains("Thunderstorm")) == true)
            return "wi-thunderstorm";

        if (metarData.Weather?.Phenomena.Any(p => p.Contains("Fog")) == true)
            return "wi-fog";

        if (metarData.Weather?.Phenomena.Any(p => p.Contains("Mist") || p.Contains("Haze")) == true)
            return "wi-fog";

        if (metarData.Weather?.Phenomena.Any(p => p.Contains("Snow")) == true)
            return "wi-snow";

        if (
            metarData.Weather?.Phenomena.Any(p => p.Contains("Rain") || p.Contains("Drizzle"))
            == true
        )
            return "wi-rain";

        if (metarData.Weather?.Phenomena.Any(p => p.Contains("Showers")) == true)
            return "wi-showers";

        // Check cloud coverage if no weather phenomena
        if (metarData.Clouds.Any(c => c.Coverage == "Overcast"))
            return "wi-cloudy";

        if (metarData.Clouds.Any(c => c.Coverage == "Broken"))
            return "wi-cloudy";

        if (metarData.Clouds.Any(c => c.Coverage == "Scattered"))
            return "wi-day-cloudy";

        if (metarData.Clouds.Any(c => c.Coverage == "Few"))
            return "wi-day-sunny-overcast";

        if (
            metarData.Clouds.Any(c =>
                c.Coverage == "Sky Clear"
                || c.Coverage == "Clear"
                || c.Coverage == "No Significant Cloud"
                || c.Coverage == "No Cloud Detected"
            )
        )
            return "wi-day-sunny";

        // Default to sunny if no clouds reported
        return "wi-day-sunny";
    }
}
