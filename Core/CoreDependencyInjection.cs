using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core;

public static class CoreDependencyInjection
{
    public static void AddCoreServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ChatServiceExternalCalls>();
    }
}