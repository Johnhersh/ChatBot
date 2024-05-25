using Core;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ChatBot.Telegram;

internal class TelegramCommands(
    ITelegramBotClient botClient,
    ILogger<TelegramCommands> logger,
    ChatServiceExternalCalls chatService,
    CharacterService characterService,
    IDatabaseFunctions databaseFunctions)
{
    public async Task ReceiveTelegramInput(Update update, CancellationToken cancellationToken)
    {
        var handler = update.Type switch
        {
            // UpdateType.Unknown:
            // UpdateType.ChannelPost:
            // UpdateType.EditedChannelPost:
            // UpdateType.ShippingQuery:
            // UpdateType.PreCheckoutQuery:
            // UpdateType.Poll:
            UpdateType.Message => ProcessMessageFromTelegram(update.Message!, cancellationToken),
            // UpdateType.EditedMessage      => BotOnMessageReceived(update.EditedMessage!),
            UpdateType.CallbackQuery => BotOnCallbackQueryReceived(update.CallbackQuery!),
            // UpdateType.InlineQuery        => BotOnInlineQueryReceived(update.InlineQuery!),
            _ => UnknownUpdateHandlerAsync(update)
        };

        try
        {
            await handler;
        }
#pragma warning disable CA1031
        catch (Exception exception)
#pragma warning restore CA1031
        {
            await HandleErrorAsync(exception);
        }
    }

    private async Task ProcessMessageFromTelegram(Message message, CancellationToken cancellationToken)
    {
        var chatId = message.Chat.Id;
        var messageText = message.Text;
        if (message.From is null || messageText is null) return; // Stop here if something is wrong with any of the fields of the message

        if (messageText[..1] == "/") await ExecuteCommand(messageText, chatId);
        else await ProcessTextMessage(messageText, chatId, cancellationToken);
    }

    private async Task ProcessTextMessage(string messageText, long chatId, CancellationToken cancellationToken)
    {
        var chatSession = await databaseFunctions.GetActiveSessionByTelegramId(chatId);
        if (chatSession is null)
        {
            await SendReply("Chat not initialized. Use '/start ScreenName' to begin.", null, null, chatId);
            return;
        }

        var result = await chatService.Send(chatSession, new ReceivedMessage
        {
            Message = messageText,
            SenderId = chatId
        }, cancellationToken);

        if (result != "") await SendReply(result, null, null, chatId);
    }

    private async Task ExecuteCommand(string messageText, long chatId)
    {
        // Split the message by spaces to separate the command from the parameters
        var splitBySpaces = messageText.Split(" ");

        // Extract the command part (before any spaces), this will have configuration parameters still
        var commandWithConfig = splitBySpaces[0].TrimStart('/');

        // Split the command from its configuration parameter if it exists
        var commandParts = commandWithConfig.Split('_');
        var command = commandParts[0];
        var config = commandParts.Length > 1 ? commandParts[1] : null;

        // Extract user-provided parameters (everything after the command)
        var userParametersCombined = string.Join(" ", splitBySpaces.Skip(1));

        try
        {
            var player = await databaseFunctions.GetPlayerByTelegramId(chatId) ?? await databaseFunctions.CreateNewPlayer(chatId);

            var reply = command switch
            {
                "start" => userParametersCombined.Length > 0 ? await characterService.StartNewSession(player, userParametersCombined) : "A screen name is required to start",
                "matches" => TextMessages.Matches(player),
                "session" => await databaseFunctions.ActivateSession(player, config),
                "unmatch" => await databaseFunctions.Unmatch(player, config),
                _ => "Unknown command"
            };
            await SendReply(reply, null, null, chatId);
        }
        catch (Exception e)
        {
            await SendReply($"Cannot process command {messageText}:\n{e.Message}", null, null, chatId);
            logger.LogError("Exception while processing command: {Command}", command);
        }
    }

    private async Task SendReply(string? message, string? photoId, string? photoCaption, long chatId, IReplyMarkup? replyKeyboardMarkup = null)
    {
        if (photoCaption is not null && photoCaption.Length >= 1024)
        {
            // Telegram has a hard limit of 1024 characters on photo captions. Ideally we wouldn't be here.
            message = photoCaption;
            photoId = null;
        }

        // if (photoId is not null)
        // await _botClient.SendPhotoAsync(
        //     chatId,
        //     photoId,
        //     photoCaption,
        //     ParseMode.Html,
        //     replyMarkup: replyKeyboardMarkup);

        if (message is not null)
            await botClient.SendTextMessageAsync(
                chatId,
                message is ""
                    ? "Nothing to show"
                    : message, parseMode // TODO Add test to make sure we never return an empty string, move this to somewhere generic
                : ParseMode.Html
            );
    }

    // Process Inline Keyboard callback data
    private async Task BotOnCallbackQueryReceived(CallbackQuery callbackQuery)
    {
        await botClient.AnswerCallbackQueryAsync(
            callbackQuery.Id,
            $"Received {callbackQuery.Data}");

        // await RunCosmicCompanionCommand(new Message
        // {
        //     Chat = callbackQuery.Message!.Chat,
        //     Text = callbackQuery.Data,
        //     From = callbackQuery.From.Id
        // });
    }

    private Task UnknownUpdateHandlerAsync(Update update)
    {
        logger.LogInformation("Unknown update type: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }

    private Task HandleErrorAsync(Exception exception)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        logger.LogInformation("HandleError: {ErrorMessage}", errorMessage);
        return Task.CompletedTask;
    }

    public async Task SendTyping(long chatId)
    {
        await botClient.SendChatActionAsync(
            chatId,
            ChatAction.Typing
        );
    }

    public async Task SendMessage(string message, long chatId)
    {
        try
        {
            await botClient.SendTextMessageAsync(
                chatId,
                message
            );
        }
        catch (ApiRequestException e)
        {
            // if (e.ErrorCode is 403 or 400) await _dbFunctions.DeletePlayer(chatId);
            logger.LogError("Error sending message:");
            logger.LogError("{Message}", message);
            logger.LogError("To user: {ChatId}", chatId);
            logger.LogError("Error Message: {Message}", e.Message);
        }
    }
}