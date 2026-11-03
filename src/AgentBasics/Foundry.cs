using System.ClientModel;
using Microsoft.Extensions.Configuration;
using OpenAI;

namespace AgentBasics;

/// <summary>Reads the Foundry settings (user secrets) and creates the client for the Foundry OpenAI v1 endpoint.</summary>
public static class Foundry
{
    public static IConfiguration Config { get; } = new ConfigurationBuilder()
        .AddUserSecrets(typeof(Foundry).Assembly)
        .Build();

    public static string ChatDeployment => Config["AzureFoundry:ChatDeployment"] ?? "gpt-4.1-mini";

    public static OpenAIClient CreateClient()
    {
        var endpoint = Config["AzureFoundry:Endpoint"]
            ?? throw new InvalidOperationException("Set AzureFoundry:Endpoint with dotnet user-secrets.");
        var apiKey = Config["AzureFoundry:ApiKey"]
            ?? throw new InvalidOperationException("Set AzureFoundry:ApiKey with dotnet user-secrets.");

        // https://<resource>.openai.azure.com/openai/v1/
        var v1 = new Uri(new Uri(endpoint), "/openai/v1/");
        return new OpenAIClient(new ApiKeyCredential(apiKey), new OpenAIClientOptions { Endpoint = v1 });
    }
}
