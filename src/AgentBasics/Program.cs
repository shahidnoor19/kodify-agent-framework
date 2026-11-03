using AgentBasics.Demos;

// dotnet run -- 03   runs the demo for episode 3, and so on.
var demo = args.FirstOrDefault() ?? "03";

Func<Task> run = demo switch
{
    "03" => FirstAgent.RunAsync,
    "04" => Streaming.RunAsync,
    "05" => ChatClients.RunAsync,
    "06" => StructuredOutput.RunAsync,
    "07" => ModelsAndCost.RunAsync,
    _ => () => { Console.WriteLine("Demos: 03 first agent, 05 chat clients"); return Task.CompletedTask; }
    ,
};

await run();
