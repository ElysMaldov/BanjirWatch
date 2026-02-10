using System.ComponentModel.DataAnnotations;

namespace BanjirWatch.Models;

/// <summary>
/// External flood data fetched from weather/flood APIs
/// </summary>
public class FloodData
{
    [Key]
    public int Id { get; set; }

    [Required]
    public double Latitude { get; set; }

    [Required]
    public double Longitude { get; set; }

    /// <summary>
    /// Severity level from 0-100 (percentage or API-specific scale)
    /// </summary>
    public int Severity { get; set; }

    [StringLength(50)]
    public string Source { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Precipitation amount in mm
    /// </summary>
    public double? Precipitation { get; set; }

    /// <summary>
    /// River water level in meters
    /// </summary>
    public double? WaterLevel { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this data expires (for cleanup)
    /// </summary>
    public DateTime ExpiresAt { get; set; }
}
