using System.Text;
using System.Text.Json;
using ChatBot;
using Core;
using Core.Models;

namespace Infrastructure.LLM;

internal class TogetherAi(CharacterService characterService, IConfiguration configuration, ILogger<TogetherAi> logger) : ILLMProvider
{
    private const string Url = "https://api.together.xyz/v1/completions";

    private static readonly Dictionary<Model, string> ModelName = new()
    {
        { Model.NousHermesMistral, "NousResearch/Nous-Hermes-2-Mistral-7B-DPO" },
        { Model.Mistral7BInstructV02, "mistralai/Mistral-7B-Instruct-v0.2" }
    };

    private readonly string _apiKey = configuration["TogetherAiApiToken"] ?? throw new NullReferenceException("No TogetherAI API Key!");

    public async Task<string> SendChat(ReceivedMessage result, ChatSession chat, CancellationToken cancellationToken)
    {
        var prompt = CharacterService.ConvertMessageToPrompt(result.Message, chat).Content;
        return await Post(cancellationToken, Model.NousHermesMistral, prompt, await characterService.GetStopSequenceForChat(result.SenderId), 250, 1.25f);
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

    private async Task<string> Post(CancellationToken cancellationToken, Model model, string prompt, string[]? stopSequence, int maxToken, float repetitionPenalty = 1f)
    {
        var requestData = new Dictionary<string, object>
        {
            { "model", ModelName[model] },
            { "prompt", prompt },
            { "max_tokens", maxToken },
            { "repetition_penalty", repetitionPenalty }
        };

        if (stopSequence is not null) requestData.Add("stop", stopSequence);

        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromMinutes(10);
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

        var content = new StringContent(JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json");

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
        Mistral7BInstructV02
    }
}