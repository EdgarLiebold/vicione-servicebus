using ViciOne.ServiceBus.Advanced.Initializers;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Initializers.Contexts;
using ViciOne.ServiceBus.Initializers.Factories;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Initializers.Factories;

public sealed class MessageInitializerBuilderContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-FACTORIES", "dynamic-class-and-contract-creation-matrix")]
    public void DynamicFactories_CreateIndependentMessagesInsideTheRequestedGraph()
    {
        var root = new BaseInitializeContext(TestContext.Current.CancellationToken);
        var classFactory = new DynamicMessageFactory<TestMessage>();
        var contractFactory = new DynamicMessageFactory<ITestMessage, ContractMessage>();

        var standalone = Assert.IsType<TestMessage>(((IMessageFactory)classFactory).Create());
        InitializeContext<TestMessage> classContext = classFactory.Create(root);
        InitializeContext<ITestMessage> contractContext = contractFactory.Create(root);

        Assert.NotSame(standalone, classContext.Message);
        Assert.IsType<TestMessage>(classContext.Message);
        Assert.IsType<ContractMessage>(contractContext.Message);
        Assert.Equal(1, classContext.Depth);
        Assert.Equal(1, contractContext.Depth);
        Assert.Same(root, classContext.Parent);
        Assert.Same(root, contractContext.Parent);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => classFactory.Create(null!)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => contractFactory.Create(null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-FACTORIES", "builder-adapters-order-and-forwarding-matrix")]
    public async Task Builder_AdaptsBothInitializerShapesAndBuildsAnIndependentPlanAsync()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        var builder = new MessageInitializerBuilder<TestMessage, TestInput>(new DynamicMessageFactory<TestMessage>());
        var independentProperty = new IndependentPropertyInitializer();
        var inputProperty = new InputPropertyInitializer();
        var independentHeader = new IndependentHeaderInitializer();
        var inputHeader = new InputHeaderInitializer();
        builder.Add(nameof(TestMessage.Value), independentProperty);
        builder.Add(nameof(TestMessage.Count), inputProperty);
        builder.Add(independentHeader);
        builder.Add(inputHeader);
        Assert.False(builder.IsInputPropertyUsed("Source"));
        builder.SetInputPropertyUsed("Source");
        Assert.True(builder.IsInputPropertyUsed("source"));

        IMessageInitializer<TestMessage> initializer = builder.Build();
        builder.Add(nameof(TestMessage.LateValue), new LatePropertyInitializer());
        var input = new TestInput("north", 7);
        InitializedMessage<TestMessage> initialized = await initializer.InitializeMessageAsync(
            input, Pipe.Empty<SendContext<TestMessage>>(), token);
        var sendContext = new MessageSendContext<TestMessage>(initialized.Message, token);
        await initialized.Pipe.SendAsync(sendContext);

        Assert.Equal("independent", initialized.Message.Value);
        Assert.Equal(7, initialized.Message.Count);
        Assert.Null(initialized.Message.LateValue);
        Assert.Equal("independent", sendContext.Headers.Get<string>("base"));
        Assert.Equal("north", sendContext.Headers.Get<string>("tenant"));
        Assert.Equal(token, independentProperty.Token);
        Assert.Equal(token, inputProperty.Token);
        Assert.Equal(token, independentHeader.Token);
        Assert.Equal(token, inputHeader.Token);
        Assert.Same(input, inputProperty.Input);
        Assert.Same(input, inputHeader.Input);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-FACTORIES", "builder-default-factory-and-boundary-matrix")]
    public void Builder_UsesTheDefaultFactoryAndRejectsInvalidMappingsAtTheBoundary()
    {
        var builder = new MessageInitializerBuilder<TestMessage, TestInput>(null);
        IMessageInitializer<TestMessage> initializer = builder.Build();
        InitializeContext<TestMessage> context = initializer.Create(TestContext.Current.CancellationToken);

        Assert.IsType<TestMessage>(context.Message);
        Assert.Equal("propertyName", Assert.Throws<ArgumentException>(() =>
            builder.Add(" ", new IndependentPropertyInitializer())).ParamName);
        Assert.Equal("initializer", Assert.Throws<ArgumentNullException>(() =>
            builder.Add("Value", (IPropertyInitializer<TestMessage>)null!)).ParamName);
        Assert.Equal("initializer", Assert.Throws<ArgumentNullException>(() =>
            builder.Add("Value", (IPropertyInitializer<TestMessage, TestInput>)null!)).ParamName);
        Assert.Equal("initializer", Assert.Throws<ArgumentNullException>(() =>
            builder.Add((IHeaderInitializer<TestMessage>)null!)).ParamName);
        Assert.Equal("initializer", Assert.Throws<ArgumentNullException>(() =>
            builder.Add((IHeaderInitializer<TestMessage, TestInput>)null!)).ParamName);
        Assert.Equal("propertyName", Assert.Throws<ArgumentException>(() => builder.IsInputPropertyUsed(" ")).ParamName);
        Assert.Equal("propertyName", Assert.Throws<ArgumentException>(() => builder.SetInputPropertyUsed(" ")).ParamName);
        Assert.Equal("TMessage", Assert.Throws<ArgumentException>(() =>
            new MessageInitializerBuilder<object, TestInput>(null)).ParamName);
        Assert.Equal("TMessage", Assert.Throws<ArgumentException>(() =>
            new MessageInitializerBuilder<NoPublicConstructorMessage, TestInput>(null).Build()).ParamName);
    }

    private sealed class IndependentPropertyInitializer : IPropertyInitializer<TestMessage>
    {
        public CancellationToken Token { get; private set; }

        public Task ApplyAsync(InitializeContext<TestMessage> context, CancellationToken cancellationToken = default)
        {
            Token = cancellationToken;
            context.Message.Value = "independent";
            return Task.CompletedTask;
        }
    }

    private sealed class InputPropertyInitializer : IPropertyInitializer<TestMessage, TestInput>
    {
        public TestInput? Input { get; private set; }

        public CancellationToken Token { get; private set; }

        public Task ApplyAsync(InitializeContext<TestMessage, TestInput> context, CancellationToken cancellationToken = default)
        {
            Input = context.Input;
            Token = cancellationToken;
            context.Message.Count = context.Input.Count;
            return Task.CompletedTask;
        }
    }

    private sealed class LatePropertyInitializer : IPropertyInitializer<TestMessage, TestInput>
    {
        public Task ApplyAsync(InitializeContext<TestMessage, TestInput> context, CancellationToken cancellationToken = default)
        {
            context.Message.LateValue = "late";
            return Task.CompletedTask;
        }
    }

    private sealed class IndependentHeaderInitializer : IHeaderInitializer<TestMessage>
    {
        public CancellationToken Token { get; private set; }

        public Task ApplyAsync(InitializeContext<TestMessage> context, SendContext sendContext,
            CancellationToken cancellationToken = default)
        {
            Token = cancellationToken;
            sendContext.Headers.Set("base", "independent");
            return Task.CompletedTask;
        }
    }

    private sealed class InputHeaderInitializer : IHeaderInitializer<TestMessage, TestInput>
    {
        public TestInput? Input { get; private set; }

        public CancellationToken Token { get; private set; }

        public Task ApplyAsync(InitializeContext<TestMessage, TestInput> context, SendContext sendContext,
            CancellationToken cancellationToken = default)
        {
            Input = context.Input;
            Token = cancellationToken;
            sendContext.Headers.Set("tenant", context.Input.Source);
            return Task.CompletedTask;
        }
    }

    public interface ITestMessage
    {
        string? Value { get; set; }
    }

    public sealed class ContractMessage : ITestMessage
    {
        public string? Value { get; set; }
    }

    public sealed class TestMessage
    {
        public int Count { get; set; }

        public string? LateValue { get; set; }

        public string? Value { get; set; }
    }

    public sealed class NoPublicConstructorMessage
    {
        public NoPublicConstructorMessage(string value)
        {
            Value = value;
        }

        public string Value { get; }
    }

    private sealed record TestInput(string Source, int Count);
}
