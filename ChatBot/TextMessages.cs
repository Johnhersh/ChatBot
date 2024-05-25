using Core.Models;

namespace ChatBot;

/// <summary>
///     Handle all user-facing text here
/// </summary>
public static class TextMessages
{
    public static string Matches(Player player)
    {
        return
            $"""
             Your matches:
             {string.Join(Environment.NewLine, player.ChatSessions.Select(session => $"{session.PromptAssistantName}\n/session_{session.Id} | /unmatch_{session.Id}\n"))}
             """;
    }
}