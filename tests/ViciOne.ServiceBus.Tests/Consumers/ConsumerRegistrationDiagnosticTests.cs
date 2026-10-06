using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Consumers;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Consumers;

public sealed class ConsumerRegistrationDiagnosticTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    [InlineData(3, true)]
    [InlineData(4, false)]
    [InlineData(4, true)]
    [InlineData(5, false)]
    [InlineData(5, true)]
    [InlineData(6, false)]
    [InlineData(6, true)]
    [InlineData(7, false)]
    [InlineData(7, true)]
    [RequirementCoverage("REQ-VSB-CONSUMER-REGISTRATION-DIAGNOSTICS", "public-overloads-preserve-registration-and-business-progress")]
    public async Task Registration_DebugFailureDoesNotPreventPublicConsumerProgressAsync(int route, bool loggerThrows)
    {
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun().GetValidatedOptions().OperationTimeout!.Value;
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        using var harness = new InMemoryTestHarness($"consumer-registration-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        bool directConnection = route < 4;
        string template = route switch
        {
            0 => "Connecting Consumer: {ConsumerType} (by type, using object consumer factory)",
            1 => "Connecting Consumer: {ConsumerType} (using delegate consumer factory)",
            2 => "Connecting Consumer: {ConsumerType} (using supplied consumer factory)",
            3 => "Connecting Consumer: {ConsumerType} (using default constructor)",
            4 => "Subscribing Consumer: {ConsumerType} (by type, using object consumer factory)",
            5 => "Subscribing Consumer: {ConsumerType} (using default constructor)",
            6 => "Subscribing Consumer: {ConsumerType} (using delegate consumer factory)",
            7 => "Subscribing Consumer: {ConsumerType} (using supplied consumer factory)",
            _ => throw new ArgumentOutOfRangeException(nameof(route)),
        };
        var diagnosticFailure = new IOException($"Consumer registration diagnostic failure route {route}");
        var logger = new RegistrationLogger(template, loggerThrows ? diagnosticFailure : null);
        Exception? registrationFailure = null;
        ConnectHandle? handle = null;
        IConsumerConfigurator<RegisteredConsumer>? configured = null;
        int callbackCalls = 0;
        int factoryCalls = 0;
        int registrationCalls = 0;
        Task? start = null;
        Task? send = null;
        Task<IPublishedMessage<RegistrationHandled>>? published = null;
        Task<IConsumedMessage<RegistrationMessage>>? consumed = null;

        RegisteredConsumer CreateConsumer()
        {
            Interlocked.Increment(ref factoryCalls);
            return new RegisteredConsumer();
        }

        object CreateRuntimeConsumer(Type type)
        {
            if (type != typeof(RegisteredConsumer))
                throw new InvalidOperationException("Unexpected runtime consumer type.");
            return CreateConsumer();
        }

        void ConfigureConsumer(IConsumerConfigurator<RegisteredConsumer> consumer)
        {
            callbackCalls++;
            configured = consumer;
        }

        if (!directConnection)
        {
            harness.InMemoryReceiveEndpointConfiguring += endpoint =>
            {
                registrationCalls++;
                ILogContext? previous = LogContext.Current;
                try
                {
                    LogContext.ConfigureCurrentLogContext(logger);
                    registrationFailure = Record.Exception(() =>
                    {
                        switch (route)
                        {
                            case 4:
                                endpoint.Consumer(typeof(RegisteredConsumer), CreateRuntimeConsumer);
                                break;
                            case 5:
                                endpoint.Consumer<RegisteredConsumer>(ConfigureConsumer);
                                break;
                            case 6:
                                endpoint.Consumer<RegisteredConsumer>(CreateConsumer, ConfigureConsumer);
                                break;
                            case 7:
                                endpoint.Consumer(new DelegateConsumerFactory<RegisteredConsumer>(CreateConsumer), ConfigureConsumer);
                                break;
                        }
                    });
                }
                finally
                {
                    LogContext.Current = previous;
                }
            };
        }

        try
        {
            start = harness.StartAsync(lifetime.Token);
            await start.WaitAsync(timeout, lifetime.Token);
            if (directConnection)
            {
                registrationCalls++;
                ILogContext? previous = LogContext.Current;
                try
                {
                    LogContext.ConfigureCurrentLogContext(logger);
                    registrationFailure = Record.Exception(() =>
                    {
                        handle = route switch
                        {
                            0 => harness.Bus.ConnectConsumer(typeof(RegisteredConsumer), CreateRuntimeConsumer),
                            1 => harness.Bus.ConnectConsumer<RegisteredConsumer>(CreateConsumer),
                            2 => harness.Bus.ConnectConsumer(new DelegateConsumerFactory<RegisteredConsumer>(CreateConsumer)),
                            3 => harness.Bus.ConnectConsumer<RegisteredConsumer>(),
                            _ => throw new ArgumentOutOfRangeException(nameof(route)),
                        };
                    });
                }
                finally
                {
                    LogContext.Current = previous;
                }
            }

            RegistrationLogger.Entry selected = Assert.Single(logger.Entries, entry => entry.Template == template);
            Assert.Equal(LogLevel.Debug, selected.Level);
            Assert.Equal(TypeCache<RegisteredConsumer>.ShortName, selected.ConsumerType);
            Assert.Equal(1, registrationCalls);
            Assert.Equal(0, Volatile.Read(ref factoryCalls));
            Assert.Equal(loggerThrows ? 1 : 0, logger.ThrowCount);
            if (loggerThrows)
            {
                Assert.Same(diagnosticFailure, logger.ThrownFailure);
                if (registrationFailure is not null)
                    Assert.Same(diagnosticFailure, registrationFailure);
            }
            else
                Assert.Null(logger.ThrownFailure);

            Assert.Null(registrationFailure);
            if (directConnection)
            {
                Assert.NotNull(handle);
                Assert.Equal(0, callbackCalls);
                Assert.Null(configured);
            }
            else
            {
                Assert.Null(handle);
                Assert.Equal(route == 4 ? 0 : 1, callbackCalls);
                if (route != 4)
                    Assert.IsType<ConsumerConfigurator<RegisteredConsumer>>(configured);
            }

            Guid messageId = NewId.NextGuid();
            published = harness.Published.SelectAsync<RegistrationHandled>(
                    entry => entry.Context.Message.MessageId == messageId, lifetime.Token)
                .FirstObservedAsync(lifetime.Token);
            consumed = harness.Consumed.SelectAsync<RegistrationMessage>(
                    entry => entry.Context.Message.MessageId == messageId, lifetime.Token)
                .FirstObservedAsync(lifetime.Token);
            send = directConnection
                ? harness.BusSendEndpoint.SendAsync(new RegistrationMessage(messageId), lifetime.Token)
                : harness.InputQueueSendEndpoint.SendAsync(new RegistrationMessage(messageId), lifetime.Token);
            await send.WaitAsync(timeout, lifetime.Token);
            IPublishedMessage<RegistrationHandled> result = await published.WaitAsync(timeout, lifetime.Token);
            IConsumedMessage<RegistrationMessage> delivery = await consumed.WaitAsync(timeout, lifetime.Token);
            Assert.Equal(messageId, result.Context.Message.MessageId);
            Assert.Null(result.Exception);
            Assert.Equal(messageId, delivery.Context.Message.MessageId);
            Assert.Null(delivery.Exception);
            Assert.Equal(route is 3 or 5 ? 0 : 1, Volatile.Read(ref factoryCalls));
        }
        finally
        {
            try
            {
                lifetime.Cancel();
            }
            finally
            {
                try
                {
                    await ObserveAllAsync([send, published, consumed, start]);
                }
                finally
                {
                    try
                    {
                        handle?.Dispose();
                    }
                    finally
                    {
                        await harness.StopAsync(CancellationToken.None)
                            .WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
                    }
                }
            }
        }
        Assert.Single(harness.Published.Snapshot<RegistrationHandled>());
        IConsumedMessage<RegistrationMessage> observed = Assert.Single(harness.Consumed.Snapshot<RegistrationMessage>());
        Assert.Null(observed.Exception);
    }

    private static async Task ObserveAllAsync(Task?[] tasks, int index = 0)
    {
        if (index == tasks.Length)
            return;
        try
        {
            if (tasks[index] is { } task)
            {
                try
                {
                    await task.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
                }
                catch (Exception failure) when (failure is not TimeoutException && (task.IsFaulted || task.IsCanceled))
                {
                }
            }
        }
        finally
        {
            await ObserveAllAsync(tasks, index + 1);
        }
    }

    private sealed class RegistrationLogger(string selectedTemplate, Exception? failure) : ILogger
    {
        public sealed record Entry(LogLevel Level, string? Template, string? ConsumerType);
        public List<Entry> Entries { get; } = [];
        public int ThrowCount { get; private set; }
        public Exception? ThrownFailure { get; private set; }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel level) => level == LogLevel.Debug;
        public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            IEnumerable<KeyValuePair<string, object?>> fields = state as IEnumerable<KeyValuePair<string, object?>>
                ?? Array.Empty<KeyValuePair<string, object?>>();
            string? template = fields.FirstOrDefault(field => field.Key == "{OriginalFormat}").Value as string;
            string? consumerType = fields.FirstOrDefault(field => field.Key == "ConsumerType").Value as string;
            Entries.Add(new Entry(level, template, consumerType));
            if (template == selectedTemplate && failure is not null)
            {
                ThrowCount++;
                ThrownFailure = failure;
                throw failure;
            }
        }
    }

    public sealed record RegistrationMessage(Guid MessageId);
    public sealed record RegistrationHandled(Guid MessageId);

    public sealed class RegisteredConsumer : IConsumer<RegistrationMessage>
    {
        public Task ConsumeAsync(ConsumeContext<RegistrationMessage> context) =>
            context.Advanced().PublishAsync(new RegistrationHandled(context.Message.MessageId), context.CancellationToken);
    }
}
