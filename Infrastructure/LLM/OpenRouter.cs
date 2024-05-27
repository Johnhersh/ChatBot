using System.Text;
using System.Text.Json;
using ChatBot;
using Core;
using Core.Models;

namespace Infrastructure.LLM;

internal class OpenRouter(CharacterService characterService, IConfiguration configuration, ILogger<OpenRouter> logger) : ILLMProvider
{
    private const string Url = "https://openrouter.ai/api/v1/chat/completions";

    private static readonly Dictionary<Model, string> ModelName = new()
    {
        { Model.NousHermesMistral, "nousresearch/nous-hermes-2-mistral-7b-dpo" },
        { Model.Mistral7BInstructV02, "mistralai/mistral-7b-instruct:nitro" },
        { Model.MythoMaxL213B, "gryphe/mythomax-l2-13b" }
    };

    private readonly string _apiKey = configuration["OpenRouterAiApiToken"] ?? throw new NullReferenceException("No TogetherAI API Key!");

    public async Task<string> SendChat(ReceivedMessage result, ChatSession chat, CancellationToken cancellationToken)
    {
        var prompt = CharacterService.ConvertMessageToPrompt(result.Message, chat).Content;
        return await Post(cancellationToken, Model.MythoMaxL213B, prompt, await characterService.GetStopSequenceForChat(result.SenderId), 250, 1.17f);
    }

    public async Task<string> SendEvaluation(long senderId, CancellationToken cancellationToken)
    {
        var sentimentPrompt = await characterService.GetSentimentPrompt(2, senderId);
        return await Post(cancellationToken, Model.Mistral7BInstructV02, sentimentPrompt, await characterService.GetStopSequenceForChat(senderId), 200);
    }

    public async Task<string> SendSummary(long senderId, List<ChatMessage> oldMessages, CancellationToken cancellationToken)
    {
        var summaryPrompt = await characterService.GetSummaryPrompt(senderId, oldMessages);
        var result = await Post(cancellationToken, Model.Mistral7BInstructV02, summaryPrompt, ["###"], 300);
        return result;
    }

    private async Task<string> Post(CancellationToken cancellationToken, Model model, string prompt, string[]? stopSequence, int maxToken, float? repetitionPenalty = null)
    {
        var requestData = new Dictionary<string, object>
        {
            { "model", ModelName[model] },
            { "prompt", prompt },
            { "max_tokens", maxToken }
        };

        if (repetitionPenalty is not null) requestData.Add("repetition_penalty", repetitionPenalty);
        if (stopSequence is not null) requestData.Add("stop", stopSequence);

        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromMinutes(10);
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

        var json = JsonSerializer.Serialize(requestData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(Url, content, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode) logger.LogError("Failed to POST data to {Url}. Response: {Response}", Url, responseContent);

        var completionResult = JsonSerializer.Deserialize<CompletionResult>(responseContent);
        if (completionResult is null) logger.LogError("Failed to deserialize LLM response");

        return completionResult is null ? "" : completionResult.choices[0].text;
    }

    private enum Model
    {
        NousHermesMistral,
        Mistral7BInstructV02,
        MythoMaxL213B
    }
}