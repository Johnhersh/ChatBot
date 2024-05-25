using ChatBot;
using Core;
using Infrastructure.Data;
using Infrastructure.LLM;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure;

public static class InfrastructureDependencyInjection
{
    public static void AddInfrastructureServices(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres");
        if (string.IsNullOrEmpty(connectionString)) throw new NullReferenceException("No Connection String!");
        services.AddDbContext<ChatDbContext>(options =>
        {
            options.UseNpgsql(connectionString,
                o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery));
        });

        services.AddScoped<IDatabaseFunctions, DatabaseFunctions>();
        services.AddScoped<ILLMProvider, OpenRouter>();
    }
}