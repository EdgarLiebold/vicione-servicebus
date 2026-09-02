using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Clients;

public sealed class ResponseAcceptanceTests
{
    [Theory]
    [InlineData(AcceptanceShape.NoResponseAddress, true, false)]
    [InlineData(AcceptanceShape.NoAcceptHeader, true, true)]
    [InlineData(AcceptanceShape.NoAcceptHeader, false, false)]
    [InlineData(AcceptanceShape.CaseChangedAcceptedType, false, true)]
    [InlineData(AcceptanceShape.DifferentAcceptedType, true, false)]
    [RequirementCoverage("REQ-VSB-RESPONSE-ACCEPTANCE", "response-address-header-and-type-matching")]
    public async Task ResponseAcceptance_RequiresAnAddressAndHonorsHeaderFallbackAndOrdinalCaseInsensitivity(
        AcceptanceShape shape,
        bool defaultIfHeaderNotFound,
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
            observed.TrySetResult(context.IsResponseAccepted<AcceptedResponse>(defaultIfHeaderNotFound));
            return Task.CompletedTask;
        });

        await harness.Start(cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.Send(
                new AcceptanceProbe(shape),
                context => Configure(context, shape, harness.Bus.Address),
                cancellationToken);

            Assert.Equal(expected, await observed.Task.WaitAsync(timeout, cancellationToken));
        }
        finally
        {
            await harness.Stop().WaitAsync(timeout, CancellationToken.None);
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

        if (shape == AcceptanceShape.CaseChangedAcceptedType)
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
        CaseChangedAcceptedType,
        DifferentAcceptedType,
    }

    private sealed record AcceptanceProbe(AcceptanceShape Shape);

    private sealed record AcceptedResponse(string Value);

    private sealed record OtherResponse(string Value);
}
