using Telegram.Bot;

namespace ChatBot.Telegram;

internal static class TelegramService
{
    public static void AddTelegramService(this WebApplicationBuilder builder, string telegramApiKey)
    {
        builder.Services.AddHostedService<ConfigureWebhook>();
        builder.Services.AddHttpClient("tgwebhook")
            .AddTypedClient<ITelegramBotClient>(httpClient => new TelegramBotClient(telegramApiKey, httpClient));
        builder.Services.AddScoped<TelegramCommands>();
        builder.Configuration["Kestrel:Certificates:Default:Path"] = "Telegram/Certificates/CosmicCompanion.pem";
        builder.Configuration["Kestrel:Certificates:Default:KeyPath"] = "Telegram/Certificates/CosmicCompanion.key";
    }
}