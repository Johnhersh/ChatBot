using System.Text;
using System.Text.Json;
using Core;
using Core.Models;

namespace Infrastructure.LLM;

internal class OpenRouter(CharacterService characterService, IConfiguration configuration, ILogger<OpenRouter> logger) : ILLMProvider
{
    private const string Url = "https://openrouter.ai/api/v1/chat/completions";

    private static readonly Dictionary<Model, string> ModelName = new()
    {
        // ReSharper disable StringLiteralTypo
        { Model.NousHermesMistral, "nousresearch/nous-hermes-2-mistral-7b-dpo" },
        { Model.Mistral7BInstructV02, "mistralai/mistral-7b-instruct:nitro" },
        { Model.MythoMaxL213B, "gryphe/mythomax-l2-13b" },
        { Model.NousHermes2ProLlama3, "nousresearch/hermes-2-pro-llama-3-8b" }, // [***--]
        { Model.NousHermesL213B, "nousresearch/nous-hermes-llama2-13b" }, // [*----]
        { Model.DolphinMixtral8X7B, "cognitivecomputations/dolphin-mixtral-8x7b" }, // [***--]
        { Model.Phi3Medium, "microsoft/phi-3-medium-128k-instruct" } // [***--]
        // ReSharper restore StringLiteralTypo
    };

    private readonly string _apiKey = configuration["OpenRouterAiApiToken"] ?? throw new NullReferenceException("No TogetherAI API Key!");

    public async Task<string> SendChat(ReceivedMessage result, ChatSession chat, CancellationToken cancellationToken)
    {
        var prompt = CharacterService.ConvertMessageToPrompt(result.Message, chat).Content;
        const Model model = Model.Phi3Medium;
        return await Post(cancellationToken, model, prompt, await characterService.GetStopSequenceForChat(result.SenderId), 250);
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
        logger.LogInformation("{Json}", json);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(Url, content, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode) logger.LogError("Failed to POST data to {Url}. Response: {Response}", Url, responseContent);

        var completionResult = JsonSerializer.Deserialize<CompletionResult>(responseContent);
        if (completionResult is null) logger.LogError("Failed to deserialize LLM response");

        return completionResult is null ? "" : completionResult.choices[0].text;
    }

    // ReSharper disable IdentifierTypo
    private enum Model
    {
        NousHermesMistral,
        Mistral7BInstructV02,
        MythoMaxL213B,
        NousHermes2ProLlama3,
        NousHermesL213B,
        DolphinMixtral8X7B,
        Phi3Medium
    }
    // ReSharper restore IdentifierTypo
}