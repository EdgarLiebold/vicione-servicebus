using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests;

public sealed class ApplicationApiMetadataTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-APPLICATION-API", "descriptive-message-generic-metadata")]
    public void SendPublishAndConsumerContracts_ExposeMessageAsTheirGenericConcept()
    {
        Assert.Equal("TMessage", Assert.Single(typeof(IConsumer<>).GetGenericArguments()).Name);
        Assert.All(
            typeof(ISendEndpoint).GetMethods().Where(method => method.Name == nameof(ISendEndpoint.SendAsync)),
            method => Assert.Equal("TMessage", Assert.Single(method.GetGenericArguments()).Name));
        Assert.All(
            typeof(IPublishEndpoint).GetMethods().Where(method => method.Name == nameof(IPublishEndpoint.PublishAsync)),
            method => Assert.Equal("TMessage", Assert.Single(method.GetGenericArguments()).Name));
        Assert.All(
            typeof(IOutgoingMessages).GetMethods().Where(method => method.IsGenericMethod),
            method => Assert.Equal("TMessage", Assert.Single(method.GetGenericArguments()).Name));
        Assert.All(
            typeof(Headers).GetMethods().Where(method => method.IsGenericMethod),
            method => Assert.Equal("TValue", Assert.Single(method.GetGenericArguments()).Name));
        Assert.All(
            typeof(PipeContext).GetMethods().Where(method => method.IsGenericMethod),
            method => Assert.Equal("TPayload", Assert.Single(method.GetGenericArguments()).Name));
        Assert.All(
            typeof(SerializerContext).GetMethods().Where(method => method.IsGenericMethod),
            method => Assert.Equal("TMessage", Assert.Single(method.GetGenericArguments()).Name));
        Assert.Equal(
            "THeaderValue",
            Assert.Single(typeof(SendHeadersExtensions).GetMethods()
                    .Single(method => method.IsGenericMethod && method.Name == nameof(SendHeadersExtensions.CopyFrom))
                    .GetGenericArguments())
                .Name);
    }
}
