using System.Runtime.CompilerServices;

namespace AgentBasics.Tests;

/// <summary>A test that needs the real Foundry model. It is skipped (not failed) when the key is not in user secrets.</summary>
public sealed class LiveFactAttribute : FactAttribute
{
    public LiveFactAttribute([CallerFilePath] string? sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = -1)
        : base(sourceFilePath, sourceLineNumber)
    {
        if (string.IsNullOrWhiteSpace(Foundry.Config["AzureFoundry:ApiKey"]))
            Skip = "No AzureFoundry:ApiKey in user secrets: live model test skipped.";
    }
}
