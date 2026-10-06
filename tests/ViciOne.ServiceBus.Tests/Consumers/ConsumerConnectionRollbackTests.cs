using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Util;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Consumers;

public sealed class ConsumerConnectionRollbackTests
{
    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 0)]
    [InlineData(false, 1)]
    [InlineData(true, 1)]
    [InlineData(false, 2)]
    [InlineData(true, 2)]
    [RequirementCoverage("REQ-VSB-CONSUMER-CONNECTION-ROLLBACK", "public-multi-message-admission-retires-all-returned-handles")]
    public void ConnectConsumer_FailedAdmissionRetiresEveryReturnedHandleWithoutLosingCauses(bool runtimeType, int mode)
    {
        var connector = new AdmissionConnector(mode);
        ConnectHandle? result = null;
        Exception? testFailure = null;
        List<Exception>? cleanupFailures = null;
        var factoryCalls = 0;

        object CreateConsumer(Type type)
        {
            factoryCalls++;
            if (type != typeof(ThreeMessageConsumer))
                throw new InvalidOperationException("Unexpected requested consumer type.");
            return new ThreeMessageConsumer();
        }

        try
        {
            Exception? admissionFailure = Record.Exception(() =>
            {
                result = runtimeType
                    ? connector.ConnectConsumer(typeof(ThreeMessageConsumer), CreateConsumer)
                    : connector.ConnectConsumer<ThreeMessageConsumer>();
            });

            Assert.Equal(3, connector.Calls.Count);
            Assert.Equal(
                new[] { typeof(FirstMessage), typeof(SecondMessage), typeof(ThirdMessage) }.OrderBy(type => type.FullName),
                connector.Calls.Select(call => call.MessageType).OrderBy(type => type.FullName));
            Assert.All(connector.Calls, call =>
            {
                Assert.NotNull(call.Pipe);
                Assert.False(call.ExplicitOptions);
            });
            Assert.Equal(0, factoryCalls);
            Assert.Equal(mode == 0 ? 3 : 2, connector.Handles.Count);
            Assert.Equal(
                connector.Calls.Take(connector.Handles.Count).Select(call => call.MessageType),
                connector.Handles.Select(handle => handle.Registration.MessageType));

            if (mode == 0)
            {
                Assert.Null(admissionFailure);
                ConnectHandle composite = Assert.IsAssignableFrom<MultipleConnectHandle>(result);
                Assert.Equal(3, connector.Connections.Count);
                Assert.All(connector.Handles, handle => Assert.Equal(0, handle.ReleaseCalls));

                composite.Dispose();

                Assert.Equal(0, connector.Connections.Count);
                Assert.All(connector.Handles, handle =>
                {
                    Assert.Equal(1, handle.ReleaseCalls);
                    Assert.Equal(1, handle.DisconnectCalls);
                    Assert.Equal(0, handle.DisposeCalls);
                    Assert.True(handle.ActualRegistrationRemoved);
                });
            }
            else
            {
                Assert.Null(result);
                Assert.NotNull(admissionFailure);
                if (admissionFailure is not AggregateException)
                    Assert.Same(mode == 2 ? connector.CleanupFailure : connector.AdmissionFailure, admissionFailure);

                TrackedHandle first = connector.Handles[0];
                Assert.Equal(1, first.DisposeCalls);
                Assert.Equal(1, first.ReleaseCalls);
                Assert.True(first.ActualRegistrationRemoved);

                // This is the first later-handle rollback oracle, before cause-shape assertions.
                Assert.Equal(1, connector.Handles[1].DisposeCalls);
                Assert.Equal(0, connector.Connections.Count);
                Assert.All(connector.Handles, handle =>
                {
                    Assert.Equal(1, handle.ReleaseCalls);
                    Assert.Equal(0, handle.DisconnectCalls);
                    Assert.True(handle.ActualRegistrationRemoved);
                });

                if (mode == 1)
                    Assert.Same(connector.AdmissionFailure, admissionFailure);
                else
                {
                    AggregateException aggregate = Assert.IsType<AggregateException>(admissionFailure);
                    Assert.Equal(2, aggregate.InnerExceptions.Count);
                    Assert.Same(connector.AdmissionFailure, aggregate.InnerExceptions[0]);
                    Assert.Same(connector.CleanupFailure, aggregate.InnerExceptions[1]);
                }
            }
        }
        catch (Exception failure)
        {
            testFailure = failure;
        }
        finally
        {
            foreach (TrackedHandle handle in connector.Handles)
            {
                handle.FailureEnabled = false;
                try
                {
                    handle.Dispose();
                }
                catch (Exception failure)
                {
                    (cleanupFailures ??= []).Add(failure);
                }
            }
        }

        if (testFailure is not null)
        {
            if (cleanupFailures is null)
                ExceptionDispatchInfo.Capture(testFailure).Throw();
            else
                cleanupFailures.Insert(0, testFailure);
        }
        if (cleanupFailures is { Count: 1 })
            ExceptionDispatchInfo.Capture(cleanupFailures[0]).Throw();
        if (cleanupFailures is { Count: > 1 })
            throw new AggregateException("Consumer registration test and cleanup failed.", cleanupFailures);
    }

    private sealed record Registration(Type MessageType, object Pipe, bool ExplicitOptions);

    private sealed class AdmissionConnector(int mode) : IConsumePipeConnector
    {
        public InvalidOperationException AdmissionFailure { get; } = new("Third consumer admission failed.");
        public IOException CleanupFailure { get; } = new("First acquired consumer registration cleanup failed.");
        public Connectable<Registration> Connections { get; } = new();
        public List<Registration> Calls { get; } = [];
        public List<TrackedHandle> Handles { get; } = [];

        public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe) where T : class =>
            Connect(pipe, false);

        public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe, ConnectPipeOptions options) where T : class =>
            Connect(pipe, true);

        private ConnectHandle Connect<T>(IPipe<ConsumeContext<T>> pipe, bool explicitOptions) where T : class
        {
            var registration = new Registration(typeof(T), pipe, explicitOptions);
            Calls.Add(registration);
            if (mode != 0 && Calls.Count == 3)
                throw AdmissionFailure;

            ConnectHandle actualHandle = Connections.Connect(registration);
            var handle = new TrackedHandle(registration, actualHandle,
                mode == 2 && Handles.Count == 0 ? CleanupFailure : null);
            Handles.Add(handle);
            return handle;
        }
    }

    private sealed class TrackedHandle(Registration registration, ConnectHandle actualHandle, Exception? failure) : ConnectHandle
    {
        public Registration Registration { get; } = registration;
        public int ReleaseCalls { get; private set; }
        public int DisposeCalls { get; private set; }
        public int DisconnectCalls { get; private set; }
        public bool ActualRegistrationRemoved { get; private set; }
        public bool FailureEnabled { get; set; } = true;

        public void Disconnect()
        {
            DisconnectCalls++;
            Release();
        }

        public void Dispose()
        {
            DisposeCalls++;
            Release();
        }

        private void Release()
        {
            ReleaseCalls++;
            actualHandle.Dispose();
            ActualRegistrationRemoved = true;
            if (FailureEnabled && failure is not null)
                throw failure;
        }
    }

    public sealed record FirstMessage(Guid MessageId);
    public sealed record SecondMessage(Guid MessageId);
    public sealed record ThirdMessage(Guid MessageId);

    public sealed class ThreeMessageConsumer : IConsumer<FirstMessage>, IConsumer<SecondMessage>, IConsumer<ThirdMessage>
    {
        public Task ConsumeAsync(ConsumeContext<FirstMessage> context) => Task.CompletedTask;
        public Task ConsumeAsync(ConsumeContext<SecondMessage> context) => Task.CompletedTask;
        public Task ConsumeAsync(ConsumeContext<ThirdMessage> context) => Task.CompletedTask;
    }
}
