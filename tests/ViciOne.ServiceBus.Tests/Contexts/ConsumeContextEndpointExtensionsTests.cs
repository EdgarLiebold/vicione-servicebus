using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Contexts;

public sealed class ConsumeContextEndpointExtensionsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-POLYMORPHIC-FAULT-PUBLICATION", "derived-interface-to-base-fault")]
    public async Task ThrownDerivedInterfaceMessage_PublishesAConsumableBaseFaultAsync()
    {
        TimeSpan operationTimeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        using var harness = new InMemoryTestHarness($"fault-publishing-{NewId.NextGuid():N}")
        {
            TestTimeout = operationTimeout,
        };
        harness.BeginTestScope();
        harness.OnConfigureInMemoryReceiveEndpoint += configurator =>
            configurator.Handler<UpdateMemberAddressCommand>(
                _ => Task.FromException(new ExpectedHandlerException()));
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        try
        {
            await harness.StartAsync(cancellationToken);
            var receivedBaseFault = new TaskCompletionSource<ConsumeContext<Fault<MemberUpdateCommand>>>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            HostReceiveEndpointHandle faultEndpoint = harness.Bus.ConnectReceiveEndpoint(configurator =>
                configurator.Handler<Fault<MemberUpdateCommand>>(context =>
                {
                    receivedBaseFault.TrySetResult(context);
                    return Task.CompletedTask;
                }));
            await faultEndpoint.Ready.WaitAsync(harness.TestTimeout, cancellationToken);

            try
            {
                await harness.InputQueueSendEndpoint.SendAsync<UpdateMemberAddressCommand>(
                    new
                    {
                        MemberName = "Frank",
                        Address = "123 American Way",
                    },
                    cancellationToken);

                ConsumeContext<Fault<MemberUpdateCommand>> context = await receivedBaseFault.Task.WaitAsync(
                    harness.TestTimeout,
                    cancellationToken);

                Assert.Equal("Frank", context.Message.Message.MemberName);
                Assert.True(context.TryGetMessage(out ConsumeContext<Fault<UpdateMemberAddressCommand>>? derivedFault));
                Assert.Equal("Frank", derivedFault.Message.Message.MemberName);
                Assert.Equal("123 American Way", derivedFault.Message.Message.Address);
                Assert.NotEqual(Guid.Empty, context.Message.FaultId);
                Assert.NotNull(context.Message.Host);
                Assert.Contains(
                    context.Message.Exceptions,
                    exception => exception.ExceptionType == TypeCache<ExpectedHandlerException>.ShortName);
                Assert.Equal(
                    new[]
                    {
                        MessageUrn.ForTypeString<ApplicationCommand>(),
                        MessageUrn.ForTypeString<MemberUpdateCommand>(),
                        MessageUrn.ForTypeString<UpdateMemberAddressCommand>(),
                    }.Order(StringComparer.Ordinal),
                    context.Message.FaultMessageTypes.Order(StringComparer.Ordinal));
                Assert.True(await harness.Published.AnyAsync<Fault<UpdateMemberAddressCommand>>(cancellationToken));
            }
            finally
            {
                await faultEndpoint.StopAsync(cancellationToken);
            }
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FAULT-NOTIFICATION", "derived-context-from-base-context")]
    public async Task DerivedContextObtainedFromBaseContext_PublishesOneCompleteFaultAsync()
    {
        TimeSpan operationTimeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        using var harness = new InMemoryTestHarness($"try-get-fault-{NewId.NextGuid():N}")
        {
            TestTimeout = operationTimeout,
            TestInactivityTimeout = operationTimeout,
        };
        harness.BeginTestScope();
        var derivedContextObserved = new TaskCompletionSource<ConsumeContext<DerivedFaultCommand>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        harness.OnConfigureInMemoryReceiveEndpoint += configurator =>
            configurator.Handler<BaseFaultCommand>(context =>
            {
                if (!context.TryGetMessage(out ConsumeContext<DerivedFaultCommand>? derivedContext))
                    throw new InvalidOperationException("The derived consume context was not available.");

                derivedContextObserved.TrySetResult(derivedContext);
                return derivedContext.NotifyFaultedAsync(
                    TimeSpan.Zero,
                    TypeCache<ConsumeContextEndpointExtensionsTests>.ShortName,
                    new ExpectedDerivedFaultException());
            });
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;
        HostReceiveEndpointHandle? faultEndpoint = null;

        try
        {
            await harness.StartAsync(cancellationToken);
            started = true;
            var receivedBaseFault = new TaskCompletionSource<ConsumeContext<Fault<BaseFaultCommand>>>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            faultEndpoint = harness.Bus.ConnectReceiveEndpoint(configurator =>
                configurator.Handler<Fault<BaseFaultCommand>>(context =>
                {
                    receivedBaseFault.TrySetResult(context);
                    return Task.CompletedTask;
                }));
            await faultEndpoint.Ready.WaitAsync(operationTimeout, cancellationToken);

            Guid commandId = Guid.Parse("856dd4c5-58c8-4530-8afc-58b7f35d274b");
            await harness.InputQueueSendEndpoint.SendAsync<DerivedFaultCommand>(
                new { CommandId = commandId, Value = "fault me" },
                cancellationToken);

            ConsumeContext<DerivedFaultCommand> derivedContext = await derivedContextObserved.Task.WaitAsync(
                operationTimeout,
                cancellationToken);
            ConsumeContext<Fault<BaseFaultCommand>> baseFault = await receivedBaseFault.Task.WaitAsync(
                operationTimeout,
                cancellationToken);
            Assert.True(baseFault.TryGetMessage(
                out ConsumeContext<Fault<DerivedFaultCommand>>? derivedFault));

            await faultEndpoint.StopAsync(CancellationToken.None);
            faultEndpoint = null;
            await harness.StopAsync(TestContext.Current.CancellationToken);
            started = false;

            using var completed = new CancellationTokenSource();
            completed.Cancel();
            Assert.Single(harness.Published.Select<Fault<BaseFaultCommand>>(completed.Token));
            Assert.Single(harness.Published.Select<Fault<DerivedFaultCommand>>(completed.Token));
            Assert.Equal(commandId, derivedContext.Message.CommandId);
            Assert.Equal("fault me", derivedContext.Message.Value);
            Assert.Equal(commandId, baseFault.Message.Message.CommandId);
            Assert.Equal(commandId, derivedFault.Message.Message.CommandId);
            Assert.Equal(
                new[]
                {
                    MessageUrn.ForTypeString<BaseFaultCommand>(),
                    MessageUrn.ForTypeString<DerivedFaultCommand>(),
                }.Order(StringComparer.Ordinal),
                baseFault.Message.FaultMessageTypes.Order(StringComparer.Ordinal));
            Assert.Contains(
                baseFault.Message.Exceptions,
                exception => exception.ExceptionType == TypeCache<ExpectedDerivedFaultException>.ShortName);
            Assert.Contains(
                MessageUrn.ForTypeString<Fault<BaseFaultCommand>>(),
                baseFault.Advanced().SupportedMessageTypes);
            Assert.Contains(
                MessageUrn.ForTypeString<Fault<DerivedFaultCommand>>(),
                baseFault.Advanced().SupportedMessageTypes);
        }
        finally
        {
            if (faultEndpoint is not null)
                await faultEndpoint.StopAsync(CancellationToken.None);
            if (started)
                await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    private sealed class ExpectedHandlerException : Exception;

    private sealed class ExpectedDerivedFaultException : Exception;
}

[ExcludeFromTopology]
public interface ApplicationCommand;

public interface MemberUpdateCommand : ApplicationCommand
{
    string MemberName { get; }
}

public interface UpdateMemberAddressCommand : MemberUpdateCommand
{
    string Address { get; }
}

public interface BaseFaultCommand
{
    Guid CommandId { get; }
}

public interface DerivedFaultCommand : BaseFaultCommand
{
    string Value { get; }
}
