using System.Reflection;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.SqlTransport.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.Tests.Scheduling;

public sealed class SqlScheduleCancellationProviderTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-SCHEDULE-CANCELLATION", "consume-context-uses-active-client-and-exact-token")]
    public async Task ConsumeCancellation_UsesActiveClientAndExactTokenAsync()
    {
        ClientContext client = DispatchProxy.Create<ClientContext, ClientProxy>();
        ConsumeContext consume = DispatchProxy.Create<ConsumeContext, ConsumeProxy>();
        ((ConsumeProxy)(object)consume).Client = client;
        var provider = new SqlScheduleMessageProvider(consume);
        Guid tokenId = Guid.NewGuid();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await provider.CancelScheduledSendAsync(tokenId, cancellationToken);

        var clientProxy = (ClientProxy)(object)client;
        Assert.Equal(1, ((ConsumeProxy)(object)consume).LookupCount);
        Assert.Equal(1, clientProxy.DeleteCount);
        Assert.Equal(tokenId, clientProxy.DeletedToken);
        Assert.Equal(cancellationToken, clientProxy.DeletionToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-SCHEDULE-CANCELLATION", "cancelled-consume-request-never-looks-up-client")]
    public async Task CancelledConsumeRequest_DoesNotTouchActiveClientAsync()
    {
        ClientContext client = DispatchProxy.Create<ClientContext, ClientProxy>();
        ConsumeContext consume = DispatchProxy.Create<ConsumeContext, ConsumeProxy>();
        ((ConsumeProxy)(object)consume).Client = client;
        var provider = new SqlScheduleMessageProvider(consume);
        using var source = new CancellationTokenSource();
        source.Cancel();

        OperationCanceledException error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            provider.CancelScheduledSendAsync(Guid.NewGuid(), source.Token));

        Assert.Equal(source.Token, error.CancellationToken);
        Assert.Equal(0, ((ConsumeProxy)(object)consume).LookupCount);
        Assert.Equal(0, ((ClientProxy)(object)client).DeleteCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-SCHEDULE-CANCELLATION", "host-cancel-passes-client-and-request-token")]
    public async Task HostCancellation_SendsThroughConnectionAndPreservesRequestTokenAsync()
    {
        var fixture = new HostFixture();
        var provider = new SqlScheduleMessageProvider(fixture.Host, fixture.Endpoints);
        Guid tokenId = Guid.NewGuid();
        var destination = new Uri("db://localhost/transport/scheduled");
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await provider.CancelScheduledSendAsync(destination, tokenId, cancellationToken);

        Assert.Equal(1, fixture.SupervisorProxy.SendCount);
        Assert.Equal(cancellationToken, fixture.SupervisorProxy.SendToken);
        Assert.Equal(1, fixture.ConnectionProxy.ClientCount);
        Assert.Equal(cancellationToken, fixture.ConnectionProxy.ClientToken);
        Assert.Equal(1, fixture.ClientProxy.DeleteCount);
        Assert.Equal(tokenId, fixture.ClientProxy.DeletedToken);
        Assert.Equal(cancellationToken, fixture.ClientProxy.DeletionToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-SCHEDULE-CANCELLATION", "host-delete-fault-propagates-without-resend")]
    public async Task HostDeleteFailure_PropagatesWithoutAnotherDispatchAsync()
    {
        var fixture = new HostFixture();
        var failure = new InvalidOperationException("database refused deletion");
        fixture.ClientProxy.DeleteFailures.Enqueue(failure);
        var provider = new SqlScheduleMessageProvider(fixture.Host, fixture.Endpoints);
        Guid tokenId = Guid.NewGuid();

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.CancelScheduledSendAsync(tokenId, TestContext.Current.CancellationToken));

        Assert.Same(failure, actual);
        Assert.Equal(1, fixture.SupervisorProxy.SendCount);
        Assert.Equal(1, fixture.ClientProxy.DeleteCount);
        Assert.Equal(tokenId, fixture.ClientProxy.DeletedToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-SCHEDULE-CANCELLATION", "host-retry-reopens-client-with-exact-token")]
    public async Task HostRetry_ReopensClientAndPreservesCancellationIdentityAsync()
    {
        var fixture = new HostFixture();
        fixture.HostProxy.RetryPolicy = Retry.Immediate(1);
        fixture.ClientProxy.DeleteFailures.Enqueue(new InvalidOperationException("transient deletion fault"));
        var provider = new SqlScheduleMessageProvider(fixture.Host, fixture.Endpoints);
        Guid tokenId = Guid.NewGuid();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await provider.CancelScheduledSendAsync(tokenId, cancellationToken);

        Assert.Equal(2, fixture.SupervisorProxy.SendCount);
        Assert.Equal(2, fixture.ConnectionProxy.ClientCount);
        Assert.Equal(2, fixture.ClientProxy.DeleteCount);
        Assert.All(fixture.ClientProxy.Deletions, attempt =>
        {
            Assert.Equal(tokenId, attempt.TokenId);
            Assert.Equal(cancellationToken, attempt.CancellationToken);
        });
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-SCHEDULE-CANCELLATION", "missing-schedule-is-a-no-op-without-retry")]
    public async Task MissingSchedule_CompletesWithoutRetryOrSecondDeletionAsync()
    {
        var fixture = new HostFixture();
        fixture.HostProxy.RetryPolicy = Retry.Immediate(1);
        fixture.ClientProxy.DeleteResult = false;
        var provider = new SqlScheduleMessageProvider(fixture.Host, fixture.Endpoints);
        Guid tokenId = Guid.NewGuid();

        await provider.CancelScheduledSendAsync(tokenId, TestContext.Current.CancellationToken);

        Assert.Equal(1, fixture.SupervisorProxy.SendCount);
        Assert.Equal(1, fixture.ClientProxy.DeleteCount);
        Assert.Equal(tokenId, fixture.ClientProxy.DeletedToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-SCHEDULE-CANCELLATION", "cancelled-host-request-never-enters-supervisor")]
    public async Task CancelledHostRequest_DoesNotEnterConnectionSupervisorAsync()
    {
        var fixture = new HostFixture();
        var provider = new SqlScheduleMessageProvider(fixture.Host, fixture.Endpoints);
        using var caller = new CancellationTokenSource();
        caller.Cancel();

        OperationCanceledException error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            provider.CancelScheduledSendAsync(Guid.NewGuid(), caller.Token));

        Assert.Equal(caller.Token, error.CancellationToken);
        Assert.Equal(0, fixture.SupervisorProxy.SendCount);
        Assert.Equal(0, fixture.ConnectionProxy.ClientCount);
        Assert.Equal(0, fixture.ClientProxy.DeleteCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-SCHEDULE-CANCELLATION", "stopping-host-never-enters-supervisor")]
    public async Task StoppingHost_RejectsDeletionBeforeConnectionDispatchAsync()
    {
        var fixture = new HostFixture();
        var provider = new SqlScheduleMessageProvider(fixture.Host, fixture.Endpoints);
        using var stopping = new CancellationTokenSource();
        fixture.SupervisorProxy.StoppingToken = stopping.Token;
        stopping.Cancel();

        ConnectionException error = await Assert.ThrowsAsync<ConnectionException>(() =>
            provider.CancelScheduledSendAsync(Guid.NewGuid(), TestContext.Current.CancellationToken));

        Assert.Contains("stopping", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, fixture.SupervisorProxy.SendCount);
        Assert.Equal(0, fixture.ClientProxy.DeleteCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-SCHEDULE-CANCELLATION", "caller-cancels-after-supervisor-entry-before-client-creation")]
    public async Task CancellationAfterSupervisorEntry_StopsBeforeClientCreationAsync()
    {
        var fixture = new HostFixture();
        using var caller = new CancellationTokenSource();
        fixture.SupervisorProxy.BeforePipe = caller.Cancel;
        var provider = new SqlScheduleMessageProvider(fixture.Host, fixture.Endpoints);

        OperationCanceledException error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            provider.CancelScheduledSendAsync(Guid.NewGuid(), caller.Token));

        Assert.Equal(caller.Token, error.CancellationToken);
        Assert.Equal(1, fixture.SupervisorProxy.SendCount);
        Assert.Equal(0, fixture.ConnectionProxy.ClientCount);
        Assert.Equal(0, fixture.ClientProxy.DeleteCount);
    }

    private sealed class HostFixture
    {
        public HostFixture()
        {
            Client = DispatchProxy.Create<ClientContext, ClientProxy>();
            Connection = DispatchProxy.Create<ConnectionContext, ConnectionProxy>();
            Supervisor = DispatchProxy.Create<IConnectionContextSupervisor, SupervisorProxy>();
            Host = DispatchProxy.Create<ISqlHostConfiguration, HostProxy>();
            Endpoints = DispatchProxy.Create<ISendEndpointProvider, UnsupportedProxy>();
            ConnectionProxy.Client = Client;
            SupervisorProxy.Connection = Connection;
            ((HostProxy)(object)Host).Supervisor = Supervisor;
        }

        public ClientContext Client { get; }
        public ClientProxy ClientProxy => (ClientProxy)(object)Client;
        public ConnectionContext Connection { get; }
        public ConnectionProxy ConnectionProxy => (ConnectionProxy)(object)Connection;
        public ISendEndpointProvider Endpoints { get; }
        public ISqlHostConfiguration Host { get; }
        public HostProxy HostProxy => (HostProxy)(object)Host;
        public IConnectionContextSupervisor Supervisor { get; }
        public SupervisorProxy SupervisorProxy => (SupervisorProxy)(object)Supervisor;
    }

    private class ConsumeProxy : DispatchProxy
    {
        public ClientContext Client { get; set; } = null!;
        public int LookupCount { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == "TryGetPayload" && targetMethod.IsGenericMethod &&
                targetMethod.GetGenericArguments()[0] == typeof(ClientContext))
            {
                LookupCount++;
                args![0] = Client;
                return true;
            }
            throw new NotSupportedException(targetMethod?.Name);
        }
    }

    private class ClientProxy : DispatchProxy
    {
        public int DeleteCount { get; private set; }
        public Queue<Exception> DeleteFailures { get; } = new();
        public bool DeleteResult { get; set; } = true;
        public Guid DeletedToken { get; private set; }
        public CancellationToken DeletionToken { get; private set; }
        public List<(Guid TokenId, CancellationToken CancellationToken)> Deletions { get; } = new();

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == "DeleteScheduledMessageAsync")
            {
                DeleteCount++;
                DeletedToken = (Guid)args![0]!;
                DeletionToken = (CancellationToken)args[1]!;
                Deletions.Add((DeletedToken, DeletionToken));
                return DeleteFailures.Count == 0
                    ? Task.FromResult(DeleteResult)
                    : Task.FromException<bool>(DeleteFailures.Dequeue());
            }
            throw new NotSupportedException(targetMethod?.Name);
        }
    }

    private class ConnectionProxy : DispatchProxy
    {
        public ClientContext Client { get; set; } = null!;
        public int ClientCount { get; private set; }
        public CancellationToken ClientToken { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == "CreateClientContext")
            {
                ClientCount++;
                ClientToken = (CancellationToken)args![0]!;
                return Client;
            }
            throw new NotSupportedException(targetMethod?.Name);
        }
    }

    private class SupervisorProxy : DispatchProxy
    {
        public ConnectionContext Connection { get; set; } = null!;
        public Action? BeforePipe { get; set; }
        public int SendCount { get; private set; }
        public CancellationToken SendToken { get; private set; }
        public CancellationToken StoppingToken { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == "get_Stopping")
                return StoppingToken;
            if (targetMethod?.Name == "SendAsync" && args?[0] is IPipe<ConnectionContext> pipe)
            {
                SendCount++;
                SendToken = (CancellationToken)args[1]!;
                BeforePipe?.Invoke();
                return pipe.SendAsync(Connection);
            }
            throw new NotSupportedException(targetMethod?.Name);
        }
    }

    private class HostProxy : DispatchProxy
    {
        public IConnectionContextSupervisor Supervisor { get; set; } = null!;
        public IRetryPolicy RetryPolicy { get; set; } = Retry.None;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            "get_ConnectionContextSupervisor" => Supervisor,
            "get_HostAddress" => new Uri("db://localhost/transport"),
            "get_SendTransportRetryPolicy" => RetryPolicy,
            _ => throw new NotSupportedException(targetMethod?.Name),
        };
    }

    private class UnsupportedProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException(targetMethod?.Name);
    }
}
