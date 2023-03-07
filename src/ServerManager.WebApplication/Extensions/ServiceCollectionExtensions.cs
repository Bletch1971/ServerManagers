using Microsoft.Extensions.Configuration;

namespace ServerManager.WebApplication.Extensions;

public static class ServiceCollectionExtensions
{
    public static T GetSectionAs<T>(this IConfiguration configuration, string sectionName = null)
    {
        if (string.IsNullOrWhiteSpace(sectionName))
            sectionName = typeof(T).Name;

        return configuration.GetSection(sectionName).Get<T>();
    }
}
