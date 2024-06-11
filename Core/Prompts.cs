using System.Reflection;
using System.Text;
using System.Text.Json;
using Core.Models;

namespace Core;

public static class Prompts
{
    private static string ReadResource(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        // var resources = assembly.GetManifestResourceNames();
        using var stream = assembly.GetManifestResourceStream(resourceName);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public static string GenerateBasePrompt(Character assistant, string userName)
    {
        var template = ReadResource("Core.Prompts.BasePrompt.md");
        return ParseTemplate(template, userName, assistant);
    }

    private static string ParseTemplate(string template, string userName, Character assistant)
    {
        return template
            .Replace("```UserName```", userName)
            .Replace("```AssistantName```", assistant.Name)
            .Replace("```HairColor```", assistant.HairColor)
            .Replace("```EyeColor```", assistant.EyeColor);
    }

    public static string GenerateBasePromptLevel2(Character assistant, string userName)
    {
        var template = ReadResource("Core.Prompts.BasePromptLevel2.md");
        return ParseTemplate(template, userName, assistant);
    }

    public static string GenerateBasePromptLevel3(Character assistant, string userName)
    {
        var template = ReadResource("Core.Prompts.BasePromptLevel3.md");
        return ParseTemplate(template, userName, assistant);
    }

    public static string GetPrompt(Character assistant, List<string> chatHistory)
    {
        var sb = new StringBuilder();
        sb.AppendLine(assistant.Prompt);
        sb.AppendLine("Conversation:");
        sb.AppendLine();
        foreach (var chat in chatHistory) sb.AppendLine(chat);
        sb.AppendLine($"{assistant.Name}: ");
        return sb.ToString();
    }

    public static string GetPromptMarkdown(Character assistant, List<ChatMessage> chatHistory)
    {
        var sb = new StringBuilder();
        sb.AppendLine(assistant.Prompt);
        sb.AppendLine("");
        sb.AppendLine($"# {assistant.Name.ToUpper()}'s MEMORY");
        sb.AppendLine(assistant.Memory);

        sb.AppendLine("");
        sb.AppendLine("# CONVERSATION");

        // Append all chat messages except for the last one (User's input)
        for (var i = 0; i < chatHistory.Count - 1; i++) sb.AppendLine($"{chatHistory[i].SenderName}: {chatHistory[i].Message}");
        chatHistory.ForEach(message => sb.AppendLine($"{message.SenderName}: {message.Message}"));

        sb.Append($"{assistant.Name}: {{");
        return sb.ToString();
    }

    public static string GetPromptChat(Character assistant, List<ChatMessage> chatHistory)
    {
        var result = new List<Dictionary<string, string>>
        {
            new() { { "role", "system" }, { "content", assistant.Prompt } }
        };

        foreach (var chatMessage in chatHistory)
        {
            var role = chatMessage.SenderName == assistant.Name ? "system" : "user";

            result.Add(new Dictionary<string, string>
            {
                { "role", role },
                { "content", chatMessage.Message }
            });
        }

        return JsonSerializer.Serialize(result);
    }

    public static string GetPromptChatMarkupLang(Character assistant, List<string> chatHistory)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<|im_start|>system");
        sb.AppendLine(assistant.Prompt);
        sb.AppendLine("<|im_end|>");

        foreach (var message in chatHistory)
        {
            var parts = message.Split(':');
            var name = parts[0].Trim().Replace(":", "");
            var messageText = parts[1].Trim();
            sb.AppendLine($"<|im_start|>{name}");
            sb.Append(messageText);
            sb.AppendLine("<|im_end|>");
        }

        return sb.ToString();
    }

    public static string GetSentimentEvaluationPrompt(IEnumerable<ChatMessage> chatHistory, Character assistant, string userName)
    {
        var sb = new StringBuilder();

        sb.AppendLine("### Instruction:");
        sb.AppendLine($"You are provided a brief conversation between {assistant.Name} and {userName}:");
        sb.AppendLine();
        sb.AppendLine("Conversation:");
        foreach (var message in chatHistory) sb.AppendLine($"{message.SenderName}: {message.Message}");

        sb.AppendLine();
        sb.AppendLine($"Classify {assistant.Name}'s message. The choices are:");
        sb.AppendLine($"NEGATIVE - If {assistant.Name} disliked or found offensive what {userName} said.");
        sb.AppendLine($"POSITIVE - If {assistant.Name} is excited or happy or agrees with what {userName} said.");
        sb.AppendLine();
        sb.AppendLine("Include either NEGATIVE or POSITIVE.");
        sb.AppendLine();
        sb.AppendLine("### Response:");
        sb.Append("Let's think step by step.");

        return sb.ToString();
    }

    public static string GetMemorySummaryPrompt(string characterName, string userName, string existingMemory, IEnumerable<ChatMessage> newChat)
    {
        var template = ReadResource("Core.Prompts.MemorySummaryPrompt.md");
        var newMessages = string.Join(Environment.NewLine, newChat.Select(chat => $"{chat.SenderName}: {chat.Message}"));

        return template
            .Replace("```characterName```", characterName)
            .Replace("```userName```", userName)
            .Replace("```existingMemory```", existingMemory)
            .Replace("```newMessages```", newMessages);
    }
}