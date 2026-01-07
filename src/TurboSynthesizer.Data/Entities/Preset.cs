using System;
using System.ComponentModel.DataAnnotations;

namespace TurboSynthesizer.Data.Entities;

public class Preset
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string Category { get; set; } = "User";
    
    public string SubCategory { get; set; } = "General";
    
    public string ParametersJson { get; set; } = "{}";
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
