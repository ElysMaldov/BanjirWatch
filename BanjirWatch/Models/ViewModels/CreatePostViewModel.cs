using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace BanjirWatch.Models.ViewModels;

public class CreatePostViewModel
{
    [StringLength(2000, ErrorMessage = "Content cannot exceed 2000 characters")]
    public string Content { get; set; } = string.Empty;

    public IFormFile? Image { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    [StringLength(200, ErrorMessage = "Location name cannot exceed 200 characters")]
    public string? LocationName { get; set; }

    public bool IsFloodReport { get; set; } = true;

    public FloodSeverity? Severity { get; set; }
}
