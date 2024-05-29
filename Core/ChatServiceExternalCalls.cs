using Core.Models;
using Microsoft.Extensions.Logging;

namespace Core;

public class ChatServiceExternalCalls(ILLMProvider llmProvider, CharacterService characterService, ILogger<ChatServiceExternalCalls> logger)
{
    private readonly ILogger _logger = logger;

    public async Task<string?> Send(ChatSession chatSession, ReceivedMessage newMessage, CancellationToken cancellationToken)
    {
        var llmResult = await llmProvider.SendChat(newMessage, chatSession, cancellationToken);
        var addChatResult = characterService.AddAiOutputToChat(chatSession, $"{{{llmResult}");

        var lastClosingCurlyBraceIndex = addChatResult.Content.LastIndexOf('}');
        var messageStartIndex = lastClosingCurlyBraceIndex + 2;
        var shouldRetry = lastClosingCurlyBraceIndex != -1 && messageStartIndex >= addChatResult.Content.Length;

        if (shouldRetry || !addChatResult.Success)
        {
            _logger.LogWarning("Received message with only inner-thoughts: {Message}", addChatResult.Content);
            llmResult = await llmProvider.SendChat(newMessage, chatSession, cancellationToken);
            addChatResult = characterService.AddAiOutputToChat(chatSession, $"{{{llmResult}");
            lastClosingCurlyBraceIndex = addChatResult.Content.LastIndexOf('}');
            messageStartIndex = lastClosingCurlyBraceIndex + 2;
            var retrySuccess = lastClosingCurlyBraceIndex != -1 && messageStartIndex >= addChatResult.Content.Length;

            if (!retrySuccess || !addChatResult.Success)
            {
                _logger.LogError("Received message with only inner-thoughts: {Message}", addChatResult.Content);
                return "We encountered some BS error.. sorry... Try again maybe?";
            }
        }

        if (!addChatResult.Success)
        {
            if (addChatResult.ErrorMessage == "Identical")
            {
                _logger.LogWarning("Trying again");
                llmResult = await llmProvider.SendChat(newMessage, chatSession, cancellationToken);
                addChatResult = characterService.AddAiOutputToChat(chatSession, $"{{{llmResult}");
            }

            if (!addChatResult.Success)
            {
                _logger.LogError("Error adding message: {NewMessage}", addChatResult.ErrorMessage);
                return addChatResult.ErrorMessage;
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
        var messageWithoutInnerThoughts = lastClosingBraceIndex != -1 ? evalPrefix + addChatResult.Content[(lastClosingBraceIndex + 2)..] : addChatResult.Content;

        return messageWithoutInnerThoughts;
    }

    private async Task<string> GetEvaluationPrefix(string incomingLLMMessage, long userId)
    {
        var newInterest = await characterService.UpdateInterest(incomingLLMMessage, userId);
        if (newInterest.ErrorMessage.Length > 0) return newInterest.ErrorMessage;
        return newInterest.DidInterestUpgrade ? "😍 " : "";
    }
}