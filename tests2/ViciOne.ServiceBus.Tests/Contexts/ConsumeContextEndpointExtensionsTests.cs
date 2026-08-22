using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Contexts;

public sealed class ConsumeContextEndpointExtensionsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-POLYMORPHIC-FAULT-PUBLICATION", "derived-interface-to-base-fault")]
    public async Task ThrownDerivedInterfaceMessage_PublishesAConsumableBaseFault()
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
            await harness.Start(cancellationToken);
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
                await harness.InputQueueSendEndpoint.Send<UpdateMemberAddressCommand>(
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
                Assert.True(await harness.Published.Any<Fault<UpdateMemberAddressCommand>>(cancellationToken));
            }
            finally
            {
                await faultEndpoint.StopAsync(cancellationToken);
            }
        }
        finally
        {
            await harness.Stop();
        }
    }

    private sealed class ExpectedHandlerException : Exception;
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
