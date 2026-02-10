namespace BanjirWatch.Models.ViewModels;

public class MapViewModel
{
    public double CenterLatitude { get; set; } = -6.2088; // Default: Jakarta
    public double CenterLongitude { get; set; } = 106.8456;
    public int Zoom { get; set; } = 12;
    public List<MapPointViewModel> FloodPoints { get; set; } = new();
    public List<MapPointViewModel> UserReports { get; set; } = new();
}

public class MapPointViewModel
{
    public int Id { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ImagePath { get; set; }
    public string Type { get; set; } = string.Empty; // "api" or "user"
    public int? Severity { get; set; }
    public DateTime CreatedAt { get; set; }
}
