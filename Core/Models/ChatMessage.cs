using System.ComponentModel.DataAnnotations;

namespace Core.Models;

public class ChatMessage
{
    public int Id { get; init; }
    [Required] public int ChatSessionId { get; init; } // This has to be required so the FK is non-nullable and cascade deleting the session deletes the messages
    [Required] public required ChatSession ChatSession { get; init; }
    public required string Message { get; init; }
    public required string SenderName { get; init; }
}