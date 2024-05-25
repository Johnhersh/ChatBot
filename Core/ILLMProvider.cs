using Core.Models;

namespace Core;

public interface ILLMProvider
{
    public Task<string> SendChat(ReceivedMessage result, ChatSession chat, CancellationToken cancellationToken);

    public Task<string> SendEvaluation(long senderId, CancellationToken cancellationToken);

    public Task<string> SendSummary(long senderId, List<ChatMessage> oldMessages, CancellationToken cancellationToken);
}

public record ReceivedMessage
{
    public bool Error { get; set; }
    public required string Message { get; set; }
    public long SenderId { get; init; }
}