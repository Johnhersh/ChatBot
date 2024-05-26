using ChatBot;
using Core.Models;
using Microsoft.Extensions.Logging;
using RandomNameGeneratorNG;
using Tiktoken;

namespace Core;

public class CharacterService(ILogger<CharacterService> logger, IDatabaseFunctions databaseFunctions)
{
    private static readonly Random Random = new();
    private static readonly string[] HairColors = ["blonde", "brown", "black", "red", "auburn", "gray"];
    private static readonly string[] EyeColors = ["blue", "green", "brown", "hazel", "gray", "amber"];

    public async Task<string> StartNewSession(Player player, string userName)
    {
        if (player.ChatSessions.Count >= 3)
            return "Only 3 sessions are allowed. Please unmatch one of your matches. Use /matches to see a list of your sessions.";

        var personGenerator = new PersonNameGenerator();
        var newCharacterName = personGenerator.GenerateRandomFemaleFirstName();
        var character = new Character(newCharacterName)
        {
            HairColor = HairColors[Random.Next(HairColors.Length)],
            EyeColor = EyeColors[Random.Next(EyeColors.Length)],
            Memory = $"{userName} has greeted me.",
            ChatSession = null!
        };
        character.Prompt = Prompts.GenerateBasePrompt(character, userName);

        var chatHistory = new List<ChatMessage>
        {
            new() { Message = $"Hello {character.Name}!", SenderName = userName },
            new() { Message = $"{{Ooh a new match! Exciting!}} Hi! {userName}!", SenderName = newCharacterName },
            new() { Message = "Nice to meet you!", SenderName = userName },
            new() { Message = "{Let's reciprocate. Let's see where this goes} Not too bad, how are you doing?", SenderName = newCharacterName }
        };

        var newSession = new ChatSession
        {
            Player = player,
            Character = character,
            ChatHistory = chatHistory,
            PromptUserName = userName,
            PromptAssistantName = character.Name
        };

        player.ChatSessions.Add(newSession);
        character.TokensUsed = GetAmountOfTokens(character.Prompt);

        await databaseFunctions.StartSession(newSession);

        return $"Started new chat with {character.Name}:\n\nHi {userName}! How are you today?";
    }

    public static CharacterServiceResult ConvertMessageToPrompt(string input, ChatSession chat)
    {
        chat.ChatHistory.Add(new ChatMessage { Message = input, SenderName = chat.PromptUserName });
        return new CharacterServiceResult(true, Prompts.GetPromptMarkdown(chat.Character, chat.ChatHistory), chat.PromptAssistantName);
    }

    public async Task<CharacterServiceResult> ConvertMessageToChatPrompt(string input, long chatId)
    {
        var chat = await databaseFunctions.GetActiveSessionByTelegramId(chatId);
        if (chat is null) return new CharacterServiceResult(false, "Chat not initialized. Use '/start ScreenName' to begin.", "");

        chat.ChatHistory.Add(new ChatMessage { Message = input, SenderName = chat.PromptUserName });
        return new CharacterServiceResult(true, Prompts.GetPromptChat(chat.Character, chat.ChatHistory), chat.PromptAssistantName);
    }

    public AddAiOutputResult AddAiOutputToChat(ChatSession chat, string chatResult)
    {
        var cleanedResponse = chatResult.Replace($"{chat.Character.Name}: ", "").Replace($"<|im_start|>{chat.Character.Name}", "").Trim();
        var lastCharacter = cleanedResponse.Last();
        var isLetter = char.IsLetter(lastCharacter);
        var isSeparator = lastCharacter is ',';
        if (isLetter || isSeparator)
        {
            // Find the index of the last non-alphabetical character
            var index = cleanedResponse.Length - 1;
            while (index >= 0 && cleanedResponse[index] is not ('.' or '!')) index--;

            // It's possible the text had no sentence-end characters
            if (index > 0) cleanedResponse = cleanedResponse[..(index + 1)];
            logger.LogWarning("Received cut-off text with that's a single sentence");
            logger.LogWarning("Input: {Input}", cleanedResponse);
        }

        var trimmed = cleanedResponse.Trim();
        chat.ChatHistory.Add(new ChatMessage { Message = trimmed, SenderName = chat.PromptAssistantName });

        if (chat.ChatHistory.Count <= 25) return new AddAiOutputResult(true, trimmed);

        var oldMessages = chat.ChatHistory.GetRange(0, 6);
        chat.ChatHistory.RemoveRange(0, 6);
        return new AddAiOutputResult(true, trimmed, oldMessages);
    }

    public async Task<string?> GetLastAssistantMessage(long chatId)
    {
        var chatSession = await databaseFunctions.GetActiveSessionByTelegramId(chatId);
        if (chatSession is null) return null;

        var lastAssistantMessage = chatSession.ChatHistory.LastOrDefault(s => s.SenderName == chatSession.Character.Name);
        if (lastAssistantMessage is not null) return lastAssistantMessage.Message;

        logger.LogWarning("Tried getting an assistant message but no message found");
        return null;
    }

    public async Task<string> GetSentimentPrompt(int numberOfMessages, long chatId)
    {
        var chatSession = await databaseFunctions.GetActiveSessionByTelegramId(chatId);
        if (chatSession is null) throw new NullReferenceException("Cannot find chat session");

        var lastMessages = chatSession.ChatHistory.TakeLast(numberOfMessages);
        var sentimentPrompt = Prompts.GetSentimentEvaluationPrompt(lastMessages, chatSession.Character, chatSession.PromptUserName);
        return sentimentPrompt;
    }

    public async Task<string> GetSummaryPrompt(long chatId, List<ChatMessage> newMessages)
    {
        var chatSession = await databaseFunctions.GetActiveSessionByTelegramId(chatId);
        if (chatSession is null) throw new NullReferenceException("Cannot find chat session");

        return Prompts.GetMemorySummaryPrompt(chatSession.PromptAssistantName, chatSession.PromptUserName, chatSession.Character.Memory, newMessages);
    }

    public async Task UpdateMemory(long chatId, string newMemory)
    {
        var chatSession = await databaseFunctions.GetActiveSessionByTelegramId(chatId);
        if (chatSession is null) throw new NullReferenceException("Cannot find chat session");

        chatSession.Character.Memory = newMemory.Trim();
    }

    public async Task<InterestResponse> UpdateInterest(string chatResult, long chatId)
    {
        var chatSession = await databaseFunctions.GetActiveSessionByTelegramId(chatId);
        if (chatSession is null) throw new NullReferenceException("Cannot find chat session");
        var interestShift = chatResult.Contains("POSITIVE") ? 1
            : chatResult.Contains("NEGATIVE") ? -1 : 0;

        chatSession.InterestScore += interestShift;
        if (chatSession.InterestScore == 0)
        {
            await databaseFunctions.RemoveActiveSessionByTelegramId(chatId);
            return new InterestResponse
            {
                ErrorMessage = $"You have been blocked by {chatSession.PromptAssistantName}. Better luck next time!",
                DidInterestUpgrade = false
            };
        }

        if (chatSession.InterestScore == 10) chatSession.Character.Prompt = Prompts.GenerateBasePromptLevel2(chatSession.Character, chatSession.PromptUserName);
        if (chatSession.InterestScore == 30) chatSession.Character.Prompt = Prompts.GenerateBasePromptLevel3(chatSession.Character, chatSession.PromptUserName);

        await databaseFunctions.UpdateDbWithChanges();
        return new InterestResponse
        {
            ErrorMessage = "",
            DidInterestUpgrade = chatSession.InterestScore is 10 or 30
        };
    }

    private static int GetAmountOfTokens(string input)
    {
        var encoder = ModelToEncoder.For("gpt-4");
        var numberOfTokens = encoder.CountTokens(input);

        return numberOfTokens;
    }

    public async Task<string[]> GetStopSequenceForChat(long chatId)
    {
        var chatSession = await databaseFunctions.GetActiveSessionByTelegramId(chatId);
        if (chatSession is null) throw new NullReferenceException("Cannot find chat session");

        string[] result = [$"{chatSession.PromptUserName}:", $"{chatSession.PromptAssistantName}:", """\n\n""", "</s>", "\\nUser "];
        return result;
    }
}

public record CharacterServiceResult(bool Success, string Content, string CharacterName, string? ErrorMessage = null);

public record AddAiOutputResult(bool Success, string Content, List<ChatMessage>? RemovedMessages = null, string? ErrorMessage = null);

public record InterestResponse
{
    public required string ErrorMessage { get; init; }
    public required bool DidInterestUpgrade { get; init; }
}