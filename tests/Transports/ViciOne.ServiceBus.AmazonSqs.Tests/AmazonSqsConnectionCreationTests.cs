using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.AmazonSqs.Configuration;
using ViciOne.ServiceBus.AmazonSqs.Tests.TestDoubles;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqs.Tests;

public sealed class AmazonSqsConnectionCreationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-LIFECYCLE", "connection-factory-handle-owns-provider-connection")]
    public async Task CreateContext_UsesConfiguredConnectionAndOwnsItsLifetime()
    {
        var creations = 0;
        var disposals = 0;
        var registrations = 0;
        IConnection connection = CreateConnection(() => disposals++);
        var factory = CreateFactory(() =>
        {
            creations++;
            return connection;
        });
        ISupervisor supervisor = CreateSupervisor(() => CancellationToken.None, agent =>
        {
            registrations++;
            Assert.IsType<PipeContextAgent<ConnectionContext>>(agent);
        });

        IPipeContextAgent<ConnectionContext> handle = factory.CreateContext(supervisor);
        var context = Assert.IsType<AmazonSqsConnectionContext>(await handle.Context.WaitAsync(
            TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));

        Assert.Same(connection, context.Connection);
        Assert.Equal(1, creations);
        Assert.Equal(1, registrations);
        Assert.Equal(0, disposals);
        await handle.StopAsync("test complete", TestContext.Current.CancellationToken);
        await handle.Completed;
        Assert.Equal(1, disposals);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-LIFECYCLE", "connection-factory-preserves-provider-failure-cause")]
    public async Task CreateContext_PreservesProviderFailureAsConnectionFailure()
    {
        var cause = new InvalidOperationException("provider unavailable");
        var creations = 0;
        var factory = CreateFactory(() =>
        {
            creations++;
            throw cause;
        });

        IPipeContextAgent<ConnectionContext> handle = factory.CreateContext(CreateSupervisor(CancellationToken.None));
        var failure = await Assert.ThrowsAsync<AmazonSqsConnectionException>(() => handle.Context.WaitAsync(
            TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));

        Assert.Same(cause, failure.InnerException);
        Assert.Equal(1, creations);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-CANCELLATION", "connection-factory-stopping-guard-prevents-provider-open")]
    public async Task CreateContext_WhenStopping_DoesNotOpenAConnection()
    {
        using var stopping = new CancellationTokenSource();
        stopping.Cancel();
        var stoppingReads = 0;
        var creations = 0;
        var factory = CreateFactory(() =>
        {
            creations++;
            throw new InvalidOperationException("Connection factory must not run after stopping.");
        });

        IPipeContextAgent<ConnectionContext> handle = factory.CreateContext(CreateSupervisor(() =>
            Interlocked.Increment(ref stoppingReads) == 1 ? CancellationToken.None : stopping.Token));
        var failure = await Assert.ThrowsAsync<AmazonSqsConnectionException>(() => handle.Context.WaitAsync(
            TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));

        Assert.Contains("stopping", failure.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(stoppingReads >= 2);
        Assert.Equal(0, creations);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-CANCELLATION", "connection-factory-propagates-provider-cancellation")]
    public async Task CreateContext_PropagatesProviderCancellationWithoutWrapping()
    {
        var canceled = new OperationCanceledException("provider canceled");
        var factory = CreateFactory(() => throw canceled);

        IPipeContextAgent<ConnectionContext> handle = factory.CreateContext(CreateSupervisor(CancellationToken.None));
        var observed = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => handle.Context.WaitAsync(
            TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));

        Assert.Same(canceled, observed);
    }

    private static ConnectionContextFactory CreateFactory(Func<IConnection> connect)
    {
        var settings = new AmazonSqsHostSettings(
            RegionEndpoint.EUCentral1, null, false, new Uri("amazonsqs://eu-central-1/"),
            new AmazonSqsClientContextCacheOptions(), connect, null);
        IAmazonSqsHostConfiguration host = InterfaceProxy<IAmazonSqsHostConfiguration>.Create((method, _) => method.Name switch
        {
            "get_Settings" => settings,
            "get_HostAddress" => settings.HostAddress,
            "get_ReceiveTransportRetryPolicy" => Retry.None,
            "get_Topology" => InterfaceProxy<IAmazonSqsBusTopology>.Create((_, _) => throw new NotSupportedException()),
            _ => throw new NotSupportedException(method.Name)
        });
        return new ConnectionContextFactory(host);
    }

    private static ISupervisor CreateSupervisor(CancellationToken stopping) => CreateSupervisor(() => stopping);

    private static ISupervisor CreateSupervisor(Func<CancellationToken> stopping, Action<IAgent>? register = null) =>
        InterfaceProxy<ISupervisor>.Create((method, args) => method.Name switch
        {
            "get_Stopping" => stopping(),
            "get_Stopped" => CancellationToken.None,
            nameof(ISupervisor.Add) => RegisterAgent(register, args),
            _ => throw new NotSupportedException(method.Name)
        });

    private static object? RegisterAgent(Action<IAgent>? register, object?[]? args)
    {
        register?.Invoke(Assert.IsAssignableFrom<IAgent>(Assert.Single(args!)));
        return null;
    }

    private static IConnection CreateConnection(Action dispose)
    {
        IAmazonSQS sqs = InterfaceProxy<IAmazonSQS>.Create((method, _) => throw new NotSupportedException(method.Name));
        IAmazonSimpleNotificationService sns = InterfaceProxy<IAmazonSimpleNotificationService>.Create((method, _) =>
            throw new NotSupportedException(method.Name));
        return InterfaceProxy<IConnection>.Create((method, _) => method.Name switch
        {
            "get_SqsClient" => sqs,
            "get_SnsClient" => sns,
            nameof(IDisposable.Dispose) => RecordDisposal(),
            _ => throw new NotSupportedException(method.Name)
        });

        object? RecordDisposal()
        {
            dispose();
            return null;
        }
    }
}
