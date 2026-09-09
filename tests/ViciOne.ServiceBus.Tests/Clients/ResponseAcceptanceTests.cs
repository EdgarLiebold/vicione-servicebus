using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Clients;

public sealed class ResponseAcceptanceTests
{
    [Theory]
    [InlineData(AcceptanceShape.NoResponseAddress, false)]
    [InlineData(AcceptanceShape.NoAcceptHeader, false)]
    [InlineData(AcceptanceShape.ExactAcceptedType, true)]
    [InlineData(AcceptanceShape.CaseChangedAcceptedType, false)]
    [InlineData(AcceptanceShape.DifferentAcceptedType, false)]
    [RequirementCoverage("REQ-VSB-RESPONSE-ACCEPTANCE", "response-address-header-and-type-matching")]
    public async Task ResponseAcceptance_RequiresAnAddressAndAnExactDeclaredContractAsync(
        AcceptanceShape shape,
        bool expected)
    {
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new InMemoryTestHarness($"response-acceptance-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        var observed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        harness.Handler<AcceptanceProbe>(context =>
        {
            observed.TrySetResult(context.Advanced().IsResponseAccepted<AcceptedResponse>());
            return Task.CompletedTask;
        });

        await harness.StartAsync(cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.SendAsync(
                new AcceptanceProbe(shape),
                context => Configure(context, shape, harness.Bus.Address),
                cancellationToken);

            Assert.Equal(expected, await observed.Task.WaitAsync(timeout, cancellationToken));
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static void Configure(
        SendContext<AcceptanceProbe> context,
        AcceptanceShape shape,
        Uri responseAddress)
    {
        if (shape == AcceptanceShape.NoResponseAddress)
            return;

        context.ResponseAddress = responseAddress;

        if (shape == AcceptanceShape.ExactAcceptedType)
        {
            context.Headers.Set(
                MessageHeaders.Request.Accept,
                new[] { MessageUrn.ForTypeString<AcceptedResponse>() });
        }
        else if (shape == AcceptanceShape.CaseChangedAcceptedType)
        {
            context.Headers.Set(
                MessageHeaders.Request.Accept,
                new[] { MessageUrn.ForTypeString<AcceptedResponse>().ToUpperInvariant() });
        }
        else if (shape == AcceptanceShape.DifferentAcceptedType)
        {
            context.Headers.Set(
                MessageHeaders.Request.Accept,
                new[] { MessageUrn.ForTypeString<OtherResponse>() });
        }
    }

    public enum AcceptanceShape
    {
        NoResponseAddress,
        NoAcceptHeader,
        ExactAcceptedType,
        CaseChangedAcceptedType,
        DifferentAcceptedType,
    }

    private sealed record AcceptanceProbe(AcceptanceShape Shape);

    private sealed record AcceptedResponse(string Value);

    private sealed record OtherResponse(string Value);
}
