using System.Net.Http.Json;
using ChatBot;

namespace Infrastructure.LLM;

internal class Replicate(CharacterService characterService, string replicateApiToken, ILogger logger)
{
    private static readonly string[] ListenEvents = ["start", "output", "completed"];
    private static readonly IEnumerable<ReplicateModel> IsChatPrompt = [ReplicateModel.OpenHermes2];

    // public async Task Send(ReplicateModel replicateModel, ReceivedMessage result)
    // {
    //     var prompt = IsChatPrompt.Contains(replicateModel)
    //         ? await characterService.ConvertMessageToChatPrompt(result.Message!, result.SenderId)
    //         : CharacterService.ConvertMessageToPrompt(result.Message!, result.SenderId);
    //
    //     var data = ModelToInputData(replicateModel, result.SenderId, prompt.Content);
    //
    //     await Post(data);
    // }

    public async Task SendEvaluation(string sentimentPrompt, long senderId)
    {
        var requestData = new Dictionary<string, object>
        {
            { "webhook", $"http://193.95.229.130:4434/replicateHook?userId={senderId}&evaluation=true" },
            { "webhook_events_filter", ListenEvents },
            {
                "input", new Dictionary<string, string>
                {
                    { "prompt", sentimentPrompt },
                    { "prompt_template", "{prompt}" }
                }
            }
        };

        var url = NativeModelToUrl[ReplicateModel.Mistral7BInstruct02];

        var inputData = new InputData
        {
            Data = requestData,
            Url = url
        };

        await Post(inputData);
    }

    private async Task Post(InputData data)
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromMinutes(10);
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {replicateApiToken}");

        var response = await client.PostAsJsonAsync(data.Url, data.Data);
        if (!response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            logger.LogError("Failed to POST data to {Url}. Response: {Response}", data.Url, responseContent);
        }
    }

    private InputData ModelToInputData(ReplicateModel replicateModel, long senderId, string prompt)
    {
        var requestData = new Dictionary<string, object>
        {
            { "webhook", $"http://193.95.229.130:4434/replicateHook?userId={senderId}&evaluation=false" },
            { "webhook_events_filter", ListenEvents }
        };

        if (ModelToVersion.TryGetValue(replicateModel, out var version)) requestData.Add("version", version);

        requestData.Add("input", new Dictionary<string, object>
        {
            { "top_k", 40 },
            { "top_p", 0.95 },
            { "prompt", prompt },
            { "temperature", 0.7 },
            { "max_new_tokens", 128 },
            { "length_penalty", 0.3 },
            { "stop_sequences", characterService.GetStopSequenceForChat(senderId) },
            { "prompt_template", "{prompt}" },
            { "presence_penalty", 0 }
        });

        var url = "https://api.replicate.com/v1/predictions";
        if (NativeModelToUrl.TryGetValue(replicateModel, out var value)) url = value;

        var inputData = new InputData
        {
            Data = requestData,
            Url = url
        };

        return inputData;
    }

    // ReSharper disable StringLiteralTypo
    private static readonly Dictionary<ReplicateModel, string> ModelToVersion = new()
    {
        { ReplicateModel.FlatDolphinMaid, "f90b3347117254e45120d34f1711943c0a102455b83de6064678816bfe5fccb2" },
        { ReplicateModel.Dolphin221Mistral7B, "0521a0090543fea1a687a871870e8f475d6581a3e6e284e32a2579cfb4433ecf" },
        { ReplicateModel.Mistral7BOpenOrca, "7afe21847d582f7811327c903433e29334c31fe861a7cf23c62882b181bacb88" },
        { ReplicateModel.Solar107B, "5f53237a53dab757767a5795f396cf0a638fdbe151faf064665d8f0fb346c0f9" },
        { ReplicateModel.OpenHermes2, "f48e0a295349761472cd9bf25f6e1ad6249e41dc702a928e149097cc8eca18c9" }
    };

    private static readonly Dictionary<ReplicateModel, string> NativeModelToUrl = new()
    {
        { ReplicateModel.Mistral7BInstruct02, "https://api.replicate.com/v1/models/mistralai/mistral-7b-instruct-v0.2/predictions" }
    };
    // ReSharper restore StringLiteralTypo
}

public record InputData
{
    public required Dictionary<string, object> Data;
    public required string Url;
}

public enum ReplicateModel
{
    FlatDolphinMaid,
    Dolphin221Mistral7B,
    Mistral7BOpenOrca,
    Solar107B,
    OpenHermes2,
    Mistral7BInstruct02
}

public record ReplicateResponse
{
    public required string status { get; init; }
    public required string[] output { get; init; }
}