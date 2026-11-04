using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;

namespace SupportCopilot.Tests;

/// <summary>A test that needs the real Foundry model. It is skipped (not failed) when the key is not in user secrets.</summary>
public sealed class LiveFactAttribute : FactAttribute
{
    public LiveFactAttribute([CallerFilePath] string? sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = -1)
        : base(sourceFilePath, sourceLineNumber)
    {
        var key = new ConfigurationBuilder().AddUserSecrets<LiveFactAttribute>().Build()["AzureFoundry:ApiKey"];
        if (string.IsNullOrWhiteSpace(key)) Skip = "No AzureFoundry:ApiKey in user secrets: live model test skipped.";
    }
}
