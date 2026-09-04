using System.Net.Mime;
using System.Runtime.Serialization;
using System.Text.Json;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Serialization;

public sealed class SerializationFaultTests
{
    private const string ConsumerFailureMessage = "The request handler rejected the serialized message.";
    private const string NotAnEnvelope = "<not-an-envelope/>";

    private static readonly ContentType UnsupportedContentType =
        new("application/vnd.vicione.servicebus.unregistered");

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-FAULT", "serialization-exception")]
    public async Task ConsumerSerializationException_ReachesTheRequestCallerAsAnExactFaultAsync()
    {
        TimeSpan operationTimeout = GetOperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness("request-serialization-fault", operationTimeout);
        harness.OnConfigureInMemoryReceiveEndpoint += configurator =>
            configurator.Handler<SerializationFailureRequest>(
                _ => Task.FromException(new SerializationException(ConsumerFailureMessage)));

        try
        {
            await harness.StartAsync(cancellationToken).WaitAsync(operationTimeout, cancellationToken);
            IRequestClient<SerializationFailureRequest> client =
                harness.CreateRequestClient<SerializationFailureRequest>();
            var request = new SerializationFailureRequest(
                Guid.Parse("67f2aecd-48f4-4a3b-b6cf-d6113780965c"));
            Guid requestMessageId = Guid.Parse("820b32b5-75cb-4a70-a90e-f63554896878");

            RequestFaultException exception = await Assert.ThrowsAsync<RequestFaultException>(() =>
                client.Advanced().GetResponseAsync<SerializationFailureResponse>(
                    request,
                    callback: configurator => configurator.UseExecute(context => context.MessageId = requestMessageId),
                    cancellationToken: cancellationToken));

            Fault fault = Assert.IsAssignableFrom<Fault>(exception.Fault);
            Fault<SerializationFailureRequest> typedFault =
                Assert.IsAssignableFrom<Fault<SerializationFailureRequest>>(fault);
            ExceptionInfo faultException = Assert.Single(fault.Exceptions);

            Assert.Equal(TypeCache<SerializationFailureRequest>.ShortName, exception.RequestType);
            Assert.NotEqual(Guid.Empty, fault.FaultId);
            Assert.Equal(requestMessageId, fault.FaultedMessageId);
            Assert.NotNull(fault.Host);
            Assert.Equal(request, typedFault.Message);
            Assert.Contains(
                MessageUrn.ForTypeString<SerializationFailureRequest>(),
                fault.FaultMessageTypes,
                StringComparer.Ordinal);
            Assert.Equal(TypeCache<SerializationException>.ShortName, faultException.ExceptionType);
            Assert.Equal(ConsumerFailureMessage, faultException.Message);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(operationTimeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECEIVE-FAULT", "unsupported-content-type")]
    public async Task UnsupportedUnreadableBody_PublishesAnExactReceiveFaultWithoutDispatchingAsync()
    {
        TimeSpan operationTimeout = GetOperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness("unsupported-body-fault", operationTimeout);
        var consumed = new TaskCompletionSource<ConsumeContext<UnreadableMessage>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var faulted = new TaskCompletionSource<ConsumeContext<ReceiveFault>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        harness.OnConfigureInMemoryReceiveEndpoint += configurator =>
        {
            configurator.Handler<UnreadableMessage>(context =>
            {
                consumed.TrySetResult(context);
                return Task.CompletedTask;
            });
            configurator.Handler<ReceiveFault>(context =>
            {
                faulted.TrySetResult(context);
                return Task.CompletedTask;
            });
        };
        Guid messageId = Guid.Parse("831751ec-b5b0-44ba-a31f-0a9019f45536");

        try
        {
            await harness.StartAsync(cancellationToken).WaitAsync(operationTimeout, cancellationToken);
            await harness.InputQueueSendEndpoint.SendAsync<UnreadableMessage>(
                    new { Value = "must-not-be-dispatched" },
                    context =>
                    {
                        context.MessageId = messageId;
                        context.Serializer = new CopyBodySerializer(
                            UnsupportedContentType,
                            new StringMessageBody(NotAnEnvelope));
                    },
                    cancellationToken)
                .WaitAsync(operationTimeout, cancellationToken);

            ConsumeContext<ReceiveFault> faultContext = await faulted.Task.WaitAsync(
                operationTimeout,
                cancellationToken);

            AssertReceiveFault(
                faultContext,
                harness.InputQueueAddress,
                messageId,
                UnsupportedContentType.MediaType,
                TypeCache<SerializationException>.ShortName,
                "deserializing the message envelope");
            Assert.False(consumed.Task.IsCompleted);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(operationTimeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECEIVE-FAULT", "nested-contract-type-mismatch")]
    public async Task NestedContractTypeMismatch_PublishesAReceiveFaultWithoutDispatchingAsync()
    {
        TimeSpan operationTimeout = GetOperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness("nested-type-fault", operationTimeout);
        var consumed = new TaskCompletionSource<ConsumeContext<MalformedOrder>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var faulted = new TaskCompletionSource<ConsumeContext<ReceiveFault>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        harness.OnConfigureInMemoryReceiveEndpoint += configurator =>
        {
            configurator.Handler<MalformedOrder>(context =>
            {
                consumed.TrySetResult(context);
                return Task.CompletedTask;
            });
            configurator.Handler<ReceiveFault>(context =>
            {
                faulted.TrySetResult(context);
                return Task.CompletedTask;
            });
        };
        Guid messageId = Guid.Parse("745ab5d9-cd33-4c8c-94b7-bcd25f859943");
        string body = CreateMalformedOrderEnvelope(messageId);

        try
        {
            await harness.StartAsync(cancellationToken).WaitAsync(operationTimeout, cancellationToken);
            await harness.InputQueueSendEndpoint.SendAsync<MalformedOrder>(
                    new
                    {
                        OrderId = Guid.Empty,
                        Forms = Array.Empty<MalformedOrderForm>(),
                    },
                    context =>
                    {
                        context.MessageId = messageId;
                        context.Serializer = new CopyBodySerializer(
                            SystemTextJsonMessageSerializer.JsonContentType,
                            new StringMessageBody(body));
                    },
                    cancellationToken)
                .WaitAsync(operationTimeout, cancellationToken);

            ConsumeContext<ReceiveFault> faultContext = await faulted.Task.WaitAsync(
                operationTimeout,
                cancellationToken);

            AssertReceiveFault(
                faultContext,
                harness.InputQueueAddress,
                messageId,
                SystemTextJsonMessageSerializer.JsonContentType.MediaType,
                TypeCache<JsonException>.ShortName,
                "System.Int32");
            Assert.False(consumed.Task.IsCompleted);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(operationTimeout, CancellationToken.None);
        }
    }

    private static InMemoryTestHarness CreateHarness(string name, TimeSpan operationTimeout)
    {
        var harness = new InMemoryTestHarness($"{name}-{NewId.NextGuid():N}")
        {
            TestTimeout = operationTimeout,
        };
        harness.BeginTestScope();

        return harness;
    }

    private static TimeSpan GetOperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private static void AssertReceiveFault(
        ConsumeContext<ReceiveFault> context,
        Uri expectedSourceAddress,
        Guid expectedMessageId,
        string expectedContentType,
        string expectedExceptionType,
        string expectedExceptionMessageFragment)
    {
        ReceiveFault fault = context.Message;
        ExceptionInfo exception = Assert.Single(fault.Exceptions);

        Assert.Equal(expectedContentType, fault.ContentType);
        Assert.Equal(expectedMessageId, fault.FaultedMessageId);
        Assert.NotEqual(Guid.Empty, fault.FaultId);
        Assert.NotNull(fault.Host);
        Assert.Equal(expectedExceptionType, exception.ExceptionType);
        Assert.Contains(expectedExceptionMessageFragment, exception.Message, StringComparison.Ordinal);
        Assert.Equal(expectedSourceAddress, context.SourceAddress);
    }

    private static string CreateMalformedOrderEnvelope(Guid messageId) => $$"""
        {
          "messageId": "{{messageId:D}}",
          "messageType": [
            "{{MessageUrn.ForTypeString<MalformedOrder>()}}"
          ],
          "message": {
            "orderId": "40e9d37d-3bdf-43a5-a2c0-c8f3f6582fd6",
            "forms": [
              {
                "metadata": {
                  "pageNumber": false
                }
              }
            ]
          }
        }
        """;
}

public sealed record SerializationFailureRequest(Guid CorrelationId);

public sealed record SerializationFailureResponse(Guid CorrelationId);

public interface UnreadableMessage
{
    string Value { get; }
}

public interface MalformedOrder
{
    Guid OrderId { get; }

    IReadOnlyList<MalformedOrderForm> Forms { get; }
}

public interface MalformedOrderForm
{
    MalformedOrderMetadata Metadata { get; }
}

public interface MalformedOrderMetadata
{
    int PageNumber { get; }
}
