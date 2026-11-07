using Microsoft.Extensions.DependencyInjection;
using SupportCopilot.Application.Services;

namespace SupportCopilot.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<OrderService>();
        services.AddScoped<TicketService>();
        return services;
    }
}
