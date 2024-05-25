using System.Text.Json.Serialization;

namespace ChatBot.LLM;

public class Llama
{
}

// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable ClassNeverInstantiated.Global
public record LlamaRequest(
    [property: JsonPropertyName("prompt")] string Prompt,
    [property: JsonPropertyName("stop")] string[] Stop,
    [property: JsonPropertyName("n_predict")]
    int NumOfPredictTokens,
    [property: JsonPropertyName("n_keep")] int NumOfTokensToKeep,
    [property: JsonPropertyName("temperature")]
    float Temperature,
    [property: JsonPropertyName("mirostat")]
    int Mirostat,
    [property: JsonPropertyName("ignore_eos")]
    bool IgnoreEoS);

public record LlamaResponse(
    [property: JsonPropertyName("content")]
    string Content);

public record TokenizeResponse([property: JsonPropertyName("tokens")] int[] Tokens);