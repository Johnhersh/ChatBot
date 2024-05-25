using System.ComponentModel.DataAnnotations;

namespace Core.Models;

public record Character(string Name)
{
    public int Id { get; init; }
    public string Prompt { get; set; } = "";
    public int? TokensUsed { get; set; }
    [MaxLength(20)] public required string HairColor { get; init; }
    [MaxLength(20)] public required string EyeColor { get; init; }
    public required string Memory { get; set; }

    public int ChatSessionId { get; set; }
    public required ChatSession ChatSession { get; set; }
}