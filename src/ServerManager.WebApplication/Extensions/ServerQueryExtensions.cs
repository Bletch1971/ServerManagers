using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServerManager.WebApplication.Models.Data;
using ServerManager.WebApplication.Services;

namespace ServerManager.WebApplication.Extensions;

public static class ServerQueryExtensions
{
    public static IServiceCollection AddServerQueryServices(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSectionAs<ServerQuerySettings>();
        services.AddSingleton(settings);
        
        services.AddScoped<IServerQueryService, QueryMasterService>();
        return services;
    }
}
