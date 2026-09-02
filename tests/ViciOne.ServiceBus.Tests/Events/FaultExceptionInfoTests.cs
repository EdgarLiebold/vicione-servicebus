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
    public async Task ApplicationDiagnosticData_IsCarriedByExactlyOnePublishedFault()
    {
        Fault<DiagnosticFailure> fault = await PublishFault(FailureSource.ApplicationWrapper);
        ExceptionInfo exception = Assert.Single(fault.Exceptions);

        Assert.Equal(TypeCache<DiagnosticFailureException>.ShortName, exception.ExceptionType);
        Assert.Equal("Frank", Assert.IsType<string>(exception.Data!["USERNAME"]));
        Assert.Equal(27L, Assert.IsType<long>(exception.Data["customerid"]));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FAULT-DIAGNOSTICS", "exception-data-transport")]
    public async Task ExceptionDiagnosticData_IsCarriedByExactlyOnePublishedFault()
    {
        Fault<DiagnosticFailure> fault = await PublishFault(FailureSource.ExceptionData);
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

    private static async Task<Fault<DiagnosticFailure>> PublishFault(FailureSource source)
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
        ITestHarness harness = await provider.StartTestHarness().WaitAsync(timeout, cancellationToken);

        try
        {
            Task<IPublishedMessage<Fault<DiagnosticFailure>>> publishedFaultTask = harness.Published
                .SelectAsync<Fault<DiagnosticFailure>>(cancellationToken)
                .First();
            Task<ConsumeContext<Fault<DiagnosticFailure>>> receivedFaultTask =
                await harness.ConnectPublishHandler<Fault<DiagnosticFailure>>(_ => true);

            await harness.Bus.Publish(new DiagnosticFailure(source), cancellationToken);
            await publishedFaultTask.WaitAsync(timeout, cancellationToken);
            Fault<DiagnosticFailure> fault =
                (await receivedFaultTask.WaitAsync(timeout, cancellationToken)).Message;

            Assert.Single(harness.Published.Select<Fault<DiagnosticFailure>>(new CancellationToken(canceled: true)));
            return fault;
        }
        finally
        {
            await harness.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
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
        public Task Consume(ConsumeContext<DiagnosticFailure> context)
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
        ExceptionInfo? innerException) : ExceptionInfo
    {
        public string ExceptionType { get; } = exceptionType;
        public ExceptionInfo? InnerException { get; } = innerException;
        public string StackTrace { get; } = "remote stack";
        public string Message { get; } = message;
        public string Source { get; } = "remote source";
        public IDictionary<string, object>? Data { get; } = null;
    }
}
