using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Util;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Consumers;

public sealed class InstanceConnectionAdmissionCleanupTests
{
    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, 1)]
    [InlineData(false, 2)]
    [InlineData(true, 0)]
    [InlineData(true, 1)]
    [InlineData(true, 2)]
    [RequirementCoverage("REQ-VSB-DYNAMIC-CONSUMER-CONNECTION", "instance-admission-all-acquired-cleanup-and-primary-causes")]
    public async Task InstanceAdmission_AttemptsEveryAcquiredRegistrationAndPreservesCausesAsync(bool generic, int failureMode)
    {
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun().GetValidatedOptions().OperationTimeout!.Value;
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var harness = new InMemoryTestHarness($"instance-admission-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        var consumer = new ThreeContractConsumer();
        var started = new List<StartedTask> { new(consumer.Release.Task, false) };
        IHostReceiveEndpointHandle? endpoint = null;
        AdmissionConnector? facade = null;

        Task ObserveAsync(Task task)
        {
            Task observation = task.WaitAsync(timeout, budget.Token);
            started.Add(new StartedTask(observation, true));
            return observation;
        }

        Task<T> ObserveValueAsync<T>(Task<T> task)
        {
            Task<T> observation = task.WaitAsync(timeout, budget.Token);
            started.Add(new StartedTask(observation, true));
            return observation;
        }

        Task OperationAsync(Task task)
        {
            started.Add(new StartedTask(task, false));
            return ObserveAsync(task);
        }

        Task<T> OperationValueAsync<T>(Task<T> task)
        {
            started.Add(new StartedTask(task, false));
            return ObserveValueAsync(task);
        }

        try
        {
            await OperationAsync(harness.StartAsync(budget.Token));
            endpoint = harness.Bus.ConnectReceiveEndpoint($"instance-target-{NewId.NextGuid():N}", configurator =>
                configurator.ConcurrentMessageLimit = 3);
            await OperationValueAsync(endpoint.Ready);
            facade = new AdmissionConnector(endpoint.ReceiveEndpoint, failureMode);
            var subject = new InstanceConnector<ThreeContractConsumer>();
            ConnectHandle? registration = null;
            Exception? outcome = Record.Exception(() =>
            {
                registration = generic
                    ? subject.ConnectInstance(facade, consumer, subject.CreateConsumerSpecification<ThreeContractConsumer>())
                    : subject.ConnectInstance(facade, (object)consumer);
            });

            Assert.Equal(3, facade.ConnectionCalls);
            Assert.True(new HashSet<Type>(facade.AttemptedTypes).SetEquals(
                new[] { typeof(FirstContract), typeof(SecondContract), typeof(ThirdContract) }));
            Assert.Equal(failureMode == 0 ? 3 : 2, facade.Acquired.Count);
            Assert.Equal(0, consumer.DisposeCalls);

            if (failureMode == 0)
            {
                Assert.Null(outcome);
                Assert.NotNull(registration);
                Assert.Null(facade.ThrownAdmission);
                Assert.Equal(3, facade.Registrations.Count);
                ISendEndpoint sendEndpoint = await OperationValueAsync(harness.Bus.GetSendEndpointAsync(
                    endpoint.ReceiveEndpoint.InputAddress, budget.Token));
                var first = new FirstContract(NewId.NextGuid());
                var second = new SecondContract(NewId.NextGuid());
                var third = new ThirdContract(NewId.NextGuid());
                Task firstSend = OperationAsync(sendEndpoint.SendAsync(first, budget.Token));
                Task secondSend = OperationAsync(sendEndpoint.SendAsync(second, budget.Token));
                Task thirdSend = OperationAsync(sendEndpoint.SendAsync(third, budget.Token));
                Assert.Equal(first, await ObserveValueAsync(consumer.First.Task));
                Assert.Equal(second, await ObserveValueAsync(consumer.Second.Task));
                Assert.Equal(third, await ObserveValueAsync(consumer.Third.Task));
                consumer.Release.TrySetResult(true);
                await OperationAsync(consumer.Release.Task);
                await firstSend;
                await secondSend;
                await thirdSend;
                registration.Disconnect();
                Assert.All(facade.Acquired, handle => Assert.Equal(1, handle.ReleaseAttempts));
                Assert.Equal(0, facade.Registrations.Count);
            }
            else
            {
                Assert.Null(registration);
                Assert.Same(facade.AdmissionFailure, facade.ThrownAdmission);
                Assert.NotNull(outcome);
                Assert.Equal(1, facade.Acquired[0].ReleaseAttempts);
                if (failureMode == 2)
                {
                    Assert.Same(facade.CleanupFailure, facade.Acquired[0].ThrownCleanup);
                    Assert.Equal(1, facade.Acquired[0].CleanupThrows);
                }
                else
                    Assert.Null(facade.Acquired[0].ThrownCleanup);

                // The initiating fault must not prevent cleanup of the second actual registration.
                Assert.Equal(1, facade.Acquired[1].ReleaseAttempts);
                Assert.Equal(0, facade.Registrations.Count);
                if (failureMode == 1)
                    Assert.Same(facade.AdmissionFailure, outcome);
                else
                {
                    Exception[] causes = ExceptionTree(outcome).ToArray();
                    Assert.Single(causes, cause => ReferenceEquals(cause, facade.AdmissionFailure));
                    Assert.Single(causes, cause => ReferenceEquals(cause, facade.CleanupFailure));
                }
            }

            await OperationAsync(endpoint.StopAsync(CancellationToken.None));
            await OperationAsync(harness.StopAsync(CancellationToken.None));
            Assert.Equal(failureMode == 0 ? 1 : 0, consumer.FirstCount);
            Assert.Equal(failureMode == 0 ? 1 : 0, consumer.SecondCount);
            Assert.Equal(failureMode == 0 ? 1 : 0, consumer.ThirdCount);
            Assert.Equal(0, consumer.DisposeCalls);
        }
        finally
        {
            consumer.Release.TrySetResult(true);
            try
            {
                budget.Cancel();
            }
            finally
            {
                try
                {
                    facade?.ReleaseAllWithoutInjectedFault();
                }
                finally
                {
                    try
                    {
                        await JoinAllAsync(started, budget.Token);
                    }
                    finally
                    {
                        try
                        {
                            if (endpoint is not null)
                            {
                                Task endpointStop = endpoint.StopAsync(CancellationToken.None);
                                await endpointStop.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
                            }
                        }
                        finally
                        {
                            try
                            {
                                Task harnessStop = harness.StopAsync(CancellationToken.None);
                                await harnessStop.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
                            }
                            finally
                            {
                                try
                                {
                                    Task harnessDispose = harness.DisposeAsync().AsTask();
                                    await harnessDispose.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
                                }
                                finally
                                {
                                    consumer.Dispose();
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    private static async Task JoinAllAsync(IEnumerable<StartedTask> started, CancellationToken observationToken)
    {
        var failures = new List<Exception>();
        foreach (StartedTask entry in started)
        {
            try
            {
                await entry.Task.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
            }
            catch (OperationCanceledException exception) when (entry.Observation && entry.Task.IsCanceled
                && observationToken.IsCancellationRequested && exception.CancellationToken == observationToken)
            {
                // Only fixture observation wrappers may be canceled by the fixture cleanup budget.
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }
        }
        if (failures.Count != 0)
            throw new AggregateException("Started instance fixture tasks failed to terminate successfully.", failures);
    }

    private static IEnumerable<Exception> ExceptionTree(Exception exception)
    {
        yield return exception;
        if (exception is AggregateException aggregate)
        {
            foreach (Exception aggregateInner in aggregate.InnerExceptions)
                foreach (Exception cause in ExceptionTree(aggregateInner))
                    yield return cause;
        }
        else if (exception.InnerException is Exception ordinaryInner)
            foreach (Exception cause in ExceptionTree(ordinaryInner))
                yield return cause;
    }

    private sealed record StartedTask(Task Task, bool Observation);

    private sealed class AdmissionConnector(IConsumePipeConnector actual, int failureMode) : IConsumePipeConnector
    {
        public readonly InvalidOperationException AdmissionFailure = new("unique instance third-contract admission failure");
        public readonly ApplicationException CleanupFailure = new("unique instance acquired-registration cleanup failure");
        public readonly Connectable<TrackedHandle> Registrations = new();
        public readonly List<TrackedHandle> Acquired = [];
        public readonly List<Type> AttemptedTypes = [];
        public int ConnectionCalls;
        public Exception? ThrownAdmission;

        public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe) where T : class =>
            Connect(() => actual.ConnectConsumePipe(pipe), typeof(T));

        public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe, ConnectPipeOptions options) where T : class =>
            Connect(() => actual.ConnectConsumePipe(pipe, options), typeof(T));

        private ConnectHandle Connect(Func<ConnectHandle> connect, Type messageType)
        {
            AttemptedTypes.Add(messageType);
            if (++ConnectionCalls == 3 && failureMode != 0)
            {
                ThrownAdmission = AdmissionFailure;
                throw AdmissionFailure;
            }

            ConnectHandle actualHandle = connect();
            var handle = new TrackedHandle(actualHandle, Acquired.Count == 0 && failureMode == 2 ? CleanupFailure : null);
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
                throw new AggregateException("Actual instance fixture registration release failed.", failures);
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

    private sealed class ThreeContractConsumer : IConsumer<FirstContract>, IConsumer<SecondContract>, IConsumer<ThirdContract>, IDisposable
    {
        public readonly TaskCompletionSource<FirstContract> First = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource<SecondContract> Second = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource<ThirdContract> Third = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource<bool> Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int FirstCount;
        public int SecondCount;
        public int ThirdCount;
        public int DisposeCalls;

        public Task ConsumeAsync(ConsumeContext<FirstContract> context)
        {
            Interlocked.Increment(ref FirstCount);
            First.TrySetResult(context.Message);
            return Release.Task;
        }

        public Task ConsumeAsync(ConsumeContext<SecondContract> context)
        {
            Interlocked.Increment(ref SecondCount);
            Second.TrySetResult(context.Message);
            return Release.Task;
        }

        public Task ConsumeAsync(ConsumeContext<ThirdContract> context)
        {
            Interlocked.Increment(ref ThirdCount);
            Third.TrySetResult(context.Message);
            return Release.Task;
        }

        public void Dispose() => Interlocked.Increment(ref DisposeCalls);
    }
}
