using System.Runtime.Serialization;
using System.Text;
using ChatBot.Telegram;
using Core;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Telegram.Bot.Types;

namespace ChatBot.Routes;

public static class RoutesDefinitions
{
    public static void MapEndpoints(this WebApplication app, string telegramApiKey)
    {
        app.MapGet("/", () => "Hello World!");

        // app.MapPost("/Chat",
        //     async ([FromBody] SendMessageInput input, [FromServices] ChatServiceMessageQueue chatService,
        //             CancellationToken cancellationToken) =>
        //         await chatService.Send(input, cancellationToken));

        // app.MapPost("/replicateHook",
        //     async (long userId,
        //         bool evaluation,
        //         ReplicateResponse replicateData,
        //         [FromServices] TelegramCommands telegramCommands,
        //         [FromServices] ChatServiceExternalCalls chatService,
        //         [FromServices] CharacterService characterService) =>
        //     {
        //         var isSuccess = replicateData.status == "succeeded";
        //         var finishedEvaluation = isSuccess && evaluation;
        //         var newChatMessage = isSuccess && !evaluation;
        //         var processingChat = !isSuccess && !evaluation;
        //
        //         var replicateOutput = string.Concat(replicateData.output);
        //
        //         if (finishedEvaluation)
        //         {
        //             var result = await chatService.ReceiveNewLLMEvaluationResult(replicateOutput, userId);
        //             await telegramCommands.SendMessage(result, userId);
        //             return TypedResults.Ok();
        //         }
        //
        //         if (newChatMessage)
        //         {
        //             // var result = await chatService.ReceiveNewLLMMessage(replicateOutput, userId);
        //             // if (result.Error) await telegramCommands.SendMessage(result.Message, userId);
        //             // else await chatService.EvaluateInterestReplicate(result);
        //         }
        //
        //         if (processingChat && replicateData.output.Length is 1 or 2) await telegramCommands.SendTyping(userId);
        //
        //         return TypedResults.Ok();
        //     });

        app.MapPost($"bot/{telegramApiKey}", async (
            CancellationToken cancellationToken,
            HttpRequest request,
            [FromServices] TelegramCommands telegramCommands,
            [FromServices] IServiceProvider serviceProvider) =>
        {
            // For some reason the Telegram package requires deserialization with newtonsoft
            var buffer = new byte[Convert.ToInt32(request.ContentLength)];
            var result = await request.Body.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
            if (result != buffer.Length) throw new InvalidOperationException();
            var requestContent = Encoding.UTF8.GetString(buffer);
            var data = JsonConvert.DeserializeObject<Update>(requestContent);
            if (data is null) throw new SerializationException("Cannot deserialize request from Telegram!");

            await telegramCommands.ReceiveTelegramInput(data, cancellationToken);

            return Results.Ok();
        });
    }
}