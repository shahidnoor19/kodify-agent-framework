using Microsoft.Extensions.DependencyInjection;
using SupportCopilot.Application.Security;
using SupportCopilot.Application.Services;

namespace SupportCopilot.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // One accessor for the whole app: the caller itself lives in an AsyncLocal, per flow of work.
        services.AddSingleton<ICallerContextAccessor, CallerContextAccessor>();
        services.AddScoped<OrderService>();
        services.AddScoped<TicketService>();
        return services;
    }
}
