using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using File = System.IO.File;

namespace ChatBot.Telegram;

internal class ConfigureWebhook : IHostedService
{
    private readonly string _domain;
    private readonly ILogger<ConfigureWebhook> _logger;
    private readonly IServiceProvider _services;
    private readonly string _telegramToken;

    public ConfigureWebhook(ILogger<ConfigureWebhook> logger,
        IServiceProvider serviceProvider,
        IConfiguration config)
    {
        _logger = logger;
        _services = serviceProvider;
        var telegramApiKey = config["TelegramAPIKey"];
        var domainName = config["Domain"];
        if (telegramApiKey is null || domainName is null) throw new NullReferenceException("Cannot configure the webhook, settings are missing");
        _telegramToken = telegramApiKey;
        _domain = domainName;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();

        // Configure custom endpoint per Telegram API recommendations:
        // https://core.telegram.org/bots/api#setwebhook
        // If you'd like to make sure that the Webhook request comes from Telegram, we recommend
        // using a secret path in the URL, e.g. https://www.example.com/<token>.
        // Since nobody else knows your bot's token, you can be pretty sure it's us.
        var webhookAddress = @$"{_domain}/bot/{_telegramToken}";
        var certificate = new InputFileStream(File.OpenRead("Telegram/Certificates/CosmicCompanion.pem"));
        _logger.LogInformation("Setting webhook: {WebhookAddress}", webhookAddress);
        await botClient.SetWebhookAsync(
            certificate: certificate,
            url: webhookAddress,
            allowedUpdates: new[] { UpdateType.Message, UpdateType.CallbackQuery },
            dropPendingUpdates: true,
            cancellationToken: cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();

        // Remove webhook upon app shutdown
        _logger.LogInformation("Removing webhook");
        await botClient.DeleteWebhookAsync(cancellationToken: cancellationToken);
    }
}