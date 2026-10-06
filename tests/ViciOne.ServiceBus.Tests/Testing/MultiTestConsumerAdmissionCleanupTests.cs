using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Util;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Testing;

public sealed class MultiTestConsumerAdmissionCleanupTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-MULTI-CONSUMER", "failed-admission-all-acquired-retirement-and-causes")]
    public async Task MultiTestConsumer_AdmissionRetiresAllAcquiredConsumersAndPreservesCausesAsync(int failureMode)
    {
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun().GetValidatedOptions().OperationTimeout!.Value;
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var harness = new InMemoryTestHarness($"multi-admission-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        var subject = new MultiTestConsumer(timeout, lifetime.Token);
        subject.AddConsumer<FirstContract>();
        subject.AddConsumer<SecondContract>();
        subject.AddConsumer<ThirdContract>();
        var operations = new List<Task>();
        IHostReceiveEndpointHandle? endpoint = null;
        AdmissionConnector? connector = null;
        ConnectHandle? composite = null;

        async Task JoinOperationAsync(Task operation)
        {
            operations.Add(operation);
            await operation.WaitAsync(timeout, CancellationToken.None);
        }

        try
        {
            await JoinOperationAsync(harness.StartAsync(lifetime.Token));
            endpoint = harness.Bus.ConnectReceiveEndpoint($"multi-admission-target-{NewId.NextGuid():N}");
            await JoinOperationAsync(endpoint.Ready);
            connector = new AdmissionConnector(endpoint.ReceiveEndpoint, failureMode);
            Exception? failure = Record.Exception(() => { composite = subject.Connect(connector); });
            Assert.Equal(3, connector.ConnectionCalls);
            Assert.Equal(new[] { typeof(FirstContract), typeof(SecondContract), typeof(ThirdContract) }, connector.AttemptedTypes);
            Assert.Equal(failureMode == 0 ? 3 : 2, connector.Acquired.Count);
            Assert.Empty(subject.Consumed.Snapshot());
            if (failureMode == 0)
            {
                Assert.Null(failure);
                Assert.NotNull(composite);
                Assert.Null(connector.ThrownAdmission);
                Assert.Equal(3, connector.Registrations.Count);
                composite.Disconnect();
                Assert.All(connector.Acquired, handle => Assert.Equal(1, handle.ReleaseAttempts));
                Assert.Equal(0, connector.Registrations.Count);
            }
            else
            {
                Assert.Null(composite);
                Assert.Same(connector.AdmissionFailure, connector.ThrownAdmission);
                Assert.NotNull(failure);
                Assert.Equal(1, connector.Acquired[0].ReleaseAttempts);
                Assert.Equal(failureMode >= 2 ? 1 : 0, connector.Acquired[0].CleanupThrows);
                Assert.Equal(failureMode >= 2 ? connector.CleanupFailures[0] : null, connector.Acquired[0].ThrownCleanup);
                // Causal second-real-registration retirement before aggregate/cause classification.
                Assert.Equal(1, connector.Acquired[1].ReleaseAttempts);
                Assert.Equal(0, connector.Registrations.Count);
                Assert.Equal(failureMode == 3 ? 1 : 0, connector.Acquired[1].CleanupThrows);
                Assert.Equal(failureMode == 3 ? connector.CleanupFailures[1] : null, connector.Acquired[1].ThrownCleanup);
                if (failureMode == 1)
                    Assert.Same(connector.AdmissionFailure, failure);
                else
                {
                    Exception[] causes = ExceptionTree(failure).ToArray();
                    Assert.Single(causes, cause => ReferenceEquals(cause, connector.AdmissionFailure));
                    Assert.Single(causes, cause => ReferenceEquals(cause, connector.CleanupFailures[0]));
                    if (failureMode == 3)
                        Assert.Single(causes, cause => ReferenceEquals(cause, connector.CleanupFailures[1]));
                }
            }
            await JoinOperationAsync(endpoint.StopAsync(CancellationToken.None));
            await JoinOperationAsync(harness.StopAsync(CancellationToken.None));
        }
        finally
        {
            try { lifetime.Cancel(); }
            finally
            {
                try { connector?.ReleaseAllWithoutInjectedFault(); }
                finally
                {
                    try { await JoinAllAsync(operations); }
                    finally
                    {
                        try
                        {
                            if (endpoint is not null)
                                await endpoint.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
                        }
                        finally
                        {
                            try { await harness.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None); }
                            finally { await harness.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None); }
                        }
                    }
                }
            }
        }
    }

    private static async Task JoinAllAsync(IEnumerable<Task> operations)
    {
        var failures = new List<Exception>();
        foreach (Task operation in operations)
        {
            try { await operation.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None); }
            catch (Exception exception) { failures.Add(exception); }
        }
        if (failures.Count != 0)
            throw new AggregateException("Multi-consumer fixture operations failed to terminate successfully.", failures);
    }

    private static IEnumerable<Exception> ExceptionTree(Exception exception)
    {
        yield return exception;
        if (exception is AggregateException aggregate)
        {
            foreach (Exception inner in aggregate.InnerExceptions)
                foreach (Exception cause in ExceptionTree(inner))
                    yield return cause;
        }
        else if (exception.InnerException is Exception inner)
            foreach (Exception cause in ExceptionTree(inner))
                yield return cause;
    }

    private sealed class AdmissionConnector(IConsumePipeConnector actual, int failureMode) : IConsumePipeConnector
    {
        public readonly InvalidOperationException AdmissionFailure = new("unique multi-consumer third-admission failure");
        public readonly Exception[] CleanupFailures =
        [new IOException("unique first multi-consumer retirement failure"), new ApplicationException("unique second multi-consumer retirement failure")];
        public readonly Connectable<TrackedHandle> Registrations = new();
        public readonly List<TrackedHandle> Acquired = [];
        public readonly List<Type> AttemptedTypes = [];
        public int ConnectionCalls;
        public Exception? ThrownAdmission;
        public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe) where T : class =>
            Connect(() => actual.ConnectConsumePipe(pipe), typeof(T));
        public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe, ConnectPipeOptions options) where T : class =>
            Connect(() => actual.ConnectConsumePipe(pipe, options), typeof(T));
        private ConnectHandle Connect(Func<ConnectHandle> connect, Type type)
        {
            AttemptedTypes.Add(type);
            if (++ConnectionCalls == 3 && failureMode != 0)
            {
                ThrownAdmission = AdmissionFailure;
                throw AdmissionFailure;
            }
            ConnectHandle actualHandle = connect();
            int index = Acquired.Count;
            Exception? cleanup = (index == 0 && failureMode >= 2) || (index == 1 && failureMode == 3)
                ? CleanupFailures[index] : null;
            var handle = new TrackedHandle(actualHandle, cleanup);
            handle.Tracking = Registrations.Connect(handle);
            Acquired.Add(handle);
            return handle;
        }
        public void ReleaseAllWithoutInjectedFault()
        {
            var failures = new List<Exception>();
            foreach (TrackedHandle handle in Acquired)
            {
                try { handle.ReleaseInnerOnly(); }
                catch (Exception exception) { failures.Add(exception); }
            }
            if (failures.Count != 0)
                throw new AggregateException("Actual multi-consumer registration cleanup failed.", failures);
        }
    }

    private sealed class TrackedHandle(ConnectHandle actual, Exception? cleanupFailure) : ConnectHandle
    {
        public ConnectHandle? Tracking;
        public int ReleaseAttempts;
        public int CleanupThrows;
        public Exception? ThrownCleanup;
        public void Disconnect() => Release();
        public void Dispose() => Release();
        private void Release()
        {
            Interlocked.Increment(ref ReleaseAttempts);
            ReleaseInnerOnly();
            if (cleanupFailure is not null && Interlocked.CompareExchange(ref CleanupThrows, 1, 0) == 0)
            {
                ThrownCleanup = cleanupFailure;
                throw cleanupFailure;
            }
        }
        public void ReleaseInnerOnly()
        {
            try { actual.Disconnect(); }
            finally { Tracking?.Disconnect(); }
        }
    }

    public sealed record FirstContract(Guid Id);
    public sealed record SecondContract(Guid Id);
    public sealed record ThirdContract(Guid Id);
}
