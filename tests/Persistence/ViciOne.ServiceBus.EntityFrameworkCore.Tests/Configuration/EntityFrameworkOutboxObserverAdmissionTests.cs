using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Providers.Persistence;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.EntityFrameworkCore.Tests.Configuration;

public sealed class EntityFrameworkOutboxObserverAdmissionTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-CONFIGURATION", "second-observer-admission-retires-returned-handle")]
    public async Task UseOutbox_SecondObserverAdmissionRetiresTheReturnedConsumerHandleAsync(int mode)
    {
        var admissionFailure = new IOException("unique EF saga observer admission failure");
        var cleanupFailure = new IOException("unique EF consumer observer cleanup failure");
        IReceiveEndpointConfigurator endpoint = DispatchProxy.Create<IReceiveEndpointConfigurator, EndpointProxy>();
        var receiver = (EndpointProxy)(object)endpoint;
        receiver.AdmissionFailure = mode == 0 ? null : admissionFailure;
        receiver.CleanupFailure = mode == 2 ? cleanupFailure : null;
        ILogContext? previous = LogContext.Current;
        ServiceProvider? provider = null;
        Exception? testFailure = null;
        var cleanupFailures = new List<Exception>();
        var callbackCount = 0;
        IOutboxOptionsConfigurator? suppliedOptions = null;
        try
        {
            var services = new ServiceCollection();
            services.AddViciOneServiceBus(configurator => configurator.UsingInMemory());
            services.AddSingleton(BusPersistenceIdentity<IBus>.Create("ef-admission-tests"));
            provider = services.BuildServiceProvider();
            IBusRegistrationContext context = provider.GetRequiredService<IBusRegistrationContext>();
            Assert.IsAssignableFrom<ISetScopedConsumeContext>(context);
            Assert.Equal("ef-admission-tests", provider.GetRequiredService<BusPersistenceIdentity<IBus>>().Require("test"));

            Exception? failure = Record.Exception(() =>
            {
                endpoint.UseEntityFrameworkOutbox<TestDbContext>(context, options =>
                {
                    callbackCount++;
                    suppliedOptions = options;
                });
            });
            Assert.Equal(1, callbackCount);
            Assert.Equal(new[] { "consumer", "saga" }, receiver.AdmissionOrder);
            Assert.NotNull(receiver.ConsumerObserver);
            Assert.Same(receiver.ConsumerObserver, receiver.SagaObserver);
            Assert.Same(receiver.ConsumerObserver, suppliedOptions);
            Assert.Single(receiver.Handles, handle => handle.Kind == "consumer");
            if (mode == 0)
            {
                Assert.Null(failure);
                Assert.Equal(1, receiver.Consumers.Count);
                Assert.Equal(1, receiver.Sagas.Count);
                Assert.Equal(new[] { "consumer", "saga" }, receiver.Handles.Select(handle => handle.Kind));
                Assert.All(receiver.Handles, handle => Assert.Equal(0, handle.RetirementCalls));
            }
            else
            {
                Assert.NotNull(failure);
                Assert.Single(ExceptionTree(failure), cause => ReferenceEquals(cause, admissionFailure));
                // Actual public registration count precedes any fallback or secondary cause assertion.
                Assert.Equal(0, receiver.Consumers.Count);
                Assert.Equal(0, receiver.Sagas.Count);
                ReturnedHandle first = Assert.Single(receiver.Handles);
                Assert.Equal(1, first.RetirementCalls);
                Assert.True(first.RealDisconnected);
                if (mode == 1)
                    Assert.Same(admissionFailure, failure);
                else
                {
                    AggregateException aggregate = Assert.IsType<AggregateException>(failure);
                    Assert.Collection(aggregate.InnerExceptions,
                        cause => Assert.Same(admissionFailure, cause),
                        cause => Assert.Same(cleanupFailure, cause));
                    Assert.Equal(1, first.ThrowCalls);
                }
            }

            IConsumerMessageConfigurator<TestConsumer, TestMessage> consumer =
                DispatchProxy.Create<IConsumerNotification, MessageProxy>();
            ISagaMessageConfigurator<TestSaga, TestMessage> saga =
                DispatchProxy.Create<ISagaNotification, MessageProxy>();
            var consumerMessages = (MessageProxy)(object)consumer;
            var sagaMessages = (MessageProxy)(object)saga;
            receiver.Consumers.ConsumerMessageConfigured(consumer);
            receiver.Sagas.SagaMessageConfigured(saga);
            if (mode == 0)
            {
                Assert.IsType<FilterPipeSpecification<ConsumeContext<TestMessage>>>(Assert.Single(consumerMessages.Specifications));
                Assert.IsType<FilterPipeSpecification<ConsumeContext<TestMessage>>>(Assert.Single(sagaMessages.Specifications));
            }
            else
            {
                Assert.Empty(consumerMessages.Specifications);
                Assert.Empty(sagaMessages.Specifications);
            }
            receiver.DisableCleanupFailure();
            foreach (ReturnedHandle handle in receiver.Handles)
                handle.Disconnect();
            Assert.Equal(0, receiver.Consumers.Count);
            Assert.Equal(0, receiver.Sagas.Count);
        }
        catch (Exception failure)
        {
            testFailure = failure;
        }
        finally
        {
            receiver.DisableCleanupFailure();
            try
            {
                foreach (ReturnedHandle handle in receiver.Handles)
                {
                    try { handle.Disconnect(); }
                    catch (Exception failure) { cleanupFailures.Add(failure); }
                }
                if (provider is not null)
                {
                    try
                    {
                        Task disposal = provider.DisposeAsync().AsTask();
                        await disposal.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
                    }
                    catch (Exception failure) { cleanupFailures.Add(failure); }
                }
            }
            finally { LogContext.Current = previous; }
        }
        if (testFailure is not null)
            cleanupFailures.Insert(0, testFailure);
        if (cleanupFailures.Count == 1)
            ExceptionDispatchInfo.Capture(cleanupFailures[0]).Throw();
        if (cleanupFailures.Count > 1)
            throw new AggregateException("EF outbox observer-admission test and cleanup failed.", cleanupFailures);
    }

    static IEnumerable<Exception> ExceptionTree(Exception failure)
    {
        yield return failure;
        if (failure is AggregateException aggregate)
        {
            foreach (Exception inner in aggregate.InnerExceptions)
                foreach (Exception cause in ExceptionTree(inner))
                    yield return cause;
        }
        else if (failure.InnerException is Exception inner)
            foreach (Exception cause in ExceptionTree(inner))
                yield return cause;
    }

    public interface IConsumerNotification : IConsumerMessageConfigurator<TestConsumer, TestMessage>, IConsumerMessageConfigurator<TestMessage>;
    public interface ISagaNotification : ISagaMessageConfigurator<TestSaga, TestMessage>, ISagaMessageConfigurator<TestMessage>;
    public sealed class TestMessage;
    public sealed class TestConsumer;
    public sealed class TestSaga;
    sealed class TestDbContext : DbContext;

    public class MessageProxy : DispatchProxy
    {
        public List<object> Specifications { get; } = [];
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method?.Name == "AddPipeSpecification" && args is { Length: 1 } && args[0] is object specification)
            {
                Specifications.Add(specification);
                return null;
            }
            throw new InvalidOperationException($"Unexpected public message configurator call: {method?.Name}.");
        }
    }

    public sealed class ReturnedHandle(string kind, ConnectHandle actual, IOException? cleanupFailure) : ConnectHandle
    {
        int _disconnected;
        int _failureDisabled;
        public string Kind { get; } = kind;
        public int DisposeCalls { get; private set; }
        public int DisconnectCalls { get; private set; }
        public int RetirementCalls => DisposeCalls + DisconnectCalls;
        public int ThrowCalls { get; private set; }
        public bool RealDisconnected => Volatile.Read(ref _disconnected) != 0;
        public void DisableFailure() => Volatile.Write(ref _failureDisabled, 1);
        public void Dispose() { DisposeCalls++; Retire(); }
        public void Disconnect() { DisconnectCalls++; Retire(); }
        void Retire()
        {
            if (Interlocked.Exchange(ref _disconnected, 1) != 0)
                return;
            actual.Disconnect();
            if (cleanupFailure is not null && Volatile.Read(ref _failureDisabled) == 0)
            {
                ThrowCalls++;
                throw cleanupFailure;
            }
        }
    }

    public class EndpointProxy : DispatchProxy
    {
        public ConsumerConfigurationObservable Consumers { get; } = new();
        public SagaConfigurationObservable Sagas { get; } = new();
        public List<ReturnedHandle> Handles { get; } = [];
        public List<string> AdmissionOrder { get; } = [];
        public object? ConsumerObserver { get; private set; }
        public object? SagaObserver { get; private set; }
        public IOException? AdmissionFailure { get; set; }
        public IOException? CleanupFailure { get; set; }
        public void DisableCleanupFailure() { foreach (ReturnedHandle handle in Handles) handle.DisableFailure(); }
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method?.Name == "get_InputAddress")
                return new Uri("loopback://localhost/ef-observer-admission");
            if (method?.Name == "ConnectConsumerConfigurationObserver" && args is { Length: 1 } && args[0] is IConsumerConfigurationObserver consumer)
            {
                AdmissionOrder.Add("consumer");
                ConsumerObserver = consumer;
                var handle = new ReturnedHandle("consumer", Consumers.Connect(consumer), CleanupFailure);
                Handles.Add(handle);
                return handle;
            }
            if (method?.Name == "ConnectSagaConfigurationObserver" && args is { Length: 1 } && args[0] is ISagaConfigurationObserver saga)
            {
                AdmissionOrder.Add("saga");
                SagaObserver = saga;
                if (AdmissionFailure is not null)
                    throw AdmissionFailure;
                var handle = new ReturnedHandle("saga", Sagas.Connect(saga), null);
                Handles.Add(handle);
                return handle;
            }
            throw new InvalidOperationException($"Unexpected public endpoint configurator call: {method?.Name}.");
        }
    }
}
