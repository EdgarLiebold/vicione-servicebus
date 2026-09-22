using ViciOne.ServiceBus.Architecture.Tests.Repository;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Architecture.Tests.Product;

public sealed class PayloadAdmissionArchitectureTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-PAYLOAD-ADMISSION-ARCHITECTURE", "common-physical-boundary-precedes-observers-and-provider")]
    public void CommonPhysicalSendBoundary_AdmitsBeforeObserversAndProviderIo()
    {
        string source = Source("src/ViciOne.ServiceBus/Transports/Sending/SendTransport.cs");
        int admission = source.IndexOf("transportContext.ApplyPayloadAdmission(sendContext)", StringComparison.Ordinal);
        Assert.True(admission >= 0, "The common physical send boundary must apply payload admission.");

        int activity = source.IndexOf("TryStartSend", StringComparison.Ordinal);
        int observer = source.IndexOf("SendObservers.PreSendAsync(sendContext)", admission, StringComparison.Ordinal);
        int provider = source.IndexOf("_sendTransportContext.SendAsync(context, sendContext)", admission, StringComparison.Ordinal);

        Assert.True(activity >= 0);
        Assert.True(admission > activity);
        Assert.True(observer > admission);
        Assert.True(provider > observer);

        string boundary = Source("src/ViciOne.ServiceBus/Serialization/Admission/PayloadAdmissionTransportBoundary.cs");
        Assert.Contains("MessageBody body = transportContext.Body", boundary, StringComparison.Ordinal);
        Assert.Contains("long serializedLength = body.Length", boundary, StringComparison.Ordinal);
        Assert.Contains("admission.HasCompleteAdmissionFor(serializedLength)", boundary, StringComparison.Ordinal);
        Assert.DoesNotContain("transportContext.Body.ToArray()", boundary, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PAYLOAD-ADMISSION-ARCHITECTURE", "eventhub-single-and-batch-boundaries")]
    public void EventHubSpecialProducer_AdmitsEverySingleAndBatchMessageBeforeObservationOrIo()
    {
        string source = Source(
            "src/Transports/ViciOne.ServiceBus.EventHubs/EventHubIntegration/EventHubProducer.cs");
        int singleStart = source.IndexOf("class SendPipe<T>", StringComparison.Ordinal);
        int batchStart = source.IndexOf("class BatchSendPipe<T>", StringComparison.Ordinal);
        Assert.True(singleStart >= 0);
        Assert.True(batchStart > singleStart);

        string single = source[singleStart..batchStart];
        int singleAdmission = single.IndexOf("transportContext.ApplyPayloadAdmission(sendContext)", StringComparison.Ordinal);
        int singleObserver = single.IndexOf("SendObservers.PreSendAsync(sendContext)", StringComparison.Ordinal);
        int singleProvider = single.IndexOf("_context.SendAsync(context, sendContext)", StringComparison.Ordinal);
        Assert.True(singleAdmission >= 0);
        Assert.True(singleObserver > singleAdmission);
        Assert.Contains("await _context.SendObservers.PreSendAsync(sendContext)", single, StringComparison.Ordinal);
        int singleAdmissionAfterObserver = single.IndexOf("transportContext.ApplyPayloadAdmission(sendContext)", singleObserver, StringComparison.Ordinal);
        Assert.True(singleAdmissionAfterObserver > singleObserver);
        Assert.True(singleProvider > singleAdmissionAfterObserver);

        string batch = source[batchStart..];
        int batchAdmission = batch.IndexOf("transportContext.ApplyPayloadAdmission(candidate)", StringComparison.Ordinal);
        int batchObserver = batch.IndexOf("SendObservers.PreSendAsync(c)", StringComparison.Ordinal);
        int batchProvider = batch.IndexOf("_context.SendAsync(context, contexts)", StringComparison.Ordinal);
        Assert.True(batchAdmission >= 0);
        Assert.True(batchObserver > batchAdmission);
        Assert.Contains("await Task.WhenAll(contexts.Select(c => _context.SendObservers.PreSendAsync(c)))", batch, StringComparison.Ordinal);
        int batchAdmissionAfterObserver = batch.IndexOf("transportContext.ApplyPayloadAdmission(candidate)", batchObserver, StringComparison.Ordinal);
        Assert.True(batchAdmissionAfterObserver > batchObserver);
        Assert.True(batchProvider > batchAdmissionAfterObserver);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PAYLOAD-ADMISSION-ARCHITECTURE", "real-serializer-owner-order")]
    public void EveryEnvelopeOwner_UsesIndependentBoundedBodyAndEnvelopeBuffersInOrder()
    {
        AssertOwnerOrder("src/ViciOne.ServiceBus/Serialization/Bodies/SystemTextJsonMessageBody.cs", bodyDecisionAfterEnvelopeEncoding: true);
        AssertOwnerOrder("src/ViciOne.ServiceBus/Serialization/Bodies/SystemTextJsonRawMessageBody.cs");
        AssertOwnerOrder("src/ViciOne.ServiceBus.MessagePack/Serialization/MessagePackMessageBody.cs");
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PAYLOAD-ADMISSION-ARCHITECTURE", "send-observer-boundary-inventory")]
    public void EveryProductSendObserverBoundary_IsExplicitlyClassified()
    {
        string[] owners = Directory.GetFiles(Path.Combine(RepositoryLayout.Root, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(path => File.ReadAllText(path).Contains("SendObservers.PreSend", StringComparison.Ordinal))
            .Select(RepositoryLayout.RelativeToRoot)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            [
                "src/Transports/ViciOne.ServiceBus.EventHubs/EventHubIntegration/EventHubProducer.cs",
                "src/ViciOne.ServiceBus/Transports/Sending/SendTransport.cs",
            ],
            owners);
    }

    private static void AssertOwnerOrder(string relativePath, bool bodyDecisionAfterEnvelopeEncoding = false)
    {
        string source = Source(relativePath);
        int bodyOwner = source.IndexOf("CreateSerializedBodyBuffer", StringComparison.Ordinal);
        Assert.True(bodyOwner >= 0, relativePath);

        int bodyDecision = source.IndexOf("EvaluateSerializedBody", bodyOwner, StringComparison.Ordinal);
        Assert.True(bodyDecision > bodyOwner, relativePath);

        int envelopeOwner = source.IndexOf("CreateTransportEnvelopeBuffer", bodyOwner, StringComparison.Ordinal);
        Assert.True(envelopeOwner > bodyOwner, relativePath);

        int envelopeDecision = source.IndexOf("ValidateTransportEnvelope", envelopeOwner, StringComparison.Ordinal);

        Assert.True(envelopeDecision > envelopeOwner, relativePath);
        Assert.True(bodyDecisionAfterEnvelopeEncoding
                ? bodyDecision > envelopeOwner && bodyDecision < envelopeDecision
                : bodyDecision < envelopeOwner,
            relativePath);
    }

    private static string Source(string relativePath) =>
        File.ReadAllText(Path.Combine(RepositoryLayout.Root, relativePath));
}
