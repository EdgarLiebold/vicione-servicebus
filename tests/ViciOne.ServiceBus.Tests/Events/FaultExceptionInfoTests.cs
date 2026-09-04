using System.Collections;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Events;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Events;

public sealed class FaultExceptionInfoTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-FAULT-DIAGNOSTICS", "application-data-transport")]
    public async Task ApplicationDiagnosticData_IsCarriedByExactlyOnePublishedFaultAsync()
    {
        Fault<DiagnosticFailure> fault = await PublishFaultAsync(FailureSource.ApplicationWrapper);
        ExceptionInfo exception = Assert.Single(fault.Exceptions);

        Assert.Equal(TypeCache<DiagnosticFailureException>.ShortName, exception.ExceptionType);
        Assert.Equal("Frank", Assert.IsType<string>(exception.Data!["USERNAME"]));
        Assert.Equal(27L, Assert.IsType<long>(exception.Data["customerid"]));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FAULT-DIAGNOSTICS", "exception-data-transport")]
    public async Task ExceptionDiagnosticData_IsCarriedByExactlyOnePublishedFaultAsync()
    {
        Fault<DiagnosticFailure> fault = await PublishFaultAsync(FailureSource.ExceptionData);
        ExceptionInfo exception = Assert.Single(fault.Exceptions);

        Assert.Equal(TypeCache<DiagnosticFailureException>.ShortName, exception.ExceptionType);
        Assert.Equal("Frank", Assert.IsType<string>(exception.Data!["USERNAME"]));
        Assert.Equal(27L, Assert.IsType<long>(exception.Data["customerid"]));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FAULT-DIAGNOSTICS", "detached-snapshot")]
    public void Construction_DetachesDiagnosticDataFromTheSourceException()
    {
        var source = new InvalidOperationException("source");
        source.Data["State"] = "before";

        var snapshot = new FaultExceptionInfo(source);
        source.Data["State"] = "after";
        source.Data["AddedLater"] = true;

        Assert.Equal("before", snapshot.Data!["state"]);
        Assert.False(snapshot.Data.ContainsKey("AddedLater"));
        Assert.NotSame(source.Data, snapshot.Data);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FAULT-DIAGNOSTICS", "wrapper-precedence")]
    public void ApplicationDiagnosticData_WinsOverWrappedExceptionDataCaseInsensitively()
    {
        var source = new InvalidOperationException("source");
        source.Data["TraceId"] = "inner";
        var wrapper = new ViciOneServiceBusApplicationException(
            source,
            new[] { new KeyValuePair<string, object>("traceid", "application") });

        var snapshot = new FaultExceptionInfo(wrapper);

        Assert.Equal(TypeCache<InvalidOperationException>.ShortName, snapshot.ExceptionType);
        Assert.Equal("application", snapshot.Data!["TRACEID"]);
        Assert.Single(snapshot.Data);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FAULT-DIAGNOSTICS", "serializable-entry-boundary")]
    public void Construction_IgnoresNonStringKeysAndNullValues()
    {
        var source = new InvalidOperationException("source");
        source.Data[42] = "not a wire key";
        source.Data["NullValue"] = null;
        source.Data["Kept"] = 27;

        var snapshot = new FaultExceptionInfo(source);

        KeyValuePair<string, object> entry = Assert.Single(snapshot.Data!);
        Assert.Equal("Kept", entry.Key);
        Assert.Equal(27, entry.Value);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FAULT-DIAGNOSTICS", "remote-type-and-inner-chain")]
    public void RemoteExceptionInfo_PreservesReportedTypesAndTheInnerChain()
    {
        var remote = new StubExceptionInfo(
            "Remote.OuterException",
            "outer",
            new StubExceptionInfo("Remote.InnerException", "inner", null));

        var snapshot = new FaultExceptionInfo(new ExceptionInfoException(remote));

        Assert.Equal("Remote.OuterException", snapshot.ExceptionType);
        Assert.Equal("outer", snapshot.Message);
        Assert.NotNull(snapshot.InnerException);
        Assert.Equal("Remote.InnerException", snapshot.InnerException.ExceptionType);
        Assert.Equal("inner", snapshot.InnerException.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FAULT-DIAGNOSTICS", "aggregate-exception-count-boundary")]
    public void AggregateFaults_AreLimitedToTheFirstSixteenExceptions()
    {
        var exceptions = Enumerable.Range(0, 17)
            .Select(index => new InvalidOperationException($"failure-{index}"))
            .ToArray();

        var fault = new FaultEvent<DiagnosticFailure>(
            new DiagnosticFailure(FailureSource.ExceptionData),
            null,
            null!,
            new AggregateException(exceptions),
            []);

        Assert.Equal(16, fault.Exceptions.Length);
        Assert.Collection(
            fault.Exceptions,
            Enumerable.Range(0, 16)
                .Select<int, Action<ExceptionInfo>>(index => exception => Assert.Equal($"failure-{index}", exception.Message))
                .ToArray());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FAULT-DIAGNOSTICS", "explicit-exception-count-boundary")]
    public void ExplicitFaultExceptionCollections_AreLimitedToTheFirstSixteenExceptions()
    {
        ExceptionInfo[] exceptions = Enumerable.Range(0, 17)
            .Select(index => (ExceptionInfo)new StubExceptionInfo($"Remote.Type{index}", $"failure-{index}", null))
            .ToArray();

        var fault = new FaultEvent<DiagnosticFailure>(
            new DiagnosticFailure(FailureSource.ExceptionData),
            null,
            null!,
            exceptions,
            []);

        Assert.Equal(16, fault.Exceptions.Length);
        Assert.Same(exceptions[0], fault.Exceptions[0]);
        Assert.Same(exceptions[15], fault.Exceptions[15]);
        Assert.DoesNotContain(exceptions[16], fault.Exceptions);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FAULT-DIAGNOSTICS", "explicit-exception-collection-null-guard")]
    public void ExplicitFaultExceptionCollections_RejectNull()
    {
        Assert.Throws<ArgumentNullException>(() => new FaultEvent<DiagnosticFailure>(
            new DiagnosticFailure(FailureSource.ExceptionData),
            null,
            null!,
            (IEnumerable<ExceptionInfo>)null!,
            []));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FAULT-DIAGNOSTICS", "receive-exception-count-boundary")]
    public void ReceiveFaults_AreLimitedToTheFirstSixteenExceptions()
    {
        var exceptions = Enumerable.Range(0, 17)
            .Select(index => new InvalidOperationException($"failure-{index}"))
            .ToArray();

        var fault = new ReceiveFaultEvent(null!, new AggregateException(exceptions), "application/json", null, []);

        Assert.Equal(16, fault.Exceptions.Length);
        Assert.Equal("failure-0", fault.Exceptions[0].Message);
        Assert.Equal("failure-15", fault.Exceptions[15].Message);
        Assert.DoesNotContain(fault.Exceptions, exception => exception.Message == "failure-16");
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FAULT-DIAGNOSTICS", "inner-exception-depth-boundary")]
    public void Construction_LimitsInnerExceptionChainToSixteenNodes()
    {
        Exception source = new InvalidOperationException("failure-16");
        for (var index = 15; index >= 0; index--)
            source = new InvalidOperationException($"failure-{index}", source);

        var snapshot = new FaultExceptionInfo(source);
        var captured = new List<ExceptionInfo>();
        for (ExceptionInfo? current = snapshot; current is not null; current = current.InnerException)
            captured.Add(current);

        Assert.Equal(16, captured.Count);
        Assert.Equal("failure-0", captured[0].Message);
        Assert.Equal("failure-15", captured[15].Message);
        Assert.Null(captured[15].InnerException);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FAULT-DIAGNOSTICS", "cardinality-key-and-text-boundaries")]
    public void Construction_BoundsDataCardinalityKeysValuesAndDiagnosticText()
    {
        var source = new InvalidOperationException("source");
        for (var index = 0; index < 31; index++)
            source.Data[$"Key-{index:D2}"] = index;

        string oversizedKey = new('k', 257);
        source.Data[oversizedKey] = new string('v', 2049);
        source.Data["Rejected-33"] = true;

        string oversizedText = new('x', 2049);
        var remote = new StubExceptionInfo(oversizedText, oversizedText, null, oversizedText, oversizedText);

        var dataSnapshot = new FaultExceptionInfo(source);
        var textSnapshot = new FaultExceptionInfo(new ExceptionInfoException(remote));

        Assert.Equal(32, dataSnapshot.Data!.Count);
        for (var index = 0; index < 31; index++)
            Assert.Equal(index, Assert.IsType<int>(dataSnapshot.Data[$"KEY-{index:D2}"]));
        Assert.False(dataSnapshot.Data.ContainsKey("Rejected-33"));
        string boundedKey = Assert.Single(dataSnapshot.Data.Keys, key => key.StartsWith('k'));
        Assert.Equal(256, boundedKey.Length);
        Assert.Equal(new string('v', 2048), Assert.IsType<string>(dataSnapshot.Data[boundedKey]));
        Assert.Equal(2048, textSnapshot.ExceptionType.Length);
        Assert.Equal(2048, textSnapshot.Message.Length);
        Assert.Equal(2048, textSnapshot.StackTrace.Length);
        Assert.Equal(2048, textSnapshot.Source.Length);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FAULT-DIAGNOSTICS", "complex-value-safe-rendering")]
    public void Construction_DoesNotInvokeArbitraryDataValueToString()
    {
        var source = new InvalidOperationException("source");
        var value = new HostileDiagnosticValue();
        source.Data["Value"] = value;

        var snapshot = new FaultExceptionInfo(source);

        Assert.Equal(0, value.ToStringCallCount);
        Assert.Equal(typeof(HostileDiagnosticValue).FullName, Assert.IsType<string>(snapshot.Data!["Value"]));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FAULT-DIAGNOSTICS", "hostile-getter-isolation")]
    public void HostileDiagnosticGetters_DoNotReplaceTheOriginalFailure()
    {
        var snapshot = new FaultExceptionInfo(new HostileDiagnosticException());

        Assert.Equal(TypeCache<HostileDiagnosticException>.ShortName, snapshot.ExceptionType);
        Assert.Contains("Message property threw", snapshot.Message, StringComparison.Ordinal);
        Assert.Equal(string.Empty, snapshot.StackTrace);
        Assert.Equal(string.Empty, snapshot.Source);
        Assert.Null(snapshot.Data);
    }

    private static async Task<Fault<DiagnosticFailure>> PublishFaultAsync(FailureSource source)
    {
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddConsumer<DiagnosticFailureConsumer>();
            })
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
        ITestHarness harness = await provider.StartTestHarnessAsync().WaitAsync(timeout, cancellationToken);

        try
        {
            Task<IPublishedMessage<Fault<DiagnosticFailure>>> publishedFaultTask = harness.Published
                .SelectAsync<Fault<DiagnosticFailure>>(cancellationToken)
                .FirstObservedAsync();
            Task<ConsumeContext<Fault<DiagnosticFailure>>> receivedFaultTask =
                await harness.ConnectPublishHandlerAsync<Fault<DiagnosticFailure>>(_ => true);

            await harness.Bus.PublishAsync(new DiagnosticFailure(source), cancellationToken);
            await publishedFaultTask.WaitAsync(timeout, cancellationToken);
            Fault<DiagnosticFailure> fault =
                (await receivedFaultTask.WaitAsync(timeout, cancellationToken)).Message;

            Assert.Single(harness.Published.Select<Fault<DiagnosticFailure>>(new CancellationToken(canceled: true)));
            return fault;
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    private sealed record DiagnosticFailure(FailureSource Source);

    private enum FailureSource
    {
        ApplicationWrapper,
        ExceptionData,
    }

    private sealed class DiagnosticFailureConsumer : IConsumer<DiagnosticFailure>
    {
        public Task ConsumeAsync(ConsumeContext<DiagnosticFailure> context)
        {
            var failure = new DiagnosticFailureException("intentional diagnostic failure");

            if (context.Message.Source == FailureSource.ApplicationWrapper)
            {
                throw new ViciOneServiceBusApplicationException(failure, new
                {
                    Username = "Frank",
                    CustomerId = 27,
                });
            }

            failure.Data["Username"] = "Frank";
            failure.Data["CustomerId"] = 27;
            throw failure;
        }
    }

    private sealed class DiagnosticFailureException(string message) : Exception(message);

    private sealed class StubExceptionInfo(
        string exceptionType,
        string message,
        ExceptionInfo? innerException,
        string stackTrace = "remote stack",
        string source = "remote source") : ExceptionInfo
    {
        public string ExceptionType { get; } = exceptionType;
        public ExceptionInfo? InnerException { get; } = innerException;
        public string StackTrace { get; } = stackTrace;
        public string Message { get; } = message;
        public string Source { get; } = source;
        public IDictionary<string, object>? Data { get; } = null;
    }

    private sealed class HostileDiagnosticValue
    {
        public int ToStringCallCount { get; private set; }

        public override string ToString()
        {
            ToStringCallCount++;
            throw new InvalidOperationException("Application ToString must not be called for fault diagnostics.");
        }
    }

    private sealed class HostileDiagnosticException : Exception
    {
        public override IDictionary Data => throw new InvalidOperationException("Data getter fault");

        public override string Message => throw new InvalidOperationException("Message getter fault");

        public override string? Source
        {
            get => throw new InvalidOperationException("Source getter fault");
            set => throw new InvalidOperationException("Source setter fault");
        }

        public override string? StackTrace => throw new InvalidOperationException("StackTrace getter fault");
    }
}
