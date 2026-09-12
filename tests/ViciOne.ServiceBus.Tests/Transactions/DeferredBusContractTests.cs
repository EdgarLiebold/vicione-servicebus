using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.Transactions;
using ViciOne.ServiceBus.Transactions;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Transactions;

public sealed class DeferredBusContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-DEFERRED-BUS-API", "publish-overloads-reject-invalid-input-before-buffering")]
    public async Task PublishOverloads_RejectInvalidInputBeforeAnythingIsBufferedAsync()
    {
        var driver = new BufferedBusTestDriver();
        IBufferedBus bus = driver.Bus;
        CancellationToken token = TestContext.Current.CancellationToken;
        IPipe<PublishContext<DeferredMessage>> typedPipe = Pipe.Empty<PublishContext<DeferredMessage>>();
        IPipe<PublishContext> pipe = Pipe.Empty<PublishContext>();

        AssertParameter("message", () => bus.PublishAsync<DeferredMessage>(null!, token));
        AssertParameter("message", () => bus.PublishAsync<DeferredMessage>(null!, typedPipe, token));
        AssertParameter("pipe", () => bus.PublishAsync(new DeferredMessage(), (IPipe<PublishContext<DeferredMessage>>)null!, token));
        AssertParameter("message", () => bus.PublishAsync<DeferredMessage>(null!, pipe, token));
        AssertParameter("pipe", () => bus.PublishAsync(new DeferredMessage(), (IPipe<PublishContext>)null!, token));
        AssertParameter("message", () => bus.Advanced().PublishAsync(null!, token));
        AssertParameter("message", () => bus.Advanced().PublishAsync(null!, pipe, token));
        AssertParameter("publishPipe", () => bus.Advanced().PublishAsync(
            new DeferredMessage(),
            (IPipe<PublishContext>)null!,
            token));
        AssertParameter("message", () => bus.Advanced().PublishAsync(null!, typeof(DeferredMessage), token));
        AssertParameter("messageType", () => bus.Advanced().PublishAsync(new DeferredMessage(), (Type)null!, token));
        AssertParameter("message", () => bus.Advanced().PublishAsync(null!, typeof(DeferredMessage), pipe, token));
        AssertParameter("messageType", () => bus.Advanced().PublishAsync(new DeferredMessage(), null!, pipe, token));
        AssertParameter("publishPipe", () => bus.Advanced().PublishAsync(new DeferredMessage(), typeof(DeferredMessage), null!, token));
        AssertParameter("values", () => bus.PublishAsync<DeferredMessage>((object)null!, token));
        AssertParameter("values", () => bus.PublishAsync<DeferredMessage>((object)null!, typedPipe, token));
        AssertParameter("pipe", () => bus.PublishAsync<DeferredMessage>(new { Value = "valid" }, (IPipe<PublishContext<DeferredMessage>>)null!, token));
        AssertParameter("values", () => bus.PublishAsync<DeferredMessage>((object)null!, pipe, token));
        AssertParameter("pipe", () => bus.PublishAsync<DeferredMessage>(new { Value = "valid" }, (IPipe<PublishContext>)null!, token));

        await bus.FlushAsync(token);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DEFERRED-BUS-API", "send-overloads-reject-invalid-input-before-buffering")]
    public async Task SendOverloads_RejectInvalidInputBeforeAnythingIsBufferedAsync()
    {
        var driver = new BufferedBusTestDriver();
        ITransportSendEndpoint endpoint = driver.CreateSendEndpoint();
        CancellationToken token = TestContext.Current.CancellationToken;
        IPipe<SendContext<DeferredMessage>> typedPipe = Pipe.Empty<SendContext<DeferredMessage>>();
        IPipe<SendContext> pipe = Pipe.Empty<SendContext>();
        DeferredMessage missingMessage = null!;

        AssertParameter("message", () => endpoint.CreateSendContextAsync<DeferredMessage>(null!, typedPipe, token));
        AssertParameter("pipe", () => endpoint.CreateSendContextAsync(new DeferredMessage(), null!, token));
        AssertParameter("message", () => endpoint.SendAsync(missingMessage, token));
        AssertParameter("message", () => endpoint.SendAsync(missingMessage, typedPipe, token));
        AssertParameter("pipe", () => endpoint.SendAsync(new DeferredMessage(), (IPipe<SendContext<DeferredMessage>>)null!, token));
        AssertParameter("message", () => endpoint.SendAsync(missingMessage, pipe, token));
        AssertParameter("pipe", () => endpoint.SendAsync(new DeferredMessage(), (IPipe<SendContext>)null!, token));
        AssertParameter("message", () => endpoint.SendAsync((object)null!, token));
        AssertParameter("message", () => endpoint.SendAsync(null!, typeof(DeferredMessage), token));
        AssertParameter("messageType", () => endpoint.SendAsync(new DeferredMessage(), (Type)null!, token));
        AssertParameter("message", () => endpoint.SendAsync(null!, pipe, token));
        AssertParameter("pipe", () => endpoint.SendAsync(new DeferredMessage(), (IPipe<SendContext>)null!, token));
        AssertParameter("message", () => endpoint.SendAsync(null!, typeof(DeferredMessage), pipe, token));
        AssertParameter("messageType", () => endpoint.SendAsync(new DeferredMessage(), null!, pipe, token));
        AssertParameter("pipe", () => endpoint.SendAsync(new DeferredMessage(), typeof(DeferredMessage), null!, token));
        AssertParameter("values", () => endpoint.SendAsync<DeferredMessage>((object)null!, token));
        AssertParameter("values", () => endpoint.SendAsync<DeferredMessage>((object)null!, typedPipe, token));
        AssertParameter("pipe", () => endpoint.SendAsync<DeferredMessage>(new { Value = "valid" }, (IPipe<SendContext<DeferredMessage>>)null!, token));
        AssertParameter("values", () => endpoint.SendAsync<DeferredMessage>((object)null!, pipe, token));
        AssertParameter("pipe", () => endpoint.SendAsync<DeferredMessage>(new { Value = "valid" }, (IPipe<SendContext>)null!, token));

        await driver.Bus.FlushAsync(token);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DEFERRED-BUS-API", "send-endpoint-construction-boundaries")]
    public void SendEndpointAdapter_RejectsMissingAndNonTransportEndpoints()
    {
        var driver = new BufferedBusTestDriver();

        AssertParameter("endpoint", () => driver.WrapSendEndpoint(null!));
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            driver.WrapSendEndpoint(BufferedBusTestDriver.CreateNonTransportSendEndpoint()));

        Assert.Equal("endpoint", exception.ParamName);
        Assert.Contains("transport send operations", exception.Message, StringComparison.Ordinal);
    }

    private static void AssertParameter(string expected, Func<object?> action)
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => action());
        Assert.Equal(expected, exception.ParamName);
    }

    private sealed record DeferredMessage(string Value = "value");
}
