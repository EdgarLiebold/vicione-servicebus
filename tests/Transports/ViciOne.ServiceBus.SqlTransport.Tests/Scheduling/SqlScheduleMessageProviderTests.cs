using Microsoft.Extensions.Logging;
using System.Reflection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.SqlTransport.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.Tests.Scheduling;

public sealed class SqlScheduleMessageProviderTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-SCHEDULE-TOKEN", "cancellation-capability-uses-caller-token")]
    public void CancellationCapability_UsesCallerSpecifiedToken()
    {
        ISendEndpointProvider endpoints = DispatchProxy.Create<ISendEndpointProvider, UnsupportedProxy>();
        ISqlHostConfiguration hostConfiguration = DispatchProxy.Create<ISqlHostConfiguration, UnsupportedProxy>();

        Assert.Equal(ScheduleCancellationMode.CallerSpecifiedToken,
            new SqlScheduleMessageProvider(hostConfiguration, endpoints).CancellationMode);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-SCHEDULE-TOKEN", "generated-token-is-persisted-and-returned")]
    public async Task ScheduleSendAsync_PersistsAndReturnsTheSameGeneratedTokenAsync()
    {
        ISendEndpoint endpoint = DispatchProxy.Create<AdvancedScheduleEndpoint, ScheduleEndpointProxy>();
        ISendEndpointProvider endpoints = DispatchProxy.Create<ISendEndpointProvider, EndpointProviderProxy>();
        ((EndpointProviderProxy)(object)endpoints).Endpoint = endpoint;
        ISqlHostConfiguration hostConfiguration = DispatchProxy.Create<ISqlHostConfiguration, UnsupportedProxy>();
        var provider = new SqlScheduleMessageProvider(hostConfiguration, endpoints);

        ScheduledMessage<Probe> scheduled = await provider.ScheduleSendAsync(
            new Uri("db://localhost/transport/scheduled"),
            DateTimeOffset.UtcNow.AddMinutes(5),
            new Probe(),
            Pipe.Empty<SendContext<Probe>>(),
            TestContext.Current.CancellationToken);

        SendContext context = Assert.IsType<MessageSendContext<Probe>>(
            ((ScheduleEndpointProxy)(object)endpoint).Context);
        Assert.NotEqual(Guid.Empty, scheduled.TokenId);
        Assert.Equal(scheduled.TokenId, context.ScheduledMessageId);
        Assert.True(context.Headers.TryGetHeader(MessageHeaders.SchedulingTokenId, out object? header));
        Assert.Equal(scheduled.TokenId.ToString("D"), header);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-SCHEDULE-TOKEN", "empty-selector-token-rejected-before-sql-dispatch")]
    public async Task EmptySelectedToken_CannotResolveSqlEndpoint_AndAValidTokenRecoversAsync()
    {
        ScheduleTokenId.UseTokenId<SelectedTokenProbe>(message => message.TokenId);
        ISendEndpoint endpoint = DispatchProxy.Create<AdvancedScheduleEndpoint, ScheduleEndpointProxy>();
        ISendEndpointProvider endpoints = DispatchProxy.Create<ISendEndpointProvider, EndpointProviderProxy>();
        var endpointProvider = (EndpointProviderProxy)(object)endpoints;
        endpointProvider.Endpoint = endpoint;
        ISqlHostConfiguration hostConfiguration = DispatchProxy.Create<ISqlHostConfiguration, UnsupportedProxy>();
        var provider = new SqlScheduleMessageProvider(hostConfiguration, endpoints);
        Uri destination = new("db://localhost/transport/scheduled");
        DateTimeOffset dueAt = DateTimeOffset.UtcNow.AddMinutes(5);
        Guid validToken = Guid.NewGuid();

        ArgumentException rejected = await Assert.ThrowsAsync<ArgumentException>(() => provider.ScheduleSendAsync(
            destination, dueAt, new SelectedTokenProbe(Guid.Empty),
            Pipe.Empty<SendContext<SelectedTokenProbe>>(), TestContext.Current.CancellationToken));
        Assert.Equal("message", rejected.ParamName);
        Assert.Equal(0, endpointProvider.ResolutionCount);
        Assert.Null(((ScheduleEndpointProxy)(object)endpoint).Context);

        ScheduledMessage<SelectedTokenProbe> accepted = await provider.ScheduleSendAsync(
            destination, dueAt, new SelectedTokenProbe(validToken),
            Pipe.Empty<SendContext<SelectedTokenProbe>>(), TestContext.Current.CancellationToken);

        Assert.Equal(1, endpointProvider.ResolutionCount);
        Assert.Equal(validToken, accepted.TokenId);
        SendContext context = Assert.IsType<MessageSendContext<SelectedTokenProbe>>(
            ((ScheduleEndpointProxy)(object)endpoint).Context);
        Assert.Equal(validToken, context.ScheduledMessageId);
        Assert.True(context.Headers.TryGetHeader(MessageHeaders.SchedulingTokenId, out object? header));
        Assert.Equal(validToken.ToString("D"), header);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-SCHEDULE-ARGUMENTS", "public-scheduling-boundary-rejects-invalid-input")]
    public async Task PublicOperations_RejectInvalidArgumentsBeforeResolvingTransportStateAsync()
    {
        ISendEndpointProvider endpoints = DispatchProxy.Create<ISendEndpointProvider, UnsupportedProxy>();
        ISqlHostConfiguration hostConfiguration = DispatchProxy.Create<ISqlHostConfiguration, UnsupportedProxy>();
        var provider = new SqlScheduleMessageProvider(hostConfiguration, endpoints);
        var destination = new Uri("db://localhost/transport/scheduled");
        var message = new Probe();
        IPipe<SendContext<Probe>> pipe = Pipe.Empty<SendContext<Probe>>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            provider.ScheduleSendAsync(null!, DateTimeOffset.UtcNow, message, pipe, cancellationToken));
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            provider.ScheduleSendAsync(destination, DateTimeOffset.UtcNow, null!, pipe, cancellationToken));
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            provider.ScheduleSendAsync(destination, DateTimeOffset.UtcNow, message, null!, cancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(() => provider.CancelScheduledSendAsync(Guid.Empty, cancellationToken));
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            provider.CancelScheduledSendAsync(null!, Guid.NewGuid(), cancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            provider.CancelScheduledSendAsync(destination, Guid.Empty, cancellationToken));

        ConsumeContext consumeContext = DispatchProxy.Create<ConsumeContext, MissingPayloadProxy>();
        var consumeProvider = new SqlScheduleMessageProvider(consumeContext);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            consumeProvider.CancelScheduledSendAsync(Guid.NewGuid(), cancellationToken));
    }

    private sealed record Probe;

    private sealed record SelectedTokenProbe(Guid TokenId);

    private interface AdvancedScheduleEndpoint :
        ISendEndpoint,
        ViciOne.ServiceBus.Advanced.IAdvancedSendEndpoint;

    private class EndpointProviderProxy : DispatchProxy
    {
        public ISendEndpoint Endpoint { get; set; } = null!;

        public int ResolutionCount { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            if (targetMethod.Name == "GetSendEndpointAsync")
            {
                ResolutionCount++;
                return Task.FromResult(Endpoint);
            }

            throw new NotSupportedException(targetMethod.Name);
        }
    }

    private class ScheduleEndpointProxy : DispatchProxy
    {
        public SendContext? Context { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            if (targetMethod.Name == "SendAsync"
                && args is [Probe message, IPipe<SendContext<Probe>> pipe, CancellationToken _])
            {
                var context = new MessageSendContext<Probe>(message);
                Context = context;
                return pipe.SendAsync(context);
            }

            if (targetMethod.Name == "SendAsync"
                && args is [SelectedTokenProbe messageWithToken, IPipe<SendContext<SelectedTokenProbe>> tokenPipe, CancellationToken _])
            {
                var context = new MessageSendContext<SelectedTokenProbe>(messageWithToken);
                Context = context;
                return tokenPipe.SendAsync(context);
            }

            throw new NotSupportedException(targetMethod.Name);
        }
    }

    private class UnsupportedProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException(targetMethod?.Name);
    }

    private class MissingPayloadProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            if (targetMethod.Name == "TryGetPayload" && targetMethod.IsGenericMethod)
            {
                args![0] = null;
                return false;
            }

            throw new NotSupportedException(targetMethod.Name);
        }
    }
    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    [RequirementCoverage("REQ-VSB-SQL-SCHEDULE-TOKEN", "owning-debug-cannot-replace-accepted-schedule-or-deletion")]
    public async Task AcceptedOperations_AreNotReplacedByTheirDebugLoggerAsync(int route, bool loggerThrows)
    {
        using var caller = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var destination = new Uri("db://localhost/transport/diagnostic-scheduled");
        DateTimeOffset dueAt = DateTimeOffset.UtcNow.AddMinutes(7);
        var message = new Probe();
        Guid tokenId = Guid.NewGuid();
        var loggerFailure = new IOException("SQL accepted scheduling diagnostic failure");
        string template = route switch
        {
            0 => "SCHED {DestinationAddress} {MessageId} {MessageType} {DeliveryTime:G} {Token}",
            1 => "CANCEL {TokenId}",
            _ => "CANCEL {DestinationAddress} {TokenId}"
        };
        var logger = new AcceptedOperationLogger(template, loggerThrows ? loggerFailure : null);
        var previous = LogContext.Current;
        ISendEndpoint endpoint = DispatchProxy.Create<AdvancedScheduleEndpoint, AcceptedSendProxy>();
        var send = (AcceptedSendProxy)(object)endpoint;
        ISendEndpointProvider endpoints = DispatchProxy.Create<ISendEndpointProvider, AcceptedEndpointProviderProxy>();
        var resolver = (AcceptedEndpointProviderProxy)(object)endpoints;
        resolver.Endpoint = endpoint;
        ClientContext client = DispatchProxy.Create<ClientContext, AcceptedDeleteProxy>();
        var deletion = (AcceptedDeleteProxy)(object)client;
        ConsumeContext consume = DispatchProxy.Create<ConsumeContext, AcceptedConsumeProxy>();
        ((AcceptedConsumeProxy)(object)consume).Client = client;
        Task? operation = null;
        Task<ScheduledMessage<Probe>>? scheduledOperation = null;
        try
        {
            LogContext.ConfigureCurrentLogContext(logger);
            if (route == 0)
            {
                ISqlHostConfiguration host = DispatchProxy.Create<ISqlHostConfiguration, UnsupportedProxy>();
                scheduledOperation = new SqlScheduleMessageProvider(host, endpoints).ScheduleSendAsync(
                    destination, dueAt, message, Pipe.Empty<SendContext<Probe>>(), caller.Token);
                operation = scheduledOperation;
                await send.Entered.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
                Assert.Equal(1, resolver.Calls);
                Assert.Same(destination, resolver.Address);
                Assert.Equal(caller.Token, resolver.Token);
                Assert.Equal(1, send.Calls);
                Assert.Same(message, send.Context!.Message);
                Assert.Equal(caller.Token, send.Context.CancellationToken);
                Assert.NotNull(send.Context.ScheduledMessageId);
                Assert.NotEqual(Guid.Empty, send.Context.ScheduledMessageId!.Value);
                Assert.True(send.Context.Headers.TryGetHeader(MessageHeaders.SchedulingTokenId, out object? header));
                Assert.Equal(send.Context.ScheduledMessageId.Value.ToString("D"), header);
                Assert.False(send.RawTask.IsCompleted);
                Assert.False(send.PublicTask!.IsCompleted);
            }
            else
            {
                var provider = new SqlScheduleMessageProvider(consume);
                operation = route == 1
                    ? provider.CancelScheduledSendAsync(tokenId, caller.Token)
                    : provider.CancelScheduledSendAsync(destination, tokenId, caller.Token);
                await deletion.Entered.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
                Assert.Equal(1, deletion.Calls);
                Assert.Equal(tokenId, deletion.TokenId);
                Assert.Equal(caller.Token, deletion.Token);
                Assert.False(deletion.RawTask.IsCompleted);
            }
            Assert.False(operation.IsCompleted);
            send.Release();
            deletion.Release();
            Exception? observed = await Record.ExceptionAsync(() => operation.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None));
            if (route == 0)
            {
                Assert.True(send.RawTask.IsCompletedSuccessfully);
                Assert.True(send.PublicTask!.IsCompletedSuccessfully);
            }
            else
                Assert.True(deletion.RawTask.IsCompletedSuccessfully);
            Dictionary<string, object?> entry = Assert.Single(logger.Entries);
            Assert.Equal(template, entry["{OriginalFormat}"]);
            Assert.Equal(loggerThrows ? 1 : 0, logger.ThrowCount);
            if (route == 0)
            {
                Assert.Same(destination, entry["DestinationAddress"]);
                Assert.Equal(send.Context!.MessageId, entry["MessageId"]);
                Assert.Equal(dueAt, entry["DeliveryTime"]);
                Assert.Equal(send.Context.ScheduledMessageId, entry["Token"]);
            }
            else
            {
                Assert.Equal(tokenId, entry["TokenId"]);
                if (route == 2)
                    Assert.Same(destination, entry["DestinationAddress"]);
            }
            if (observed != null)
                Assert.Same(loggerFailure, observed);
            Assert.Null(observed);
            Assert.True(operation.IsCompletedSuccessfully);
            if (route == 0)
            {
                ScheduledMessage<Probe> accepted = await scheduledOperation!;
                Assert.Equal(send.Context!.ScheduledMessageId, accepted.TokenId);
                Assert.Same(destination, accepted.Destination);
                Assert.Equal(dueAt, accepted.DueAt);
                Assert.Same(message, accepted.Payload);
            }
        }
        finally
        {
            send.Release();
            deletion.Release();
            try
            {
                if (operation != null)
                    await ObserveAcceptedTaskAsync(operation);
            }
            finally
            {
                try
                {
                    if (send.PublicTask != null)
                        await ObserveAcceptedTaskAsync(send.PublicTask);
                }
                finally
                {
                    try
                    {
                        if (send.Calls > 0)
                            await ObserveAcceptedTaskAsync(send.RawTask);
                    }
                    finally
                    {
                        try
                        {
                            if (deletion.Calls > 0)
                                await ObserveAcceptedTaskAsync(deletion.RawTask);
                        }
                        finally
                        {
                            LogContext.Current = previous;
                        }
                    }
                }
            }
        }
    }

    private static async Task ObserveAcceptedTaskAsync(Task task)
    {
        try
        {
            await task.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
        }
        catch (Exception exception) when (exception is not TimeoutException && (task.IsFaulted || task.IsCanceled))
        {
        }
    }

    private sealed class AcceptedOperationLogger(string selectedTemplate, Exception? failure) : ILogger
    {
        public List<Dictionary<string, object?>> Entries { get; } = new();
        public int ThrowCount { get; private set; }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel level) => level == LogLevel.Debug;
        public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (level != LogLevel.Debug)
                return;
            var values = ((IEnumerable<KeyValuePair<string, object?>>)(object)state!).ToDictionary(x => x.Key, x => x.Value);
            if (!Equals(values["{OriginalFormat}"], selectedTemplate))
                return;
            Entries.Add(values);
            if (failure != null)
            {
                ThrowCount++;
                throw failure;
            }
        }
    }

    private class AcceptedEndpointProviderProxy : DispatchProxy
    {
        public ISendEndpoint Endpoint { get; set; } = null!;
        public int Calls { get; private set; }
        public Uri? Address { get; private set; }
        public CancellationToken Token { get; private set; }
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method?.Name != "GetSendEndpointAsync")
                throw new NotSupportedException(method?.Name);
            Calls++;
            Address = (Uri)args![0]!;
            Token = (CancellationToken)args[1]!;
            return Task.FromResult(Endpoint);
        }
    }

    private class AcceptedSendProxy : DispatchProxy
    {
        readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        readonly TaskCompletionSource _raw = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task Entered => _entered.Task;
        public Task RawTask => _raw.Task;
        public Task? PublicTask { get; private set; }
        public MessageSendContext<Probe>? Context { get; private set; }
        public int Calls { get; private set; }
        public void Release() => _raw.TrySetResult();
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method?.Name == "SendAsync" && args is [Probe message, IPipe<SendContext<Probe>> pipe, CancellationToken token])
            {
                Calls++;
                Context = new MessageSendContext<Probe>(message, token);
                return PublicTask = SendAsync(Context, pipe);
            }
            throw new NotSupportedException(method?.Name);
        }
        async Task SendAsync(MessageSendContext<Probe> context, IPipe<SendContext<Probe>> pipe)
        {
            await pipe.SendAsync(context);
            _entered.TrySetResult();
            await _raw.Task;
        }
    }

    private class AcceptedDeleteProxy : DispatchProxy
    {
        readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        readonly TaskCompletionSource<bool> _raw = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task Entered => _entered.Task;
        public Task<bool> RawTask => _raw.Task;
        public int Calls { get; private set; }
        public Guid TokenId { get; private set; }
        public CancellationToken Token { get; private set; }
        public void Release() => _raw.TrySetResult(true);
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method?.Name != "DeleteScheduledMessageAsync")
                throw new NotSupportedException(method?.Name);
            Calls++;
            TokenId = (Guid)args![0]!;
            Token = (CancellationToken)args[1]!;
            _entered.TrySetResult();
            return _raw.Task;
        }
    }

    private class AcceptedConsumeProxy : DispatchProxy
    {
        public ClientContext Client { get; set; } = null!;
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method?.Name == "TryGetPayload" && method.IsGenericMethod && method.GetGenericArguments()[0] == typeof(ClientContext))
            {
                args![0] = Client;
                return true;
            }
            throw new NotSupportedException(method?.Name);
        }
    }

}
