using Core.Models;

namespace Core;

public interface IDatabaseFunctions
{
    public Task StartSession(ChatSession newSession);
    public Task<Player?> GetFullPlayerByTelegramId(long telegramId);
    public Task RemoveActiveSessionByTelegramId(long telegramId);
    public Task UpdateDbWithChanges();
    public Task<Player?> GetEmptyPlayerByTelegramId(long telegramId);
    public Task<Player> CreateNewPlayer(long? telegramId = null);
    public Task<string> ActivateSession(Player player, string? sessionId);
    public Task<string> Unmatch(Player player, string? sessionId);
}