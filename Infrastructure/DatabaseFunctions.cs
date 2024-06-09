using Core;
using Core.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure;

internal class DatabaseFunctions(ChatDbContext chatDbContext) : IDatabaseFunctions
{
    public async Task UpdateDbWithChanges()
    {
        await chatDbContext.SaveChangesAsync();
    }

    public async Task StartSession(ChatSession newSession)
    {
        chatDbContext.ChatSessions.Add(newSession);
        await chatDbContext.SaveChangesAsync();
        newSession.Player.ActiveSession = newSession;
        await chatDbContext.SaveChangesAsync();
    }

    public async Task<ChatSession?> GetActiveSessionByTelegramId(long telegramId)
    {
        var player = await chatDbContext.Players
            .Include(p => p.ActiveSession)
            .ThenInclude(s => s.ChatHistory)
            .Include(p => p.ActiveSession)
            .ThenInclude(s => s.Character)
            .FirstOrDefaultAsync(player => player.TelegramId == telegramId);

        return player?.ActiveSession;
    }

    public async Task RemoveActiveSessionByTelegramId(long telegramId)
    {
        var player = await chatDbContext.Players
            .Include(p => p.ActiveSession)
            .FirstAsync(player => player.TelegramId == telegramId);

        if (player.ActiveSession is not null) chatDbContext.ChatSessions.Remove(player.ActiveSession);
        await chatDbContext.SaveChangesAsync();
    }

    public async Task<Player> CreateNewPlayer(long? telegramId = null)
    {
        var newPlayer = chatDbContext.Players.Add(new Player { TelegramId = telegramId, ChatSessions = [] });
        await chatDbContext.SaveChangesAsync();
        return newPlayer.Entity;
    }

    public async Task<Player?> GetPlayerByTelegramId(long telegramId)
    {
        return await chatDbContext.Players
            .Include(player => player.ChatSessions)
            .FirstOrDefaultAsync(player => player.TelegramId == telegramId);
    }

    public async Task<string> Unmatch(Player player, string? sessionId)
    {
        if (!int.TryParse(sessionId, out var sessionIdInt)) return "Invalid session provided";

        var session = await chatDbContext.ChatSessions
            .Where(cs => cs.Id == sessionIdInt && cs.PlayerId == player.Id)
            .SingleOrDefaultAsync();

        if (session is null) return "Match not found";

        chatDbContext.ChatSessions.Remove(session);
        await chatDbContext.SaveChangesAsync();

        return "Unmatched successfully";
    }

    public async Task<string> ActivateSession(Player player, string? sessionId)
    {
        if (!int.TryParse(sessionId, out var sessionIdInt)) return "Invalid session provided";

        var session = await chatDbContext.ChatSessions
            .Where(cs => cs.Id == sessionIdInt && cs.PlayerId == player.Id)
            .SingleOrDefaultAsync();

        if (session is null) return "Match not found";

        player.ActiveSession = session;
        await chatDbContext.SaveChangesAsync();

        return $"Now chatting with {session.PromptAssistantName}";
    }
}