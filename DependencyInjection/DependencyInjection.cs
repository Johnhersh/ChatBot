using Core;
using Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DependencyInjection;

public static class DependencyInjection
{
    public static void AddChatBotServices(this IServiceCollection services, IConfiguration configuration)
    {
        InfrastructureDependencyInjection.AddInfrastructureServices(services, configuration);
        CoreDependencyInjection.AddCoreServices(services, configuration);
    }
}