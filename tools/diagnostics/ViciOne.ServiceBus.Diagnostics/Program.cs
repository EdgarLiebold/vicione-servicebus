namespace ViciOne.ServiceBus.Diagnostics;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        string[] known = args[0] switch
        {
            "bus-lifecycle" => ["cycles", "sample-every", "output"],
            "publish-load" => ["messages", "concurrency", "prefetch", "completion-limit-seconds", "output"],
            _ => []
        };

        using var cancellation = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellation.Cancel();
        };

        Dictionary<string, string> options;
        try
        {
            if (known.Length == 0)
                throw new ArgumentException($"unknown scenario '{args[0]}'. It is bus-lifecycle or publish-load");

            options = ParseOptions(args, known);

            var cycles = Number(options, "cycles", 240);
            var sampleEvery = Number(options, "sample-every", 20);

            if (args[0] == "bus-lifecycle" && sampleEvery > cycles)
                throw new ArgumentException($"--sample-every {sampleEvery} exceeds --cycles {cycles}, so nothing between the first and the last cycle would ever be sampled");

            object result = args[0] switch
            {
                "bus-lifecycle" => await BusLifecycleScenario.Run(cycles, sampleEvery, cancellation.Token),
                "publish-load" => await PublishLoadScenario.Run(
                    Number(options, "messages", 100_000), Number(options, "concurrency", 32),
                    Number(options, "prefetch", 10_000),
                    TimeSpan.FromSeconds(Number(options, "completion-limit-seconds", 180)), cancellation.Token),
                _ => throw new ArgumentException($"unknown scenario '{args[0]}'")
            };

            await Report(result, options, cancellation.Token);

            return 0;
        }
        catch (OperationCanceledException)
        {
            await Report(new { scenario = args[0], status = "cancelled", reason = "cancelled before the scenario could finish, so nothing was measured" },
                new Dictionary<string, string>(StringComparer.Ordinal), CancellationToken.None);

            return 1;
        }
        catch (Exception exception)
        {
            // Structured on failure too: a caller that reads the output of a success has to be able to
            // read the output of a failure without switching to parsing prose.
            await Report(new { scenario = args[0], status = "failed", error = exception.Message },
                new Dictionary<string, string>(StringComparer.Ordinal), CancellationToken.None);

            return 1;
        }
    }

    static async Task Report(object result, Dictionary<string, string> options, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });

        if (options.TryGetValue("output", out var path))
            await File.WriteAllTextAsync(path, json + Environment.NewLine, cancellationToken);
        else
            Console.WriteLine(json);
    }

    /// <summary>
    /// Reads the options, and refuses anything it cannot account for.
    /// <para>
    /// An unknown option is almost always a typo, and silently ignoring it means the run measured
    /// something other than what was asked for. A repeated option means two intentions and no way to
    /// tell which one was meant. An option without a value is a value that was forgotten.
    /// </para>
    /// </summary>
    internal static Dictionary<string, string> ParseOptions(string[] args, IReadOnlyCollection<string> known)
    {
        var options = new Dictionary<string, string>(StringComparer.Ordinal);

        for (var index = 1; index < args.Length; index++)
        {
            var token = args[index];

            if (!token.StartsWith("--", StringComparison.Ordinal))
                throw new ArgumentException($"'{token}' is not an option and this scenario takes no positional arguments");

            var name = token.Substring(2);
            if (!known.Contains(name))
            {
                throw new ArgumentException(
                    $"--{name} is not an option of this scenario. It takes: {string.Join(", ", known.Select(o => "--" + o))}");
            }

            if (index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal))
                throw new ArgumentException($"--{name} was given without a value");

            if (!options.TryAdd(name, args[index + 1]))
                throw new ArgumentException($"--{name} was given more than once, so it names two different runs");

            index++;
        }

        return options;
    }

    internal static int Number(Dictionary<string, string> options, string name, int fallback)
    {
        if (!options.TryGetValue(name, out var value))
            return fallback;

        return int.TryParse(value, out var number) && number > 0
            ? number
            : throw new ArgumentException($"--{name} must be a positive number, not '{value}'");
    }

    internal const string Usage = """
        Deliberately started diagnostics against the pinned RabbitMQ fixture of a run.

          bus-lifecycle [--cycles 240] [--sample-every 20] [--output <file>]
          publish-load  [--messages 100000] [--concurrency 32] [--prefetch 10000]
                        [--completion-limit-seconds 180] [--output <file>]

        Start them through the canonical runner, which owns the fixture:

          python3 tools/ci/run_broker_category.py --broker rabbitmq --command -- \
            dotnet run --project tools/diagnostics/ViciOne.ServiceBus.Diagnostics -c Release -- \
            publish-load --messages 100000

        Neither reads a default host, port or account, so neither runs outside that environment, and
        neither gates a build.
        """;
}
