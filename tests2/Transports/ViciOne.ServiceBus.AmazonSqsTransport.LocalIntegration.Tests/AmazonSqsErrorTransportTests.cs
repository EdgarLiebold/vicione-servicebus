namespace ViciOne.ServiceBus.AmazonSqsTransport.LocalIntegration.Tests;

using System.Runtime.Serialization;
using ViciOne.ServiceBus.AmazonSqsTransport.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

public sealed class AmazonSqsErrorTransportTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-ERROR-TRANSPORT", "complete-sanitized-fault-envelope-moves-once")]
    public async Task SerializationFault_MovesOneCompleteSanitizedEnvelope()
    {
        await using AmazonSqsLocalStack fixture = AmazonSqsLocalStack.Create("errortransport");
        string inputQueue = fixture.Name("input");
        string errorQueue = $"{inputQueue}_error";
        Guid correlationId = Guid.NewGuid();
        var moved = new TaskCompletionSource<ErrorObservation>(TaskCreationOptions.RunContinuationsAsynchronously);
        IBusControl bus = Bus.Factory.CreateUsingAmazonSqs(configurator =>
        {
            fixture.ConfigureHost(configurator, host =>
                host.AllowTransportHeader(header => header.Key != "Estelle"));
            configurator.ReceiveEndpoint(inputQueue, endpoint =>
            {
                endpoint.ConfigureConsumeTopology = false;
                endpoint.Durable = false;
                endpoint.AutoDelete = true;
                endpoint.Handler<FaultingMessage>(_ =>
                    throw new SerializationException(IntentionalFailureMessage));
            });
            configurator.ReceiveEndpoint(errorQueue, endpoint =>
            {
                endpoint.ConfigureConsumeTopology = false;
                endpoint.Durable = false;
                endpoint.AutoDelete = true;
                endpoint.Handler<FaultingMessage>(context =>
                {
                    moved.TrySetResult(new ErrorObservation(
                        context.CorrelationId,
                        context.SourceAddress,
                        context.DestinationAddress,
                        context.ResponseAddress,
                        context.FaultAddress,
                        context.ReceiveContext.TransportHeaders.Get(MessageHeaders.FaultMessage, (string?)null),
                        context.ReceiveContext.TransportHeaders.Get(MessageHeaders.Reason, (string?)null),
                        context.ReceiveContext.TransportHeaders.Get(MessageHeaders.FaultInputAddress, (Uri?)null),
                        context.ReceiveContext.TransportHeaders.Get("Frank", (string?)null),
                        context.ReceiveContext.TransportHeaders.Get("Estelle", (string?)null),
                        context.ReceiveContext.TransportHeaders.Get(MessageHeaders.FaultExceptionType, (string?)null),
                        context.ReceiveContext.TransportHeaders.Get(MessageHeaders.Host.MachineName, (string?)null)));
                    return Task.CompletedTask;
                });
            });
        });
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Uri? inputAddress = null;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            ISendEndpoint input = await bus.GetSendEndpoint(new Uri($"queue:{inputQueue}"))
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            await input.Send(
                    new FaultingMessage(correlationId),
                    context =>
                    {
                        context.CorrelationId = correlationId;
                        inputAddress = context.DestinationAddress;
                        context.ResponseAddress = bus.Address;
                        context.FaultAddress = bus.Address;
                        context.Headers.Set("Frank", "Happy");
                        context.Headers.Set("Estelle", "Sad");
                    },
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            ErrorObservation actual = await moved.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            Uri expectedInputAddress = Assert.IsType<Uri>(inputAddress);
            var expectedFaultInputAddress = new Uri(
                $"amazonsqs://{fixture.Region}/{fixture.Prefix}/{inputQueue}?durable=false&autodelete=true");

            Assert.Equal(correlationId, actual.CorrelationId);
            Assert.Equal(bus.Address, actual.SourceAddress);
            Assert.Equal(expectedInputAddress, actual.DestinationAddress);
            Assert.Equal(bus.Address, actual.ResponseAddress);
            Assert.Equal(bus.Address, actual.FaultAddress);
            Assert.Equal(IntentionalFailureMessage, actual.FaultMessage);
            Assert.Equal("fault", actual.Reason);
            Assert.Equal(expectedFaultInputAddress, actual.FaultInputAddress);
            Assert.Equal("Happy", actual.AllowedHeader);
            Assert.Null(actual.DeniedHeader);
            Assert.Null(actual.FaultExceptionType);
            Assert.Null(actual.HostMachineName);
        }
        finally
        {
            if (started)
                await bus.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
        }
    }

    private const string IntentionalFailureMessage = "Intentional serialization failure.";

    private sealed record FaultingMessage(Guid CorrelationId);

    private sealed record ErrorObservation(
        Guid? CorrelationId,
        Uri? SourceAddress,
        Uri? DestinationAddress,
        Uri? ResponseAddress,
        Uri? FaultAddress,
        string? FaultMessage,
        string? Reason,
        Uri? FaultInputAddress,
        string? AllowedHeader,
        string? DeniedHeader,
        string? FaultExceptionType,
        string? HostMachineName);
}
