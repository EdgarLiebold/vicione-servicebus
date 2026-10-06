using System.Globalization;
using System.Text.Json;
using ViciOneReview.CorePackage05;

if (args.Length != 3)
    throw new ArgumentException("Usage: CorePackage05.PublicConsumer <case|all> <operationTimeout> <callerDeadline> (TimeSpan invariant format)");
TimeSpan operationTimeout = ParseDeadline(args[1]);
TimeSpan callerDeadline = ParseDeadline(args[2]);
var cases = new List<(string Name, Func<TimeSpan, CancellationToken, Task> Run)>();
foreach (bool inner in new[] { false, true })
{
    cases.Add(($"results-membership-{(inner ? "inner" : "plain")}", (_, _) => ResultsAssertions.MembershipAsync(inner)));
    cases.Add(($"results-null-{(inner ? "inner" : "plain")}", (_, _) => ResultsAssertions.NullSequenceAsync(inner)));
}
cases.Add(("results-empty", (_, _) => ResultsAssertions.EmptyAsync()));
foreach (string property in new[] { "StartTimeout", "StopTimeout", "ConsumerStopTimeout" })
foreach (string boundary in new[] { "null", "maximum", "fraction", "too-large", "maxvalue", "zero", "infinite" })
    cases.Add(($"host-{property}-{boundary}", (_, _) => HostTimeoutAssertions.BoundsAsync(property, boundary)));
foreach (string variant in new[] { "greater", "equal", "unset-stop" })
    cases.Add(($"host-relation-{variant}", (_, _) => HostTimeoutAssertions.RelationAsync(variant)));
foreach (string boundary in new[] { "null", "maximum", "fraction" })
    cases.Add(($"host-runtime-{boundary}", (timeout, caller) => HostTimeoutAssertions.HostedRuntimeAsync(boundary, timeout, caller)));
foreach (string boundary in new[] { "maximum", "fraction", "too-large" })
    cases.Add(($"cts-custom-{boundary}", (_, _) => HostTimeoutAssertions.CustomProviderCtsAsync(boundary)));
foreach (string variant in new[] { "default-too-large", "default-maxvalue", "system-maximum", "system-fraction", "system-ordinary", "custom-long-size", "custom-long-expire" })
    cases.Add(($"batch-{variant}", (timeout, caller) => BatchTimerAssertions.RunAsync(variant, timeout, caller)));
var selected = cases.Where(x => args[0] == "all" || x.Name == args[0]).ToArray();
if (selected.Length == 0)
    throw new ArgumentException("Unknown case. Allowed: " + string.Join(", ", cases.Select(x => x.Name)));
var results = new List<object>();
int failures = 0;
foreach (var item in selected)
{
    using var caller = new CancellationTokenSource(callerDeadline);
    try
    {
        caller.Token.ThrowIfCancellationRequested();
        await item.Run(operationTimeout, caller.Token);
        results.Add(new { @case = item.Name, assertions_passed = true });
    }
    catch (Exception exception)
    {
        failures++;
        results.Add(new { @case = item.Name, assertions_passed = false,
            exception_type = exception.GetType().FullName, exception.Message, exception.StackTrace,
            bounded_guard_failure = HasGuardFailure(exception) });
    }
}
var runtime = new {
    framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
    processArchitecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(),
    coreLibPath = typeof(object).Assembly.Location,
    coreLibSha256 = HashAssembly(typeof(object).Assembly.Location),
    loadedProductAssemblies = AppDomain.CurrentDomain.GetAssemblies()
        .Where(a => a.GetName().Name is "ViciOne.ServiceBus" or "ViciOne.ServiceBus.Abstractions")
        .OrderBy(a => a.GetName().Name)
        .Select(a => new { name = a.GetName().Name, path = a.Location, sha256 = HashAssembly(a.Location) }).ToArray()
};
Console.WriteLine(JsonSerializer.Serialize(new { cases = results, count = selected.Length, failures, runtime }));
return failures == 0 ? 0 : 1;

static bool HasGuardFailure(Exception failure) => failure is TimeoutException or OperationCanceledException
    || (failure is AggregateException aggregate && aggregate.InnerExceptions.Any(HasGuardFailure))
    || (failure.InnerException is { } inner && HasGuardFailure(inner));
static TimeSpan ParseDeadline(string text)
{
    TimeSpan value = TimeSpan.Parse(text, CultureInfo.InvariantCulture);
    if (value <= TimeSpan.Zero || (long)value.TotalMilliseconds > uint.MaxValue - 1L)
        throw new ArgumentOutOfRangeException(nameof(text), "Deadline must be positive and supported by the .NET timer.");
    return value;
}

static string HashAssembly(string path) => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
