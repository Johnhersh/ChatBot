using Core.Models;
using Microsoft.Extensions.Logging;

namespace Core;

public class ChatServiceExternalCalls(ILLMProvider llmProvider, CharacterService characterService, ILogger<ChatServiceExternalCalls> logger)
{
    private readonly ILogger _logger = logger;

    public async Task<string?> Send(ChatSession chatSession, ReceivedMessage newMessage, CancellationToken cancellationToken)
    {
        var llmResult = await llmProvider.SendChat(newMessage, chatSession, cancellationToken);
        var addChatResult = characterService.AddAiOutputToChat(chatSession, $"{llmResult}");

        if (!addChatResult.Success)
        {
            _logger.LogWarning("Received bad message: {Message}", llmResult);
            chatSession.ChatHistory.RemoveAt(chatSession.ChatHistory.Count - 1); // SendChat() will add the user's message to the history. If we fail we have to undo that
            llmResult = await llmProvider.SendChat(newMessage, chatSession, cancellationToken);
            addChatResult = characterService.AddAiOutputToChat(chatSession, $"{llmResult}");

            if (!addChatResult.Success)
            {
                _logger.LogError("Received message twice with only inner-thoughts: {Message}", llmResult);
                return "We encountered some BS error.. sorry... Try again maybe?";
            }
        }

        if (addChatResult.RemovedMessages is not null)
        {
            var result = await llmProvider.SendSummary(newMessage.SenderId, addChatResult.RemovedMessages, cancellationToken);
            await characterService.UpdateMemory(newMessage.SenderId, result);
        }

        // Evaluate interest
        var evaluationResult = await llmProvider.SendEvaluation(newMessage.SenderId, cancellationToken);
        var evalPrefix = await GetEvaluationPrefix(evaluationResult, newMessage.SenderId);

        var lastClosingBraceIndex = addChatResult.Content.LastIndexOf('}');
        try
        {
            var messageWithoutInnerThoughts = lastClosingBraceIndex != -1 ? evalPrefix + addChatResult.Content[(lastClosingBraceIndex + 2)..] : addChatResult.Content;
            return messageWithoutInnerThoughts;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            logger.LogError("{Content}", addChatResult.Content);
            throw;
        }
    }

    private async Task<string> GetEvaluationPrefix(string incomingLLMMessage, long userId)
    {
        var newInterest = await characterService.UpdateInterest(incomingLLMMessage, userId);
        if (newInterest.ErrorMessage.Length > 0) return newInterest.ErrorMessage;
        return newInterest.DidInterestUpgrade ? "😍 " : "";
    }
}