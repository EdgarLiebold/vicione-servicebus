using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Advanced.Initializers;

public sealed class InitializedMessageTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZED-MESSAGE", "complete-deconstruction")]
    public void ConstructorAndDeconstruction_PreserveTheMessageAndNonEmptyPipe()
    {
        var message = new TestMessage("value");
        var pipe = new RecordingPipe();

        var initialized = new InitializedMessage<TestMessage>(message, pipe);
        (TestMessage actualMessage, IPipe<SendContext<TestMessage>> actualPipe) = initialized;

        Assert.Same(message, initialized.Message);
        Assert.Same(pipe, initialized.Pipe);
        Assert.Same(message, actualMessage);
        Assert.Same(pipe, actualPipe);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZED-MESSAGE", "empty-pipe-normalization")]
    public void MissingOrEmptyPipe_UsesTheCanonicalEmptyPipe()
    {
        var message = new TestMessage("value");
        IPipe<SendContext<TestMessage>> empty = Pipe.Empty<SendContext<TestMessage>>();

        var implicitEmpty = new InitializedMessage<TestMessage>(message);
        var nullPipe = new InitializedMessage<TestMessage>(message, null);
        var explicitEmpty = new InitializedMessage<TestMessage>(message, empty);

        Assert.Same(empty, implicitEmpty.Pipe);
        Assert.Same(empty, nullPipe.Pipe);
        Assert.Same(empty, explicitEmpty.Pipe);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZED-MESSAGE", "required-message")]
    public void Constructors_RejectAMissingMessage()
    {
        Assert.Equal(
            "message",
            Assert.Throws<ArgumentNullException>(() => new InitializedMessage<TestMessage>(null!)).ParamName);
        Assert.Equal(
            "message",
            Assert.Throws<ArgumentNullException>(() => new InitializedMessage<TestMessage>(null!, new RecordingPipe())).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZED-MESSAGE", "property-based-valid-state")]
    public void PublicShape_UsesPropertiesAndRejectsDefaultValues()
    {
        Type type = typeof(InitializedMessage<TestMessage>);
        InitializedMessage<TestMessage> uninitialized = default;

        Assert.True(type.IsValueType);
        Assert.True(type.IsSealed);
        Assert.Empty(type.GetFields(BindingFlags.Instance | BindingFlags.Public));
        Assert.Equal(
            ["Message", "Pipe"],
            type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Select(property => property.Name)
                .Order(StringComparer.Ordinal));
        Assert.Throws<InvalidOperationException>(() => uninitialized.Message);
        Assert.Throws<InvalidOperationException>(() => uninitialized.Pipe);
    }

    private sealed record TestMessage(string Value);

    private sealed class RecordingPipe : IPipe<SendContext<TestMessage>>
    {
        public Task SendAsync(SendContext<TestMessage> context) => Task.CompletedTask;

        public void Probe(ProbeContext context)
        {
        }
    }
}
