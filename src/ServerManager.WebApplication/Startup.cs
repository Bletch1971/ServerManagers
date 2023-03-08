using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ServerManager.WebApplication.Extensions;
using ServerManager.WebApplication.Middleware;

namespace ServerManager.WebApplication;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();

        services.AddResponseCaching();

        /*
         * https://github.com/Microsoft/aspnet-api-versioning/wiki
         */
        services.AddApiVersioning(o =>
        {
            o.DefaultApiVersion = ApiVersion.Default;
            o.AssumeDefaultVersionWhenUnspecified = true;
            o.ReportApiVersions = true;
            o.ApiVersionReader = ApiVersionReader.Combine(
                new MediaTypeApiVersionReader("Version"),
                new HeaderApiVersionReader("X-Version")
            );
        });

        services.AddVersionedApiExplorer(o =>
        {
            // add the versioned api explorer, which also adds IApiVersionDescriptionProvider service
            // note: the specified format code will format the version as "'v'major[.minor][-status]"
            o.GroupNameFormat = "'v'VVV";
        });

        services.AddServerQueryServices(Configuration);

        services.AddSwaggerGen(o =>
        {
            o.OperationFilter<SwaggerDefaultValues>();
        });

        services.AddHealthChecks();
        services.AddApplicationInsightsTelemetry();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IApiVersionDescriptionProvider provider)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        var enableSwagger = Configuration.GetValue<bool>("EnableSwagger");
        if (enableSwagger)
        {
            var swaggerRoutePrefix = Configuration.GetValue<string>("SwaggerRoutePrefix");
            if (!string.IsNullOrWhiteSpace(swaggerRoutePrefix) && !swaggerRoutePrefix.EndsWith("/"))
            {
                swaggerRoutePrefix += "/";
            }

            app.UseSwagger();
            app.UseSwaggerUI(o =>
            {
                // build a swagger endpoint for each discovered API version
                foreach (var description in provider.ApiVersionDescriptions)
                {
                    o.SwaggerEndpoint($"/{swaggerRoutePrefix}swagger/{description.GroupName}/swagger.json", $"Server Managers API {description.GroupName.ToUpperInvariant()}");
                }
            });
        }

        app.UseHttpsRedirection();

        app.UseRouting();

        app.UseResponseCaching();

        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapHealthChecks("/api/health", new HealthCheckOptions()
            {
                AllowCachingResponses = false
            });
        });
    }
}
