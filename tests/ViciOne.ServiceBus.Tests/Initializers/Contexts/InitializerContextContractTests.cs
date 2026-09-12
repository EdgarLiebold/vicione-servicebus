using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Initializers.Contexts;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Initializers.Contexts;

public sealed class InitializerContextContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-CONTEXTS", "message-input-and-nested-depth-matrix")]
    public void MessageAndInputViews_PreserveOneDepthStepPerObjectGraphNode()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        var root = new BaseInitializeContext(token);
        var parentMessage = new ParentMessage();
        InitializeContext<ParentMessage> parent = root.CreateMessageContext(parentMessage);
        var inputValue = new InputValue("input");
        InitializeContext<ParentMessage, InputValue> input = parent.CreateInputContext(inputValue);
        var childMessage = new ChildMessage();
        InitializeContext<ChildMessage> child = input.CreateMessageContext(childMessage);

        Assert.Equal(0, root.Depth);
        Assert.Null(root.Parent);
        Assert.Equal(1, parent.Depth);
        Assert.Same(root, parent.Parent);
        Assert.Same(parentMessage, parent.Message);
        Assert.Equal(typeof(ParentMessage), parent.MessageType);
        Assert.Equal(1, input.Depth);
        Assert.Same(root, input.Parent);
        Assert.True(input.HasInput);
        Assert.Same(inputValue, input.Input);
        Assert.Same(parentMessage, input.Message);
        Assert.Equal(2, child.Depth);
        Assert.Same(input, child.Parent);
        Assert.Same(childMessage, child.Message);

        Assert.False(root.TryGetParent<ParentMessage>(out var absentAtRoot));
        Assert.Null(absentAtRoot);
        Assert.True(input.TryGetParent<ParentMessage>(out var currentParent));
        Assert.Same(input, currentParent);
        Assert.True(child.TryGetParent<ParentMessage>(out var ancestor));
        Assert.Same(input, ancestor);
        Assert.True(child.TryGetParent<ChildMessage>(out var currentChild));
        Assert.Same(child, currentChild);
        Assert.False(child.TryGetParent<UnrelatedMessage>(out var unrelated));
        Assert.Null(unrelated);
        Assert.Equal(token, child.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-CONTEXTS", "scope-payload-and-construction-boundaries")]
    public void RootContexts_PreservePipelinePayloadsAndRejectMissingGraphState()
    {
        using var cancellation = new CancellationTokenSource();
        var payload = new TestPayload("owned");
        var pipeContext = new TestPipeContext(cancellation.Token, payload);
        var scope = new ScopeInitializeContext(pipeContext);

        Assert.Equal(0, scope.Depth);
        Assert.Null(scope.Parent);
        Assert.Equal(cancellation.Token, scope.CancellationToken);
        Assert.True(scope.HasPayloadType(typeof(TestPayload)));
        Assert.True(scope.TryGetPayload<TestPayload>(out var resolvedPayload));
        Assert.Same(payload, resolvedPayload);
        Assert.Same(payload, scope.GetOrAddPayload(() => new TestPayload("replacement")));
        Assert.False(scope.TryGetParent<ParentMessage>(out _));

        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => new ScopeInitializeContext(null!)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
            new DynamicInitializeContext<ParentMessage>(null!, new ParentMessage())).ParamName);
        var root = new BaseInitializeContext(TestContext.Current.CancellationToken);
        Assert.Equal("message", Assert.Throws<ArgumentNullException>(() =>
            new DynamicInitializeContext<ParentMessage>(root, null!)).ParamName);
        InitializeContext<ParentMessage> messageContext = root.CreateMessageContext(new ParentMessage());
        Assert.Equal("input", Assert.Throws<ArgumentNullException>(() => messageContext.CreateInputContext<InputValue>(null!)).ParamName);
        Assert.Equal("message", Assert.Throws<ArgumentNullException>(() => root.CreateMessageContext<ParentMessage>(null!)).ParamName);
        Assert.Equal("message", Assert.Throws<ArgumentNullException>(() => messageContext.CreateMessageContext<ChildMessage>(null!)).ParamName);
    }

    private sealed class TestPipeContext(CancellationToken cancellationToken, TestPayload payload) :
        BasePipeContext(cancellationToken, payload);

    private sealed record TestPayload(string Value);

    private sealed record InputValue(string Value);

    private sealed class ParentMessage;

    private sealed class ChildMessage;

    private sealed class UnrelatedMessage;
}
