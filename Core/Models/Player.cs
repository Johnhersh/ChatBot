namespace Core.Models;

public class Player
{
    public int Id { get; init; }
    public required List<ChatSession> ChatSessions { get; init; } = [];
    public int? ActiveSessionId { get; init; }
    public ChatSession? ActiveSession { get; set; }
    public long? TelegramId { get; init; }
}