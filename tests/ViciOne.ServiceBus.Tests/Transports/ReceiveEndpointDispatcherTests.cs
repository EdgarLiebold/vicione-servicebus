using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.Logging;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Transports;

public sealed class ReceiveEndpointDispatcherTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RECEIVE-DISPATCHER", "caller-cancellation-clears-active-dispatch-and-allows-successor")]
    public async Task TypedDispatcher_CallerCancellationStopsPendingConsumerAndAllowsSuccessorAsync()
    {
        TimeSpan timeout = OperationTimeout();
        var observation = new DispatchCancellationObservation();
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(observation)
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddConsumer<DispatchCancellationConsumer>();
                configuration.AddConfigureEndpointsCallback((_, endpoint) =>
                    endpoint.UseRawJsonSerializer(RawSerializerOptions.AnyMessageType));
            })
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken)
            .WaitAsync(timeout, TestContext.Current.CancellationToken);

        try
        {
            IReceiveEndpointDispatcher<DispatchCancellationConsumer> dispatcher =
                provider.GetRequiredService<IReceiveEndpointDispatcher<DispatchCancellationConsumer>>();
            using var caller = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            (byte[] blockedBody, Dictionary<string, object> blockedHeaders) = Serialize(
                DispatchBody.RawJson, NewId.NextGuid(), "blocked");
            Task blocked = dispatcher.DispatchAsync(blockedBody, blockedHeaders, [], caller.Token);
            try
            {
                await observation.Entered.Task.WaitAsync(timeout, TestContext.Current.CancellationToken);
                Assert.False(blocked.IsCompleted);
                Assert.Equal(1, dispatcher.ActiveDispatchCount);

                await caller.CancelAsync();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                    blocked.WaitAsync(timeout, TestContext.Current.CancellationToken));
                Assert.Equal(0, dispatcher.ActiveDispatchCount);

                (byte[] successorBody, Dictionary<string, object> successorHeaders) = Serialize(
                    DispatchBody.RawJson, NewId.NextGuid(), "successor");
                await dispatcher.DispatchAsync(successorBody, successorHeaders, [], TestContext.Current.CancellationToken)
                    .WaitAsync(timeout, TestContext.Current.CancellationToken);

                Assert.Equal("successor", await observation.Successor.Task.WaitAsync(timeout, TestContext.Current.CancellationToken));
                Assert.Equal(0, dispatcher.ActiveDispatchCount);
                Assert.Equal(2, dispatcher.DispatchCount);
            }
            finally
            {
                observation.Release.TrySetResult();
                try
                {
                    await blocked.WaitAsync(timeout, CancellationToken.None);
                }
                catch (OperationCanceledException) when (caller.IsCancellationRequested)
                {
                }
            }
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECEIVE-DISPATCHER", "typed-dispatcher-reports-concurrent-delivery-until-consumers-finish")]
    public async Task TypedDispatcher_ReportsConcurrentDeliveryAndSignalsZeroOnlyAfterBothConsumersFinishAsync()
    {
        TimeSpan timeout = OperationTimeout();
        using var caller = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var gate = new DispatchGate();
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(gate)
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddConsumer<DispatchHoldingConsumer>();
                configuration.AddConfigureEndpointsCallback((_, endpoint) =>
                    endpoint.UseRawJsonSerializer(RawSerializerOptions.AnyMessageType));
            })
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: caller.Token).WaitAsync(timeout, caller.Token);

        try
        {
            IReceiveEndpointDispatcher<DispatchHoldingConsumer> dispatcher =
                provider.GetRequiredService<IReceiveEndpointDispatcher<DispatchHoldingConsumer>>();
            int zeroSignals = 0;
            var zeroEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseZero = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            dispatcher.ZeroActivity += async () =>
            {
                Interlocked.Increment(ref zeroSignals);
                zeroEntered.TrySetResult();
                await releaseZero.Task;
            };
            (byte[] firstBody, Dictionary<string, object> firstHeaders) = Serialize(
                DispatchBody.RawJson, NewId.NextGuid(), "first");
            (byte[] secondBody, Dictionary<string, object> secondHeaders) = Serialize(
                DispatchBody.RawJson, NewId.NextGuid(), "second");

            Task first = dispatcher.DispatchAsync(firstBody, firstHeaders, [], caller.Token);
            Task second = dispatcher.DispatchAsync(secondBody, secondHeaders, [], caller.Token);
            try
            {
                await gate.BothEntered.Task.WaitAsync(timeout, caller.Token);

                Assert.False(first.IsCompleted);
                Assert.False(second.IsCompleted);
                Assert.Equal(2, dispatcher.ActiveDispatchCount);
                Assert.Equal(2, dispatcher.DispatchCount);
                Assert.Equal(2, dispatcher.MaxConcurrentDispatchCount);
                Assert.Equal(0, Volatile.Read(ref zeroSignals));
                Assert.Equal(["first", "second"], gate.Values.Order());

                gate.ReleaseFirst.TrySetResult();
                await first.WaitAsync(timeout, caller.Token);

                Assert.False(second.IsCompleted);
                Assert.Equal(1, dispatcher.ActiveDispatchCount);
                Assert.Equal(0, Volatile.Read(ref zeroSignals));

                gate.ReleaseSecond.TrySetResult();
                await zeroEntered.Task.WaitAsync(timeout, caller.Token);
                Assert.False(second.IsCompleted);
                Assert.Equal(0, dispatcher.ActiveDispatchCount);
                Assert.Equal(1, Volatile.Read(ref zeroSignals));

                releaseZero.TrySetResult();
                await second.WaitAsync(timeout, caller.Token);

                Assert.Equal(0, dispatcher.ActiveDispatchCount);
                Assert.Equal(2, dispatcher.GetMetrics().DeliveryCount);
                Assert.Equal(2, dispatcher.GetMetrics().MaxConcurrentDeliveryCount);
                Assert.Equal(1, Volatile.Read(ref zeroSignals));
            }
            finally
            {
                gate.ReleaseFirst.TrySetResult();
                gate.ReleaseSecond.TrySetResult();
                releaseZero.TrySetResult();
                await Task.WhenAll(first, second).WaitAsync(timeout, CancellationToken.None);
            }
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Theory]
    [InlineData(DispatchBody.RawJson)]
    [InlineData(DispatchBody.Empty)]
    [RequirementCoverage("REQ-VSB-RECEIVE-DISPATCHER", "raw-json-and-empty-body-dispatch-through-consumer-pipeline")]
    public async Task Dispatch_DeliversRawJsonAndEmptyBodiesThroughTheConfiguredConsumerAsync(DispatchBody body)
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observation = new DispatchObservation();
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(observation)
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddConsumer<DispatchCommandConsumer>();
                configuration.AddConfigureEndpointsCallback((_, endpoint) =>
                    endpoint.UseRawJsonSerializer(RawSerializerOptions.AnyMessageType));
            })
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

        try
        {
            Guid messageId = NewId.NextGuid();
            string? expectedValue = body == DispatchBody.RawJson ? "dispatcher-value" : null;
            (byte[] bytes, Dictionary<string, object> headers) = Serialize(body, messageId, expectedValue);
            IReceiveEndpointDispatcher<DispatchCommandConsumer> dispatcher =
                provider.GetRequiredService<IReceiveEndpointDispatcher<DispatchCommandConsumer>>();

            await dispatcher.DispatchAsync(bytes, headers, [], cancellationToken).WaitAsync(timeout, cancellationToken);
            DispatchResult result = await observation.Completed.Task.WaitAsync(timeout, cancellationToken);

            Assert.Equal(messageId, result.MessageId);
            Assert.Equal(expectedValue, result.Value);
            Assert.Equal(SystemTextJsonRawMessageSerializer.JsonContentType.ToString(), result.ContentType);
            Assert.Equal(1, observation.InvocationCount);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-RECEIVE-DISPATCHER", "dispose-diagnostics-do-not-retain-cached-receivers")]
    public async Task DispatcherDispose_DiagnosticFailureDoesNotRetainCachedReceiversAsync(bool loggerThrows)
    {
        TimeSpan timeout = OperationTimeout();
        var observation = new DispatchObservation();
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(observation)
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddConsumer<DispatchCommandConsumer>();
                configuration.AddConfigureEndpointsCallback((_, endpoint) =>
                    endpoint.UseRawJsonSerializer(RawSerializerOptions.AnyMessageType));
            })
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken)
            .WaitAsync(timeout, TestContext.Current.CancellationToken);
        ILogContext? previous = LogContext.Current;
        Task? dispose = null;
        try
        {
            var factory = Assert.IsType<ReceiveEndpointDispatcherFactory>(provider.GetRequiredService<IReceiveEndpointDispatcherFactory>());
            string queue = "factory-dispose-" + NewId.NextGuid().ToString("N");
            IReceiveEndpointDispatcher receiver = factory.CreateReceiver(queue,
                (endpoint, registration) => registration.ConfigureConsumer<DispatchCommandConsumer>(endpoint));
            Assert.Same(receiver, factory.CreateReceiver(queue,
                (endpoint, registration) => registration.ConfigureConsumer<DispatchCommandConsumer>(endpoint)));
            Assert.Equal(1, ReadCachedReceiverCount(factory));
            var primary = new IOException("dispatcher disposal diagnostic failed");
            var logger = new SelectedDispatcherCompletionLogger(loggerThrows ? primary : null);
            LogContext.ConfigureCurrentLogContext(logger);
            dispose = DisposeFactoryAsync(factory);
            Exception? failure = await Record.ExceptionAsync(() => dispose.WaitAsync(timeout, CancellationToken.None));
            Assert.IsNotType<TimeoutException>(failure);
            Assert.True(dispose.IsCompleted);
            Assert.Equal(1, logger.TargetCount);
            Assert.Equal(receiver.InputAddress, logger.InputAddress);
            Assert.Equal(loggerThrows ? 1 : 0, logger.ThrowCount);
            if (failure is not null)
                Assert.Same(primary, failure);

            Assert.Equal(0, ReadCachedReceiverCount(factory));
            Assert.Null(failure);
            await factory.DisposeAsync();
            Assert.Equal(1, logger.TargetCount);
            Assert.Throws<ObjectDisposedException>(() => factory.CreateReceiver(queue));
        }
        finally
        {
            try
            {
                if (dispose is not null)
                {
                    try { await dispose.WaitAsync(timeout, CancellationToken.None); }
                    catch (Exception failure) when (failure is not TimeoutException && (dispose.IsFaulted || dispose.IsCanceled)) { }
                }
            }
            finally
            {
                LogContext.Current = previous!;
                await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
            }
        }
    }

    private static async Task DisposeFactoryAsync(ReceiveEndpointDispatcherFactory factory) => await factory.DisposeAsync();

    private static int ReadCachedReceiverCount(ReceiveEndpointDispatcherFactory factory)
    {
        // Observe cache retirement only. No private state is modified or dispatcher disposal inferred.
        var field = typeof(ReceiveEndpointDispatcherFactory).GetField("_dispatchers",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("The dispatcher cache observation seam is absent.");
        return Assert.IsType<System.Collections.Concurrent.ConcurrentDictionary<string, Lazy<IReceiveEndpointDispatcher>>>(
            field.GetValue(factory)).Count;
    }

    private sealed class SelectedDispatcherCompletionLogger(Exception? failure) : ILogger
    {
        public int TargetCount { get; private set; }
        public int ThrowCount { get; private set; }
        public Uri? InputAddress { get; private set; }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (level != LogLevel.Debug || state is not IEnumerable<KeyValuePair<string, object?>> values)
                return;
            Dictionary<string, object?> fields = values.ToDictionary(x => x.Key, x => x.Value);
            if (!fields.TryGetValue("{OriginalFormat}", out object? template)
                || !Equals(template, "Dispatcher completed {InputAddress}: {DeliveryCount} received, {ConcurrentDeliveryCount} concurrent"))
                return;
            TargetCount++;
            InputAddress = fields.GetValueOrDefault("InputAddress") as Uri;
            if (failure is not null)
            {
                ThrowCount++;
                throw failure;
            }
        }
    }

    private static (byte[], Dictionary<string, object>) Serialize(
        DispatchBody body,
        Guid messageId,
        string? value)
    {
        var sendContext = new MessageSendContext<DispatchCommand>(new DispatchCommand { Value = value })
        {
            MessageId = messageId,
        };
        byte[] bytes = body == DispatchBody.Empty
            ? []
            : new SystemTextJsonRawMessageSerializer(ServiceBusMetadataJson.Options).GetMessageBody(sendContext).ToArray();
        var headers = new Dictionary<string, object>
        {
            [MessageHeaders.ContentType] = SystemTextJsonRawMessageSerializer.JsonContentType,
            [MessageHeaders.MessageId] = messageId,
        };
        headers.Set(sendContext.Headers);
        return (bytes, headers);
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    public enum DispatchBody
    {
        RawJson,
        Empty,
    }

    public sealed class DispatchCommand
    {
        public string? Value { get; init; }
    }

    public sealed record DispatchResult(Guid? MessageId, string? Value, string? ContentType);

    public sealed class DispatchObservation
    {
        private int _invocationCount;

        public int InvocationCount => Volatile.Read(ref _invocationCount);

        public TaskCompletionSource<DispatchResult> Completed { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Record(DispatchResult result)
        {
            Interlocked.Increment(ref _invocationCount);
            Completed.TrySetResult(result);
        }
    }

    public sealed class DispatchGate
    {
        private int _entered;

        public List<string?> Values { get; } = [];

        public TaskCompletionSource BothEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource ReleaseFirst { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource ReleaseSecond { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Enter(string? value)
        {
            lock (Values)
                Values.Add(value);

            if (Interlocked.Increment(ref _entered) == 2)
                BothEntered.TrySetResult();
        }
    }

    public sealed class DispatchCancellationObservation
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<string?> Successor { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public sealed class DispatchCancellationConsumer(DispatchCancellationObservation observation) : IConsumer<DispatchCommand>
    {
        public async Task ConsumeAsync(ConsumeContext<DispatchCommand> context)
        {
            if (context.Message.Value == "blocked")
            {
                observation.Entered.TrySetResult();
                await observation.Release.Task.WaitAsync(context.CancellationToken);
            }
            else
                observation.Successor.TrySetResult(context.Message.Value);
        }
    }

    public sealed class DispatchHoldingConsumer(DispatchGate gate) : IConsumer<DispatchCommand>
    {
        public async Task ConsumeAsync(ConsumeContext<DispatchCommand> context)
        {
            gate.Enter(context.Message.Value);
            Task release = context.Message.Value == "first" ? gate.ReleaseFirst.Task : gate.ReleaseSecond.Task;
            await release.WaitAsync(context.CancellationToken);
        }
    }

    public sealed class DispatchCommandConsumer(DispatchObservation observation) : IConsumer<DispatchCommand>
    {
        public Task ConsumeAsync(ConsumeContext<DispatchCommand> context)
        {
            observation.Record(new DispatchResult(
                context.MessageId,
                context.Message.Value,
                context.Advanced().ReceiveContext.ContentType?.ToString()));
            return Task.CompletedTask;
        }
    }
}
