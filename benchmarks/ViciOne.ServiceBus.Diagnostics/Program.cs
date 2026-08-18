namespace ViciOne.ServiceBus.Diagnostics;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;


/// <summary>
/// Deliberately started diagnostics. Neither scenario gates anything: the exit code is non zero only
/// when the scenario could not run at all, never because a number was worse than another number.
/// </summary>
internal static class Program
{
    static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help")
        {
            Console.Error.WriteLine(Usage);
            return args.Length == 0 ? 2 : 0;
        }

        Dictionary<string, string> options = ParseOptions(args);

        using var cancellation = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellation.Cancel();
        };

        try
        {
            object result = args[0] switch
            {
                "bus-lifecycle" => await BusLifecycleScenario.Run(
                    Number(options, "cycles", 240), Number(options, "sample-every", 20), cancellation.Token),
                "publish-load" => await PublishLoadScenario.Run(
                    Number(options, "messages", 100_000), Number(options, "concurrency", 32),
                    Number(options, "prefetch", 10_000),
                    TimeSpan.FromSeconds(Number(options, "completion-limit-seconds", 180)), cancellation.Token),
                _ => throw new ArgumentException($"unknown scenario '{args[0]}'")
            };

            var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });

            if (options.TryGetValue("output", out var path))
                await File.WriteAllTextAsync(path, json + Environment.NewLine, cancellation.Token);
            else
                Console.WriteLine(json);

            return 0;
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("cancelled before the scenario could finish, so nothing is reported");
            return 1;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    static Dictionary<string, string> ParseOptions(string[] args)
    {
        var options = new Dictionary<string, string>(StringComparer.Ordinal);

        for (var index = 1; index < args.Length - 1; index++)
        {
            if (args[index].StartsWith("--", StringComparison.Ordinal))
                options[args[index].Substring(2)] = args[index + 1];
        }

        return options;
    }

    static int Number(Dictionary<string, string> options, string name, int fallback)
    {
        if (!options.TryGetValue(name, out var value))
            return fallback;

        return int.TryParse(value, out var number) && number > 0
            ? number
            : throw new ArgumentException($"--{name} must be a positive number, not '{value}'");
    }

    const string Usage = """
        Deliberately started diagnostics against the pinned RabbitMQ fixture of a run.

          bus-lifecycle [--cycles 240] [--sample-every 20] [--output <file>]
          publish-load  [--messages 100000] [--concurrency 32] [--prefetch 10000]
                        [--completion-limit-seconds 180] [--output <file>]

        Both read the broker of the run from the environment the canonical runner publishes and refuse
        to start without it. Neither gates a build.
        """;
}
