using System.ComponentModel.DataAnnotations;

namespace Core.Models;

public class ChatSession
{
    public int Id { get; init; }
    public required Character Character { get; init; }
    public int PlayerId { get; init; }
    public Player Player { get; init; } = null!;
    public required List<ChatMessage> ChatHistory { get; init; }
    [MaxLength(50)] public required string PromptUserName { get; init; }
    [MaxLength(50)] public required string PromptAssistantName { get; init; }
    public int InterestScore { get; set; } = 2; // Has to be at least 2 so there's some chance at creating a message pair
}