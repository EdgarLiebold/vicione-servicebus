using System.Dynamic;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Initializers.Contexts;
using ViciOne.ServiceBus.Initializers.Conventions;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Initializers.Conventions;

public sealed class DictionaryInitializerConventionTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-CONVENTIONS", "dictionary-exact-and-unsupported-mapping-matrix")]
    public async Task DictionaryConvention_DistinguishesExactMappingsFromUnsupportedTypesAsync()
    {
        var exact = new DictionaryInitializerConvention<ExactDictionaryMessage, Dictionary<string, Guid?>, Guid?>();
        var valueProperty = typeof(ExactDictionaryMessage).GetProperty(nameof(ExactDictionaryMessage.Value))!;
        var requestIdProperty = typeof(SendContext).GetProperty(nameof(SendContext.RequestId))!;
        Assert.True(exact.TryGetPropertyInitializer<Guid?>(valueProperty, out var propertyInitializer));
        Assert.True(exact.TryGetHeaderInitializer<Guid?>(requestIdProperty, out var headerInitializer));

        Guid value = Guid.Parse("8e6005b3-06d2-4ed6-87d6-b19752c0c46d");
        var values = new Dictionary<string, Guid?>
        {
            [nameof(ExactDictionaryMessage.Value)] = value,
            ["__RequestId"] = value,
        };
        var message = new ExactDictionaryMessage();
        InitializeContext<ExactDictionaryMessage, Dictionary<string, Guid?>> context =
            new BaseInitializeContext(TestContext.Current.CancellationToken)
                .CreateMessageContext(message)
                .CreateInputContext(values);
        var sendContext = new MessageSendContext<ExactDictionaryMessage>(message, TestContext.Current.CancellationToken);

        await propertyInitializer.ApplyAsync(context, TestContext.Current.CancellationToken);
        await headerInitializer.ApplyAsync(context, sendContext, TestContext.Current.CancellationToken);

        Assert.Equal(value, message.Value);
        Assert.Equal(value, sendContext.RequestId);

        var unsupported = new DictionaryInitializerConvention<UnsupportedDictionaryMessage, Dictionary<string, DateOnly>, DateOnly>();
        var countProperty = typeof(UnsupportedDictionaryMessage).GetProperty(nameof(UnsupportedDictionaryMessage.Count))!;
        var timeToLiveProperty = typeof(SendContext).GetProperty(nameof(SendContext.TimeToLive))!;
        Assert.False(unsupported.TryGetPropertyInitializer<int>(countProperty, out var unsupportedProperty));
        Assert.Null(unsupportedProperty);
        Assert.False(unsupported.TryGetHeaderInitializer<TimeSpan?>(timeToLiveProperty, out var unsupportedHeader));
        Assert.Null(unsupportedHeader);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DICTIONARY-INITIALIZER", "expando-scalar-enum-guid")]
    public async Task ExpandoObjectInput_ConvertsScalarEnumAndGuidValuesAsync()
    {
        var uniqueId = Guid.Parse("9b915632-74ed-4145-af1c-9793468c48b8");
        var source = new ExpandoObject();
        var values = (IDictionary<string, object?>)source;
        values.Add(nameof(MessageContract.Id), 27);
        values.Add(nameof(MessageContract.CustomerId), "SuperMart");
        values.Add(nameof(MessageContract.UniqueId), uniqueId);
        values.Add(nameof(MessageContract.CustomerType), 1L);
        values.Add(nameof(MessageContract.TypeByName), "Internal");

        InitializeContext<MessageContract> context = await MessageInitializerCache<MessageContract>.InitializeAsync(
            source,
            TestContext.Current.CancellationToken);

        Assert.Equal(27, context.Message.Id);
        Assert.Equal("SuperMart", context.Message.CustomerId);
        Assert.Equal(uniqueId, context.Message.UniqueId);
        Assert.Equal(CustomerType.Public, context.Message.CustomerType);
        Assert.Equal(CustomerType.Internal, context.Message.TypeByName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DICTIONARY-INITIALIZER", "plain-dictionary")]
    public async Task DictionaryInput_InitializesTheMessageContractAsync()
    {
        var uniqueId = Guid.Parse("7cdcb2c7-7f7b-4aa7-9896-e2f4aef1614f");
        IDictionary<string, object> source = new Dictionary<string, object>
        {
            [nameof(MessageContract.Id)] = 27,
            [nameof(MessageContract.CustomerId)] = "SuperMart",
            [nameof(MessageContract.UniqueId)] = uniqueId,
        };

        InitializeContext<MessageContract> context = await MessageInitializerCache<MessageContract>.InitializeAsync(
            source,
            TestContext.Current.CancellationToken);

        Assert.Equal(27, context.Message.Id);
        Assert.Equal("SuperMart", context.Message.CustomerId);
        Assert.Equal(uniqueId, context.Message.UniqueId);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DICTIONARY-INITIALIZER", "nested-dictionary-property")]
    public async Task DictionaryValuedProperty_InitializesANestedMessageContractAsync()
    {
        var uniqueId = Guid.Parse("a637c61e-4ca8-46c5-a1db-dee0f24ed020");
        IDictionary<string, object> contract = new Dictionary<string, object>
        {
            [nameof(MessageContract.Id)] = 27,
            [nameof(MessageContract.CustomerId)] = "SuperMart",
            [nameof(MessageContract.UniqueId)] = uniqueId,
        };

        InitializeContext<MessageEnvelope> context = await MessageInitializerCache<MessageEnvelope>.InitializeAsync(
            new { Contract = contract },
            TestContext.Current.CancellationToken);

        Assert.NotNull(context.Message.Contract);
        Assert.Equal(27, context.Message.Contract.Id);
        Assert.Equal("SuperMart", context.Message.Contract.CustomerId);
        Assert.Equal(uniqueId, context.Message.Contract.UniqueId);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DICTIONARY-INITIALIZER", "nested-expando-list")]
    public async Task NestedExpandoObjectList_ConvertsEveryExposedValueAsync()
    {
        var orderId = Guid.Parse("59010453-4b77-4a1e-9827-665a11cb1ddc");

        var product = new ExpandoObject();
        var productValues = (IDictionary<string, object?>)product;
        productValues.Add(nameof(IProduct.Name), "Foo");
        productValues.Add(nameof(IProduct.Category), "Bar");

        var orderProduct = new ExpandoObject();
        var orderProductValues = (IDictionary<string, object?>)orderProduct;
        orderProductValues.Add(nameof(IProduct.Name), "Product");
        orderProductValues.Add(nameof(IProduct.Category), "Category");

        var order = new ExpandoObject();
        var orderValues = (IDictionary<string, object?>)order;
        orderValues.Add(nameof(IOrder.Id), orderId.ToString());
        orderValues.Add(nameof(IOrder.Product), orderProduct);
        orderValues.Add(nameof(IOrder.Quantity), 10L);

        var source = new ExpandoObject();
        var values = (IDictionary<string, object?>)source;
        values.Add(nameof(MessageContract.Id), 32L);
        values.Add(nameof(MessageContract.CustomerId), "CustomerXp");
        values.Add(nameof(MessageContract.Product), product);
        values.Add(nameof(MessageContract.Orders), new List<object> { order });

        InitializeContext<MessageContract> context = await MessageInitializerCache<MessageContract>.InitializeAsync(
            source,
            TestContext.Current.CancellationToken);

        Assert.Equal(32, context.Message.Id);
        Assert.Equal("CustomerXp", context.Message.CustomerId);
        Assert.NotNull(context.Message.Product);
        Assert.Equal("Foo", context.Message.Product.Name);
        Assert.Equal("Bar", context.Message.Product.Category);
        IOrder actualOrder = Assert.Single(context.Message.Orders);
        Assert.Equal(orderId, actualOrder.Id);
        Assert.Equal(10, actualOrder.Quantity);
        Assert.NotNull(actualOrder.Product);
        Assert.Equal("Product", actualOrder.Product.Name);
        Assert.Equal("Category", actualOrder.Product.Category);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-HEADERS", "dictionary-prefixed-converted-header")]
    public async Task DictionaryInput_UsesThePrefixedKeyWhenConvertingAStandardHeaderAsync()
    {
        var convention = new DictionaryInitializerConvention<HeaderMessage, Dictionary<string, object>, object>();
        var headerProperty = typeof(SendContext).GetProperty(nameof(SendContext.TimeToLive));
        Assert.NotNull(headerProperty);
        Assert.True(convention.TryGetHeaderInitializer<TimeSpan?>(headerProperty, out var initializer));
        Assert.NotNull(initializer);

        var values = new Dictionary<string, object>
        {
            ["__TimeToLive"] = 5_000L,
        };
        var baseContext = new BaseInitializeContext(TestContext.Current.CancellationToken);
        InitializeContext<HeaderMessage> messageContext = baseContext.CreateMessageContext(new HeaderMessage());
        InitializeContext<HeaderMessage, Dictionary<string, object>> inputContext = messageContext.CreateInputContext(values);
        var sendContext = new MessageSendContext<HeaderMessage>(messageContext.Message, TestContext.Current.CancellationToken);

        await initializer.ApplyAsync(inputContext, sendContext, TestContext.Current.CancellationToken);

        Assert.Equal(TimeSpan.FromSeconds(5), sendContext.TimeToLive);
    }

    public interface MessageContract
    {
        int Id { get; }

        string CustomerId { get; }

        Guid UniqueId { get; }

        CustomerType CustomerType { get; }

        CustomerType TypeByName { get; }

        IProduct Product { get; }

        List<IOrder> Orders { get; }
    }

    public interface MessageEnvelope
    {
        MessageContract Contract { get; }
    }

    public interface IOrder
    {
        Guid Id { get; }

        IProduct Product { get; }

        int Quantity { get; }
    }

    public interface IProduct
    {
        string Name { get; }

        string Category { get; }
    }

    public enum CustomerType
    {
        Public = 1,
        Internal = 2,
    }

    private sealed class HeaderMessage;

    private sealed class ExactDictionaryMessage
    {
        public Guid? Value { get; set; }
    }

    private sealed class UnsupportedDictionaryMessage
    {
        public int Count { get; set; }
    }
}
