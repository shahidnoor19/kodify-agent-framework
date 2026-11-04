using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SupportCopilot.Application;
using SupportCopilot.Infrastructure.Data;
using SupportCopilot.Infrastructure.Options;

namespace SupportCopilot.Infrastructure;

public static class DependencyInjection
{
    /// <param name="contentRoot">The app's content root; relative data paths are resolved against it.</param>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, string contentRoot)
    {
        // Fail at startup, with a clear message, instead of deep inside the first chat request.
        services.AddOptions<FoundryOptions>().BindConfiguration(FoundryOptions.Section)
            .ValidateDataAnnotations()
            .Validate(o => o.UseEntraId || !string.IsNullOrWhiteSpace(o.ApiKey),
                "Set AzureFoundry:ApiKey in user secrets, or set AzureFoundry:UseEntraId to true.")
            // Prompts and answers can contain PII: never log them outside Development, whatever the config says.
            .PostConfigure<IHostEnvironment>((o, env) => o.LogSensitiveData &= env.IsDevelopment())
            .ValidateOnStart();

        services.AddOptions<StorageOptions>().BindConfiguration(StorageOptions.Section)
            .PostConfigure(o => o.DataRoot = Path.GetFullPath(Path.Combine(contentRoot, o.DataRoot)));

        services.AddDbContext<AppDbContext>((sp, o) =>
        {
            var dataRoot = Directory.CreateDirectory(sp.GetRequiredService<IOptions<StorageOptions>>().Value.DataRoot).FullName;
            o.UseSqlite(configuration.GetConnectionString("Default") ?? $"Data Source={Path.Combine(dataRoot, "supportcopilot.db")}");
        });
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        return services;
    }

    /// <summary>Creates and seeds the database.</summary>
    public static Task InitializeDataAsync(this IServiceProvider services, string contentRoot)
    {
        using var scope = services.CreateScope();
        SeedData.Initialize(scope.ServiceProvider.GetRequiredService<AppDbContext>());
        return Task.CompletedTask;
    }
}
