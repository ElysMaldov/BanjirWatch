namespace BanjirWatch.DTOs;

// Open-Meteo API Response DTOs
public class OpenMeteoResponseDto
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public CurrentWeatherDto? Current { get; set; }
}

public class CurrentWeatherDto
{
    public DateTime Time { get; set; }
    public int Interval { get; set; }
    public double? Temperature_2m { get; set; }
    public int? Relative_Humidity_2m { get; set; }
    public double? Precipitation { get; set; }
    public double? Rain { get; set; }
    public double? Showers { get; set; }
    public int? WeatherCode { get; set; }
    public int? CloudCover { get; set; }
    public double? Pressure_Msl { get; set; }
    public double? Surface_Pressure { get; set; }
    public double? Wind_Speed_10m { get; set; }
}

// DTO for flood risk calculation result
public class FloodRiskDto
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int Severity { get; set; }
    public double Precipitation { get; set; }
    public int WeatherCode { get; set; }
    public string Description { get; set; } = string.Empty;
    public string RiskLevel { get; set; } = string.Empty;
}

// DTO for flood data from API (before mapping to domain model)
public class FloodDataDto
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int Severity { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Precipitation { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
