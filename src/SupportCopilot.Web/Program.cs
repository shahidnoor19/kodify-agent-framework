using SupportCopilot.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Aspire service defaults: OpenTelemetry, health checks, resilience, service discovery.
builder.AddServiceDefaults();

// The layers: Application (rules) → Infrastructure (EF Core, Foundry) → Web.
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.ContentRootPath);
builder.Services.AddControllersWithViews();

var app = builder.Build();

await app.Services.InitializeDataAsync(app.Environment.ContentRootPath);

if (!app.Environment.IsDevelopment()) app.UseExceptionHandler("/Home/Error");
app.UseStaticFiles();
app.UseRouting();
app.MapDefaultEndpoints();
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");

app.Run();
