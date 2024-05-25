using Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

internal class ChatDbContext() : DbContext(MigrationsOptions)
{
    private static readonly DbContextOptions MigrationsOptions = new DbContextOptionsBuilder()
        .UseNpgsql("Host=localhost:5432;Database=chatbot;Username=john;Password=1234;Include Error Detail=True")
        .Options;

    public DbSet<Player> Players { get; init; }
    public DbSet<Character> Characters { get; init; }
    public DbSet<ChatSession> ChatSessions { get; init; }
    public DbSet<ChatMessage> ChatMessages { get; init; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Player>()
            .HasMany(p => p.ChatSessions)
            .WithOne(cs => cs.Player)
            .HasForeignKey(cs => cs.PlayerId);

        modelBuilder.Entity<Player>()
            .HasOne(p => p.ActiveSession)
            .WithOne()
            .HasForeignKey<Player>(p => p.ActiveSessionId);
    }
}