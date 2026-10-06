using System.Globalization;
using System.Text.Json;
using ViciOneReview.F018.Gate;

// Three explicit arguments: case (or all), operation timeout, caller deadline.
if (args.Length != 3)
    throw new ArgumentException("Usage: F018.IdentityGateConsumer <case|all> <operationTimeout> <callerDeadline> (TimeSpan invariant format)");
TimeSpan operationTimeout = ParseDeadline(args[1]);
TimeSpan callerDeadline = ParseDeadline(args[2]);
var cases = new List<(string Name, bool Query, bool Foreign)>();
foreach (bool query in new[] { false, true })
foreach (bool foreign in new[] { false, true })
    cases.Add(($"{(query ? "query" : "send")}-{(foreign ? "foreign" : "matching")}", query, foreign));
var selected = cases.Where(x => args[0] == "all" || x.Name == args[0]).ToArray();
if (selected.Length == 0)
    throw new ArgumentException("Unknown case. Allowed: " + string.Join(", ", cases.Select(x => x.Name)));
var results = new List<object>();
int failures = 0;
foreach (var item in selected)
{
    // Each case owns a finite caller token. The CLI deadline is never a PASS oracle.
    using var caller = new CancellationTokenSource(callerDeadline);
    var fixture = new EntityFrameworkSagaTransactionOwnershipRegressionTests(operationTimeout, caller.Token);
    try
    {
        await fixture.Gate_WithAnActualCurrentTransactionAsync(item.Query, item.Foreign);
        results.Add(new { @case = item.Name, assertions_passed = true });
    }
    catch (Exception exception)
    {
        failures++;
        results.Add(new { @case = item.Name, assertions_passed = false,
            exception_type = exception.GetType().FullName, exception.Message, exception.StackTrace,
            bounded_guard_failure = exception is TimeoutException or OperationCanceledException });
    }
}
Console.WriteLine(JsonSerializer.Serialize(new { cases = results, count = selected.Length, failures }));
return failures == 0 ? 0 : 1;

static TimeSpan ParseDeadline(string text)
{
    TimeSpan value = TimeSpan.Parse(text, CultureInfo.InvariantCulture);
    if (value <= TimeSpan.Zero || value.TotalMilliseconds > uint.MaxValue - 1)
        throw new ArgumentOutOfRangeException(nameof(text), "Deadline must be positive and supported by the .NET timer.");
    return value;
}
