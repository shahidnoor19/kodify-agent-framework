using SupportCopilot.Infrastructure;
using SupportCopilot.Agents;
using SupportCopilot.Application;

var builder = WebApplication.CreateBuilder(args);

// Aspire service defaults: OpenTelemetry, health checks, resilience, service discovery.
builder.AddServiceDefaults();

// The layers: Application (rules) → Infrastructure (EF Core, Foundry) → Web.
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.ContentRootPath);
// The application services: the rules, used by the dashboard, the agent tools and the tests.
builder.Services.AddApplication();
// The agents: keyed AIAgent singletons that share the one Foundry client.
builder.AddSupportAgents();
builder.Services.AddControllersWithViews();
// One chat request per second per user, bursts of 5: protects the Foundry quota (429s) and the budget.
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("agent", ctx => System.Threading.RateLimiting.RateLimitPartition.GetTokenBucketLimiter(
        ctx.User.Identity?.Name ?? ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
        _ => new System.Threading.RateLimiting.TokenBucketRateLimiterOptions
        {
            TokenLimit = 5, TokensPerPeriod = 1, ReplenishmentPeriod = TimeSpan.FromSeconds(1), QueueLimit = 0,
        }));
});

var app = builder.Build();

await app.Services.InitializeDataAsync(app.Environment.ContentRootPath);

if (!app.Environment.IsDevelopment()) app.UseExceptionHandler("/Home/Error");
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.MapDefaultEndpoints();
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");

app.Run();
