using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Serialization;

public sealed class ArrayMessageTypeTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ARRAY-MESSAGE-PUBLICATION", "single-array-message")]
    public async Task PublicOneDimensionalArray_IsDeliveredAsOneOrderedMessageAsync()
    {
        TimeSpan operationTimeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        using var harness = new InMemoryTestHarness($"array-message-{NewId.NextGuid():N}")
        {
            TestTimeout = operationTimeout,
        };
        harness.BeginTestScope();
        var receivedMessage = new TaskCompletionSource<ConsumeContext<ArrayMessageItem[]>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        harness.OnConfigureInMemoryReceiveEndpoint += configurator =>
            configurator.Handler<ArrayMessageItem[]>(context =>
            {
                receivedMessage.TrySetResult(context);
                return Task.CompletedTask;
            });
        var cancellationToken = TestContext.Current.CancellationToken;

        try
        {
            await harness.StartAsync(cancellationToken);
            ArrayMessageItem[] message =
            [
                new(1, "first"),
                new(2, "second"),
                new(3, "third"),
            ];

            await harness.Bus.PublishAsync(message, cancellationToken);

            ConsumeContext<ArrayMessageItem[]> context = await receivedMessage.Task.WaitAsync(
                harness.TestTimeout,
                cancellationToken);
            ArrayMessageItem[] received = context.Message;

            Assert.Collection(
                received,
                item => Assert.Equal(new ArrayMessageItem(1, "first"), item),
                item => Assert.Equal(new ArrayMessageItem(2, "second"), item),
                item => Assert.Equal(new ArrayMessageItem(3, "third"), item));
            Assert.True(await harness.Consumed.AnyAsync<ArrayMessageItem[]>(cancellationToken));
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }
}

public sealed record ArrayMessageItem(int Sequence, string Value);
