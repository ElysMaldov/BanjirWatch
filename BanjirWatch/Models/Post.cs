using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BanjirWatch.Models;

public class Post
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [StringLength(2000)]
    public string Content { get; set; } = string.Empty;

    [StringLength(500)]
    public string? ImagePath { get; set; }

    // Location data for mapping
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    [StringLength(200)]
    public string? LocationName { get; set; }

    public bool IsFloodReport { get; set; } = true;

    public FloodSeverity? Severity { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<Like> Likes { get; set; } = new List<Like>();
    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
}

public enum FloodSeverity
{
    Low = 1,
    Moderate = 2,
    High = 3,
    Severe = 4
}
