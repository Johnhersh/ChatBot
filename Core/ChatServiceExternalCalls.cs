using ChatBot;
using Core.Models;
using Microsoft.Extensions.Logging;

namespace Core;

public class ChatServiceExternalCalls(ILLMProvider llmProvider, CharacterService characterService, ILogger<ChatServiceExternalCalls> logger)
{
    private readonly ILogger _logger = logger;

    public async Task<string?> Send(ChatSession chatSession, ReceivedMessage newMessage, CancellationToken cancellationToken)
    {
        var llmResult = await llmProvider.SendChat(newMessage, chatSession, cancellationToken);
        var addChatResult = characterService.AddAiOutputToChat(chatSession, llmResult);

        if (addChatResult.RemovedMessages is not null)
        {
            var result = await llmProvider.SendSummary(newMessage.SenderId, addChatResult.RemovedMessages, cancellationToken);
            await characterService.UpdateMemory(newMessage.SenderId, result);
        }

        if (!addChatResult.Success)
        {
            _logger.LogError("Error adding message: {NewMessage}", addChatResult.ErrorMessage);
            return addChatResult.ErrorMessage;
        }

        // Evaluate interest
        var evaluationResult = await llmProvider.SendEvaluation(newMessage.SenderId, cancellationToken);
        var messageAfterEval = await ReceiveNewLLMEvaluationResult(evaluationResult, newMessage.SenderId);

        return messageAfterEval;
    }

    private async Task<string> ReceiveNewLLMEvaluationResult(string incomingLLMMessage, long userId)
    {
        var lastMessage = await characterService.GetLastAssistantMessage(userId);
        var newInterest = await characterService.UpdateInterest(incomingLLMMessage, userId);

        if (newInterest.ErrorMessage.Length > 0) return lastMessage + "\n\n" + newInterest.ErrorMessage;

        if (lastMessage is null) throw new Exception("Cannot find any assistant messages");
        if (newInterest.DidInterestUpgrade) lastMessage = "😍 " + lastMessage;

        return lastMessage;
    }
}