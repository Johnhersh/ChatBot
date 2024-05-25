using ChatBot;
using ChatBot.Routes;
using ChatBot.Telegram;
using Core;
using DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("env.local.json", true, true);
builder.Configuration.AddEnvironmentVariables();
builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.AddFilter("Microsoft", LogLevel.Warning);
    if (builder.Environment.IsDevelopment()) loggingBuilder.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Information);
});
builder.Services.AddCors();

builder.Services.AddChatBotServices(builder.Configuration);

// Services
builder.Services.AddScoped<CharacterService>();

// Telegram
var telegramApiKey = builder.Configuration["TelegramAPIKey"];
if (telegramApiKey is null) throw new NullReferenceException("No Telegram API Key!");
builder.AddTelegramService(telegramApiKey);

// Build the application
var app = builder.Build();

app.UseCors();
app.MapEndpoints(telegramApiKey);

app.Urls.Add("https://*:4433");
app.Urls.Add("http://*:4434");

app.Run();