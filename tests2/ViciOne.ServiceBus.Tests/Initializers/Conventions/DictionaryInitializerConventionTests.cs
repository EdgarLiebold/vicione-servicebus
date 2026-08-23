using System.Dynamic;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Initializers.Conventions;

public sealed class DictionaryInitializerConventionTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-DICTIONARY-INITIALIZER", "expando-scalar-enum-guid")]
    public async Task ExpandoObjectInput_ConvertsScalarEnumAndGuidValues()
    {
        var uniqueId = Guid.Parse("9b915632-74ed-4145-af1c-9793468c48b8");
        var source = new ExpandoObject();
        var values = (IDictionary<string, object?>)source;
        values.Add(nameof(MessageContract.Id), 27);
        values.Add(nameof(MessageContract.CustomerId), "SuperMart");
        values.Add(nameof(MessageContract.UniqueId), uniqueId);
        values.Add(nameof(MessageContract.CustomerType), 1L);
        values.Add(nameof(MessageContract.TypeByName), "Internal");

        InitializeContext<MessageContract> context = await MessageInitializerCache<MessageContract>.Initialize(
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
    public async Task DictionaryInput_InitializesTheMessageContract()
    {
        var uniqueId = Guid.Parse("7cdcb2c7-7f7b-4aa7-9896-e2f4aef1614f");
        IDictionary<string, object> source = new Dictionary<string, object>
        {
            [nameof(MessageContract.Id)] = 27,
            [nameof(MessageContract.CustomerId)] = "SuperMart",
            [nameof(MessageContract.UniqueId)] = uniqueId,
        };

        InitializeContext<MessageContract> context = await MessageInitializerCache<MessageContract>.Initialize(
            source,
            TestContext.Current.CancellationToken);

        Assert.Equal(27, context.Message.Id);
        Assert.Equal("SuperMart", context.Message.CustomerId);
        Assert.Equal(uniqueId, context.Message.UniqueId);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DICTIONARY-INITIALIZER", "nested-dictionary-property")]
    public async Task DictionaryValuedProperty_InitializesANestedMessageContract()
    {
        var uniqueId = Guid.Parse("a637c61e-4ca8-46c5-a1db-dee0f24ed020");
        IDictionary<string, object> contract = new Dictionary<string, object>
        {
            [nameof(MessageContract.Id)] = 27,
            [nameof(MessageContract.CustomerId)] = "SuperMart",
            [nameof(MessageContract.UniqueId)] = uniqueId,
        };

        InitializeContext<MessageEnvelope> context = await MessageInitializerCache<MessageEnvelope>.Initialize(
            new { Contract = contract },
            TestContext.Current.CancellationToken);

        Assert.NotNull(context.Message.Contract);
        Assert.Equal(27, context.Message.Contract.Id);
        Assert.Equal("SuperMart", context.Message.Contract.CustomerId);
        Assert.Equal(uniqueId, context.Message.Contract.UniqueId);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DICTIONARY-INITIALIZER", "nested-expando-list")]
    public async Task NestedExpandoObjectList_ConvertsEveryExposedValue()
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

        InitializeContext<MessageContract> context = await MessageInitializerCache<MessageContract>.Initialize(
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
}
