using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Initializers.Contexts;
using ViciOne.ServiceBus.Initializers.Conventions;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Initializers.Conventions;

public sealed class DefaultInitializerConventionTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-CONVENTIONS", "object-destination-sync-and-task-sources")]
    public async Task ObjectDestinations_PreserveBothDirectAndAwaitedRuntimeValuesAsync()
    {
        var convention = new DefaultInitializerConvention<ObjectDestinationMessage, ObjectDestinationInput>();
        var directProperty = typeof(ObjectDestinationMessage).GetProperty(nameof(ObjectDestinationMessage.Direct))!;
        var awaitedProperty = typeof(ObjectDestinationMessage).GetProperty(nameof(ObjectDestinationMessage.Awaited))!;
        Assert.True(convention.TryGetPropertyInitializer<object>(directProperty, out var directInitializer));
        Assert.True(convention.TryGetPropertyInitializer<object>(awaitedProperty, out var awaitedInitializer));

        var message = new ObjectDestinationMessage();
        var directValue = new Uri("urn:vicione:initializer-value");
        var input = new ObjectDestinationInput(directValue, Task.FromResult(73));
        InitializeContext<ObjectDestinationMessage, ObjectDestinationInput> context =
            new BaseInitializeContext(TestContext.Current.CancellationToken)
                .CreateMessageContext(message)
                .CreateInputContext(input);

        await directInitializer.ApplyAsync(context, TestContext.Current.CancellationToken);
        await awaitedInitializer.ApplyAsync(context, TestContext.Current.CancellationToken);

        Assert.Same(directValue, message.Direct);
        Assert.Equal(73, message.Awaited);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-HEADERS", "publish-context")]
    public async Task PublishInitializer_MapsStandardAndCustomHeaderPropertiesAsync()
    {
        TimeSpan operationTimeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        using var harness = new InMemoryTestHarness($"initializer-headers-{NewId.NextGuid():N}")
        {
            TestTimeout = operationTimeout,
        };
        harness.BeginTestScope();
        var received = new TaskCompletionSource<ConsumeContext<HeaderInitializedMessage>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        harness.InMemoryReceiveEndpointConfiguring += configurator =>
            configurator.Handler<HeaderInitializedMessage>(context =>
            {
                received.TrySetResult(context);
                return Task.CompletedTask;
            });
        var observer = new HeaderSnapshotObserver();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var requestId = new Guid("28c645e4-bc7c-4384-84b2-dc0b3367e16a");
        var responseAddress = new Uri("loopback://localhost/client-queue");

        try
        {
            await harness.StartAsync(cancellationToken);
            using ConnectHandle observerHandle = harness.Bus.ConnectPublishObserver(observer);

            await harness.Bus.PublishAsync<HeaderInitializedMessage>(
                new
                {
                    __ResponseAddress = responseAddress,
                    __RequestId = requestId,
                    __TimeToLive = 5000,
                    __Header_Custom_Header_Value = "Frankie Say Relax",
                    __Header_Custom_Header_Value2 = 27,
                    __Header_Preserved__Separator = "underscore",
                    Text = "Hello",
                },
                cancellationToken);

            HeaderSnapshot snapshot = await observer.Observed.WaitAsync(operationTimeout, cancellationToken);
            ConsumeContext<HeaderInitializedMessage> context = await received.Task.WaitAsync(
                operationTimeout,
                cancellationToken);

            Assert.Equal(responseAddress, snapshot.ResponseAddress);
            Assert.Equal(requestId, snapshot.RequestId);
            Assert.Equal(TimeSpan.FromSeconds(5), snapshot.TimeToLive);
            Assert.True(snapshot.StringHeaderFound);
            Assert.Equal("Frankie Say Relax", snapshot.StringHeader);
            Assert.True(snapshot.IntegerHeaderFound);
            Assert.IsType<int>(snapshot.IntegerHeader);
            Assert.Equal(27, snapshot.IntegerHeader);
            Assert.True(snapshot.PreservedSeparatorHeaderFound);
            Assert.Equal("underscore", snapshot.PreservedSeparatorHeader);

            Assert.Equal(responseAddress, context.ResponseAddress);
            Assert.Equal(requestId, context.RequestId);
            Assert.NotNull(context.ExpirationTime);
            Assert.True(context.Headers.TryGetHeader("Custom-Header-Value", out object? receivedStringHeader));
            Assert.Equal("Frankie Say Relax", receivedStringHeader);
            Assert.True(context.Advanced().TryGetHeader<int>("Custom-Header-Value2", out int? receivedIntegerHeader));
            Assert.Equal(27, receivedIntegerHeader);
            Assert.True(context.Headers.TryGetHeader("Preserved_Separator", out object? receivedPreservedSeparatorHeader));
            Assert.Equal("underscore", receivedPreservedSeparatorHeader);
            Assert.Equal("Hello", context.Message.Text);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-CONVENTIONS", "required-property-metadata")]
    public void ConventionEntryPoints_RejectNullPropertyMetadata()
    {
        var convention = new DefaultInitializerConvention<HeaderInitializedMessage, HeaderBoundaryInput>();

        ArgumentNullException propertyException = Assert.Throws<ArgumentNullException>(() =>
            convention.TryGetPropertyInitializer<string>(null!, out _));
        ArgumentNullException headerException = Assert.Throws<ArgumentNullException>(() =>
            convention.TryGetHeaderInitializer<string>(null!, out _));
        ArgumentNullException namedHeaderException = Assert.Throws<ArgumentNullException>(() =>
            convention.TryGetHeadersInitializer<string>(null!, out _));

        Assert.Equal("propertyInfo", propertyException.ParamName);
        Assert.Equal("propertyInfo", headerException.ParamName);
        Assert.Equal("propertyInfo", namedHeaderException.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-HEADER-CONVENTION", "invalid-property")]
    public void HeaderConvention_IgnoresAnEmptyHeaderName()
    {
        var convention = new DefaultInitializerConvention<HeaderInitializedMessage, HeaderBoundaryInput>();
        var emptyHeaderProperty = typeof(HeaderBoundaryInput).GetProperty(nameof(HeaderBoundaryInput.__Header_))!;

        bool found = convention.TryGetHeadersInitializer<string>(emptyHeaderProperty, out var initializer);

        Assert.False(found);
        Assert.Null(initializer);
    }

    private sealed class HeaderSnapshotObserver : IPublishObserver
    {
        private readonly TaskCompletionSource<HeaderSnapshot> _observed = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<HeaderSnapshot> Observed => _observed.Task;

        public Task PrePublishAsync<T>(PublishContext<T> context)
            where T : class
        {
            if (context.Message is HeaderInitializedMessage)
            {
                bool stringHeaderFound = context.Headers.TryGetHeader("Custom-Header-Value", out object? stringHeader);
                bool integerHeaderFound = context.Headers.TryGetHeader("Custom-Header-Value2", out object? integerHeader);
                bool preservedSeparatorHeaderFound = context.Headers.TryGetHeader(
                    "Preserved_Separator",
                    out object? preservedSeparatorHeader);
                _observed.TrySetResult(new HeaderSnapshot(
                    context.ResponseAddress,
                    context.RequestId,
                    context.TimeToLive,
                    stringHeaderFound,
                    stringHeader,
                    integerHeaderFound,
                    integerHeader,
                    preservedSeparatorHeaderFound,
                    preservedSeparatorHeader));
            }

            return Task.CompletedTask;
        }

        public Task PostPublishAsync<T>(PublishContext<T> context)
            where T : class => Task.CompletedTask;

        public Task PublishFaultAsync<T>(PublishContext<T> context, Exception exception)
            where T : class => Task.CompletedTask;
    }

    private sealed record HeaderSnapshot(
        Uri? ResponseAddress,
        Guid? RequestId,
        TimeSpan? TimeToLive,
        bool StringHeaderFound,
        object? StringHeader,
        bool IntegerHeaderFound,
        object? IntegerHeader,
        bool PreservedSeparatorHeaderFound,
        object? PreservedSeparatorHeader);

    private sealed class HeaderBoundaryInput
    {
        public string? __Header_ { get; init; }
    }

    private sealed class ObjectDestinationMessage
    {
        public object? Awaited { get; set; }

        public object? Direct { get; set; }
    }

    private sealed record ObjectDestinationInput(Uri Direct, Task<int> Awaited);
}

public interface HeaderInitializedMessage
{
    string Text { get; }
}
