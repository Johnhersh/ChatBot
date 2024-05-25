// ReSharper disable InconsistentNaming
// ReSharper disable CollectionNeverUpdated.Global

internal record CompletionResult
{
    public required List<Choice> choices { get; init; }
    public required Usage usage { get; init; }
}

// ReSharper disable ClassNeverInstantiated.Global
internal record Choice
{
    public required string text { get; set; }
    public required string finish_reason { get; set; }
}

internal class Usage
{
    public int prompt_tokens { get; set; }
    public int completion_tokens { get; set; }
    public int total_tokens { get; set; }
}