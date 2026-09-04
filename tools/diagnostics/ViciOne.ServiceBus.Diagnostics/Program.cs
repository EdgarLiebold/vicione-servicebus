using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

#nullable enable
namespace ViciOne.ServiceBus.Diagnostics;
/// <summary>
/// Deliberately started diagnostics. Neither scenario gates anything: the exit code is non zero only
/// when the scenario could not run at all, never because a number was worse than another number.
/// </summary>
internal static class Program
{
    /// <summary>Internal so the command boundary can be driven directly, including its failure exits.</summary>
    internal static async Task<int> Main(string[] args)
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
        // Unsubscribed again before this method returns, and before the source above is disposed. An
        // anonymous handler stood here and was never removed, so every invocation in one process left
        // one behind, each holding a cancellation source that had already been disposed; the next
        // Ctrl+C would have reached all of them.
        using IDisposable interrupt = HandleCancellation(cancellation, handler => Console.CancelKeyPress += handler,
            handler => Console.CancelKeyPress -= handler);

        // Resolved as soon as the options are complete, and used by every exit below. The failure paths
        // used to build a fresh empty option set of their own, so a --output that had already been
        // parsed was thrown away: publish-load --messages 0 --output <file> returned 1, wrote its
        // structured failure to stdout and left the requested file absent. An option set that could not
        // be read at all still has no sink, because then nothing was successfully parsed.
        string? sink = null;

        try
        {
            if (known.Length == 0)
                throw new ArgumentException($"unknown scenario '{args[0]}'. It is bus-lifecycle or publish-load");

            Dictionary<string, string> options = ParseOptions(args, known);
            sink = options.TryGetValue("output", out var path) ? path : null;

            var cycles = Number(options, "cycles", 240);
            var sampleEvery = Number(options, "sample-every", 20);

            if (args[0] == "bus-lifecycle" && sampleEvery > cycles)
                throw new ArgumentException($"--sample-every {sampleEvery} exceeds --cycles {cycles}, so nothing between the first and the last cycle would ever be sampled");

            object result = args[0] switch
            {
                "bus-lifecycle" => await BusLifecycleScenario.RunAsync(cycles, sampleEvery, cancellation.Token),
                "publish-load" => await PublishLoadScenario.RunAsync(
                    Number(options, "messages", 100_000), Number(options, "concurrency", 32),
                    Number(options, "prefetch", 10_000),
                    TimeSpan.FromSeconds(Number(options, "completion-limit-seconds", 180)), cancellation.Token),
                _ => throw new ArgumentException($"unknown scenario '{args[0]}'")
            };

            return await DeliverAsync(result, sink, cancellation.Token);
        }
        catch (OperationCanceledException)
        {
            await ReportAsync(new { scenario = args[0], status = "cancelled", reason = "cancelled before the scenario could finish, so nothing was measured" },
                sink, CancellationToken.None);

            return 1;
        }
        catch (Exception exception)
        {
            // Structured on failure too, and into the same sink: a caller that reads the output of a
            // success has to be able to read the output of a failure without switching to parsing
            // prose, and without looking somewhere else for it.
            await ReportAsync(new { scenario = args[0], status = "failed", error = exception.Message },
                sink, CancellationToken.None);

            return 1;
        }
    }

    /// <summary>
    /// Subscribes a cancellation handler and gives back the way to remove it again.
    /// <para>
    /// The two event operations are parameters so that this can be shown to subscribe once and
    /// unsubscribe once without touching the real console of the test process.
    /// </para>
    /// </summary>
    internal static IDisposable HandleCancellation(CancellationTokenSource cancellation,
        Action<ConsoleCancelEventHandler> subscribe, Action<ConsoleCancelEventHandler> unsubscribe)
    {
        void Requested(object? sender, ConsoleCancelEventArgs eventArgs)
        {
            eventArgs.Cancel = true;
            cancellation.Cancel();
        }

        subscribe(Requested);

        return new Unsubscribe(() => unsubscribe(Requested));
    }


    sealed class Unsubscribe :
        IDisposable
    {
        Action? _remove;

        public Unsubscribe(Action remove)
        {
            _remove = remove;
        }

        public void Dispose()
        {
            // Once, whatever the caller does: a second dispose would remove a handler somebody else
            // subscribed in the meantime.
            Interlocked.Exchange(ref _remove, null)?.Invoke();
        }
    }


    /// <summary>
    /// The one place a result leaves this process, whether the run succeeded or failed.
    /// <para>
    /// The fallback is bounded and not recursive. A sink that cannot be written is itself a failure,
    /// and the failure path used to report it to the very sink that had just failed; the run then
    /// ended on the second exception instead of on its own result.
    /// </para>
    /// </summary>
    /// <summary>
    /// The exit code of a run whose scenario measured what it was asked to measure.
    /// <para>
    /// The scenario succeeding and the caller receiving the result are two things, and this is the
    /// second one: a run that could not write the file it was told to write did not do what was asked,
    /// whatever it measured. Both scenarios require a broker, so the delivery result is named and
    /// handled explicitly instead of being hidden in an unreachable no-broker success branch.
    /// </para>
    /// </summary>
    /// <param name="result">The result used by the operation.</param>
    /// <param name="sink">The sink used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <param name="output">The output used by the operation.</param>
    /// <param name="error">The error used by the operation.</param>
    internal static async Task<int> DeliverAsync(object result, string? sink, CancellationToken cancellationToken,
        TextWriter? output = null, TextWriter? error = null)
    {
        return await ReportAsync(result, sink, cancellationToken, output, error) ? 0 : 1;
    }

    /// <summary>
    /// Writes the result where the caller asked for it, and says whether that is where it went.
    /// <para>
    /// The two writers are the seam. They defaulted to the real console, so a case could observe that
    /// this method did not throw and nothing else: the fallback could have dropped the result on the
    /// floor and every case about it stayed green. Measured - removing the line that writes the result
    /// left the whole fixture passing.
    /// </para>
    /// <para>
    /// A caller that asked for a file and did not get one has not had its request carried out, whatever
    /// the scenario itself measured, so the answer here is the delivery and not the measurement. The
    /// result is still written to the output the caller can read, because losing it as well would
    /// help nobody.
    /// </para>
    /// </summary>
    /// <returns>True when the result reached the place the caller named.</returns>
    /// <param name="result">The result used by the operation.</param>
    /// <param name="sink">The sink used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <param name="output">The output used by the operation.</param>
    /// <param name="error">The error used by the operation.</param>
    internal static async Task<bool> ReportAsync(object result, string? sink, CancellationToken cancellationToken,
        TextWriter? output = null, TextWriter? error = null)
    {
        TextWriter destination = output ?? Console.Out;
        TextWriter diagnostics = error ?? Console.Error;
        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });

        if (sink is null)
        {
            await destination.WriteLineAsync(json);

            return true;
        }

        try
        {
            await File.WriteAllTextAsync(sink, json + Environment.NewLine, cancellationToken);

            return true;
        }
        catch (Exception unwritable) when (unwritable is IOException or UnauthorizedAccessException
                                               or NotSupportedException or ArgumentException)
        {
            await diagnostics.WriteLineAsync($"the result could not be written to '{sink}': {unwritable.Message}");
            await destination.WriteLineAsync(json);

            return false;
        }
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
