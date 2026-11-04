// .NET Aspire: one F5 starts the web app, and opens the dashboard with logs, traces and metrics.
var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.SupportCopilot_Web>("web")
    .WithExternalHttpEndpoints();

builder.Build().Run();
