using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace ViciOne.ServiceBus.Tests.Infrastructure.Analyzers.MessageContracts;

/// <summary>
/// Canonical source scenarios for the message-contract analyzer and its code fix.
/// Each scenario describes product behavior; xUnit projects remain the sole verdict owners.
/// </summary>
public static class MessageContractScenarioCatalog
{
    private const string MissingId = "VOSB1004";
    private const string IncompatibleId = "VOSB1002";

    private const string SimpleArrayContract = """
        namespace ConsoleApplication1
        {
            public interface OrderSubmitted
            {
                Guid Id { get; }
                string CustomerId { get; }
                IReadOnlyList<Guid> OrderItems { get; }
            }
        }

        """;

    private const string NullableContract = """
        namespace ConsoleApplication1
        {
            public interface OrderSubmitted
            {
                Guid Id { get; }
                int Quantity { get; }
                decimal? Price { get; }
            }
        }

        """;

    private const string RecursiveContract = """
        namespace ConsoleApplication1
        {
            public interface Foo
            {
                IList<Foo> Children { get; }
                Bar Bar { get; }
            }

            public interface Bar;
        }

        """;

    private const string EnumerableContract = """
        namespace ConsoleApplication1
        {
            public interface SubmitOrderEnumerable
            {
                Guid Id { get; }
                string CustomerId { get; }
                IEnumerable<OrderItem> OrderItems { get; }
            }

            public interface OrderItem
            {
                Guid Id { get; }
                Product Product { get; }
                int Quantity { get; }
                decimal Price { get; }
            }

            public interface Product
            {
                string Name { get; }
                Uri Category { get; }
            }
        }

        """;

    private const string RecordContract = """
        namespace ConsoleApplication1
        {
            public record OrderSubmissionReceived
            {
                public Guid Id { get; init; }
                public string CustomerId { get; init; }
            }
        }

        """;

    private const string ConcreteContract = """
        namespace ConsoleApplication1
        {
            public class OrderDto
            {
                public Guid Id { get; set; }
                public string CustomerId { get; set; }
                public IReadOnlyList<OrderItemDto> OrderItems { get; set; }
            }

            public class OrderItemDto
            {
                public Guid Id { get; set; }
                public ProductDto Product { get; set; }
                public int Quantity { get; set; }
                public decimal Price { get; set; }
            }

            public class ProductDto
            {
                public string Name { get; set; }
                public string Category { get; set; }
            }
        }

        """;

    private const string MessageDataContract = """
        namespace ConsoleApplication1
        {
            public class DataMessage
            {
                public Guid CorrelationId { get; set; }
                public MessageData<DataDictionary> Dictionary { get; set; }
            }

            public class DataDictionary
            {
                public Dictionary<string, string> Values { get; set; }
            }
        }

        """;

    private const string NamespaceContracts = """
        namespace ConsoleApplication1.Messages
        {
            public interface OrderSubmitted
            {
                Guid Id { get; }
                string CustomerId { get; }
                IReadOnlyList<OrderItem> OrderItems { get; }
            }

            public interface OrderItem
            {
                Guid Id { get; }
                Product Product { get; }
                int Quantity { get; }
                decimal Price { get; }
            }

            public interface Product
            {
                string Name { get; }
                Uri Category { get; }
            }
        }

        """;

    private const string OrderDtos = """
        namespace ConsoleApplication1
        {
            public sealed class OrderDto
            {
                public Guid Id { get; set; }
                public string CustomerId { get; set; }
                public List<OrderItemDto> OrderItems { get; } = [];
            }

            public sealed class OrderItemDto
            {
                public Guid Id { get; set; }
                public ProductDto Product { get; set; }
                public int Quantity { get; set; }
                public decimal Price { get; set; }
            }

            public sealed class ProductDto
            {
                public string Name { get; set; }
                public string Category { get; set; }
            }
        }

        """;

    private const string IncompatibleOrderDtos = """
        namespace ConsoleApplication1
        {
            public sealed class OrderDto
            {
                public Guid Id { get; set; }
                public string CustomerId { get; set; }
                public List<OrderItemDto> OrderItems { get; } = [];
            }

            public sealed class OrderItemDto
            {
                public Guid Id { get; set; }
                public ProductDto Product { get; set; }
                public int Quantity { get; set; }
                public decimal Price { get; set; }
            }

            public sealed class ProductDto
            {
                public string Name { get; set; }
                public int Category { get; set; }
            }
        }

        """;

    public static ImmutableArray<MessageContractScenario> All { get; } =
    [
        Pair(
            "generic-contract-missing",
            form => MessageContractSourceFactory.GenericPublish("new { }", form),
            Missing("INotification", "StreamId"),
            Init("StreamId", "default(Guid)")),
        Pair(
            "generic-contract-complete",
            form => MessageContractSourceFactory.GenericPublish("new { StreamId = streamId }", form)),
        Single(
            "non-servicebus-anonymous-object",
            _ => MessageContractSourceFactory.MinimalUsings + $$"""
                namespace ConsoleApplication1
                {
                    class Program
                    {
                        static void Main()
                        {
                            _ = {{MessageContractSourceFactory.Mark("new { Module = 13, Index = 412 }")}};
                        }
                    }
                }
                """),
        Pair(
            "generic-contract-incompatible",
            form => MessageContractSourceFactory.GenericPublish("new { StreamId = streamId }", form, "int"),
            Incompatible("INotification", "StreamId")),
        Pair(
            "inferred-members-missing",
            form => InferredOrder(form, missing: true, incompatible: false),
            Missing("OrderSubmitted", "CustomerId", "OrderItems.Product.Category", "OrderItems.Price"),
            Init("CustomerId", "default(string)"),
            Init("OrderItems.Product.Category", "default(Uri)"),
            Init("OrderItems.Price", "default(decimal)")),
        Pair(
            "inferred-members-complete",
            form => InferredOrder(form, missing: false, incompatible: false)),
        Pair(
            "inferred-members-incompatible",
            form => InferredOrder(form, missing: false, incompatible: true),
            Incompatible("OrderSubmitted", "OrderItems.Product.Category")),
        Pair(
            "request-create-missing",
            form => MessageContractSourceFactory.CreateRequest("new { }", form),
            Missing("CheckOrderStatus", "OrderId")),
        Pair(
            "request-get-response-missing",
            form => MessageContractSourceFactory.GetResponse("new { }", form),
            Missing("CheckOrderStatus", "OrderId")),
        Pair(
            "generic-request-complete",
            form => MessageContractSourceFactory.GenericRequest("new { ClientId = Guid.Empty }", form)),
        Pair(
            "generic-request-missing",
            form => MessageContractSourceFactory.GenericRequest("new { }", form),
            Missing("Link", "ClientId")),
        Pair(
            "request-create-complete",
            form => MessageContractSourceFactory.CreateRequest("new { OrderId = Guid.Empty }", form)),
        Pair(
            "namespaced-contract-missing",
            form => MessageContractSourceFactory.Publish(
                "Messages.OrderSubmitted",
                MissingNestedOrderPayload,
                form,
                NamespaceContracts),
            Missing("OrderSubmitted", "CustomerId", "OrderItems.Product.Category", "OrderItems.Price"),
            Init("CustomerId", "default(string)"),
            Init("OrderItems.Product.Category", "default(Uri)"),
            Init("OrderItems.Price", "default(decimal)")),
        Pair(
            "namespaced-contract-complete",
            form => MessageContractSourceFactory.Publish(
                "Messages.OrderSubmitted",
                MessageContractSourceFactory.CompleteOrderPayload,
                form,
                NamespaceContracts)),
        Pair(
            "namespaced-contract-incompatible",
            form => MessageContractSourceFactory.Publish(
                "Messages.OrderSubmitted",
                RootIdIncompatiblePayload,
                form,
                NamespaceContracts),
            Incompatible("OrderSubmitted", "Id")),
        Pair(
            "nullable-property-missing",
            form => MessageContractSourceFactory.Publish(
                "OrderSubmitted",
                "new { Id = NewId.NextGuid(), Quantity = 10 }",
                form,
                NullableContract),
            Missing("OrderSubmitted", "Price"),
            Init("Price", "default(decimal?)")),
        Single(
            "case-insensitive-nullable-property-missing",
            form => MessageContractSourceFactory.Publish(
                "OrderSubmitted",
                "new { Id = NewId.NextGuid(), quantity = 10 }",
                form,
                NullableContract),
            Missing("OrderSubmitted", "Price"),
            Init("Price", "default(decimal?)")),
        Pair(
            "publish-empty-message",
            form => MessageContractSourceFactory.PublishOrder("new { }", form),
            Missing("OrderSubmitted", "Id", "CustomerId", "OrderItems")),
        Pair(
            "consumer-send-missing",
            form => MessageContractSourceFactory.ConsumerSend(
                "new { Id = context.Message.Id, CustomerId = context.Message.CustomerId }",
                form),
            Missing("OrderSubmitted", "OrderItems")),
        Pair(
            "endpoint-send-empty-message",
            form => MessageContractSourceFactory.EndpointSend("SubmitOrder", "new { }", form, MessageContractSourceFactory.OrderContracts),
            Missing("SubmitOrder", "Id", "CustomerId", "OrderItems")),
        Pair(
            "simple-array-missing",
            form => MessageContractSourceFactory.Publish(
                "OrderSubmitted",
                "new { Id = NewId.NextGuid(), CustomerId = \"Customer\" }",
                form,
                SimpleArrayContract),
            Missing("OrderSubmitted", "OrderItems"),
            Init("OrderItems", "new[] { default(Guid) }")),
        Pair(
            "simple-array-complete",
            form => MessageContractSourceFactory.Publish(
                "OrderSubmitted",
                "new { Id = NewId.NextGuid(), CustomerId = \"Customer\", OrderItems = Array.Empty<Guid>() }",
                form,
                SimpleArrayContract)),
        Single(
            "message-data-generic-compatible",
            form => MessageContractSourceFactory.Publish(
                "DataMessage",
                "new { CorrelationId = NewId.NextGuid(), Dictionary = new DataDictionary { Values = new Dictionary<string, string>() } }",
                form,
                MessageDataContract)),
        Pair(
            "simple-array-incompatible",
            form => MessageContractSourceFactory.Publish(
                "OrderSubmitted",
                "new { Id = NewId.NextGuid(), CustomerId = \"Customer\", OrderItems = Array.Empty<int>() }",
                form,
                SimpleArrayContract),
            Incompatible("OrderSubmitted", "OrderItems")),
        Pair(
            "concrete-contract-compatible",
            form => MessageContractSourceFactory.Publish(
                "OrderDto",
                MessageContractSourceFactory.CompleteOrderPayload,
                form,
                ConcreteContract)),
        Pair(
            "multiple-nodes-missing",
            form => MessageContractSourceFactory.PublishOrder(MissingNestedOrderPayload, form),
            Missing("OrderSubmitted", "CustomerId", "OrderItems.Product.Category", "OrderItems.Price"),
            Init("CustomerId", "default(string)"),
            Init("OrderItems.Product.Category", "default(Uri)"),
            Init("OrderItems.Price", "default(decimal)")),
        Pair(
            "nested-array-missing",
            form => MessageContractSourceFactory.PublishOrder(
                "new { Id = NewId.NextGuid(), CustomerId = \"Customer\" }",
                form),
            Missing("OrderSubmitted", "OrderItems"),
            Init("OrderItems.Id", "default(Guid)"),
            Init("OrderItems.Product.Name", "default(string)"),
            Init("OrderItems.Product.Category", "default(Uri)"),
            Init("OrderItems.Quantity", "default(int)"),
            Init("OrderItems.Price", "default(decimal)")),
        Pair(
            "nested-product-missing",
            form => MessageContractSourceFactory.PublishOrder(OrderWithoutProductPayload, form),
            Missing("OrderSubmitted", "OrderItems.Product"),
            Init("OrderItems.Product.Name", "default(string)"),
            Init("OrderItems.Product.Category", "default(Uri)")),
        Pair(
            "root-customer-id-missing",
            form => MessageContractSourceFactory.PublishOrder(OrderWithoutCustomerPayload, form),
            Missing("OrderSubmitted", "CustomerId"),
            Init("CustomerId", "default(string)")),
        Pair(
            "nested-price-missing",
            form => MessageContractSourceFactory.PublishOrder(OrderWithoutPricePayload, form),
            Missing("OrderSubmitted", "OrderItems.Price"),
            Init("OrderItems.Price", "default(decimal)")),
        Pair(
            "nested-category-missing",
            form => MessageContractSourceFactory.PublishOrder(OrderWithoutCategoryPayload, form),
            Missing("OrderSubmitted", "OrderItems.Product.Category"),
            Init("OrderItems.Product.Category", "default(Uri)")),
        Pair(
            "standard-order-complete",
            form => MessageContractSourceFactory.PublishOrder(MessageContractSourceFactory.CompleteOrderPayload, form)),
        Pair(
            "async-expression-compatible",
            form => MessageContractSourceFactory.PublishOrder(
                MessageContractSourceFactory.CompleteOrderPayload.Replace(
                    "Id = NewId.NextGuid()",
                    "Id = await Task.FromResult(NewId.NextGuid())",
                    StringComparison.Ordinal),
                form)),
        Pair(
            "header-property-compatible",
            form => MessageContractSourceFactory.PublishOrder(OrderWithHeaderPayload, form)),
        Pair(
            "root-id-incompatible",
            form => MessageContractSourceFactory.PublishOrder(RootIdIncompatiblePayload, form),
            Incompatible("OrderSubmitted", "Id")),
        Pair(
            "multiple-nodes-incompatible",
            form => MessageContractSourceFactory.PublishOrder(MultipleIncompatiblePayload, form),
            Incompatible("OrderSubmitted", "OrderItems.Product.Category", "OrderItems.Price")),
        Pair(
            "nested-price-incompatible",
            form => MessageContractSourceFactory.PublishOrder(
                MessageContractSourceFactory.CompleteOrderPayload.Replace("Price = 10.0m", "Price = 10.0", StringComparison.Ordinal),
                form),
            Incompatible("OrderSubmitted", "OrderItems.Price")),
        Pair(
            "nested-category-incompatible",
            form => MessageContractSourceFactory.PublishOrder(
                MessageContractSourceFactory.CompleteOrderPayload.Replace("Category = \"category:general\"", "Category = 1", StringComparison.Ordinal),
                form),
            Incompatible("OrderSubmitted", "OrderItems.Product.Category")),
        Pair(
            "unknown-root-property",
            form => MessageContractSourceFactory.PublishOrder(OrderWithUnknownRootPayload, form),
            Incompatible("OrderSubmitted", "Amount")),
        Pair(
            "unknown-order-item-property",
            form => MessageContractSourceFactory.PublishOrder(
                MessageContractSourceFactory.CompleteOrderPayload.Replace("Price = 10.0m", "Price = 10.0m, Amount = 100.0m", StringComparison.Ordinal),
                form),
            Incompatible("OrderSubmitted", "OrderItems.Amount")),
        Pair(
            "unknown-product-property",
            form => MessageContractSourceFactory.PublishOrder(
                MessageContractSourceFactory.CompleteOrderPayload.Replace(
                    "Category = \"category:general\"",
                    "Category = \"category:general\", Price = 10.0m",
                    StringComparison.Ordinal),
                form),
            Incompatible("OrderSubmitted", "OrderItems.Product.Price")),
        Pair(
            "leaf-variables-compatible",
            form => MessageContractSourceFactory.EndpointSend(
                "SubmitOrder",
                CompleteOrderWithVariables,
                form,
                MessageContractSourceFactory.OrderContracts,
                VariableSetup)),
        Pair(
            "enumerable-contract-compatible",
            form => MessageContractSourceFactory.EndpointSend(
                "SubmitOrderEnumerable",
                MessageContractSourceFactory.CompleteOrderPayload,
                form,
                EnumerableContract)),
        Pair(
            "record-contract-compatible",
            form => MessageContractSourceFactory.EndpointSend(
                "OrderSubmissionReceived",
                "new { Id = NewId.NextGuid(), CustomerId = \"Customer\" }",
                form,
                RecordContract)),
        Pair(
            "leaf-variable-incompatible",
            form => MessageContractSourceFactory.EndpointSend(
                "SubmitOrder",
                IncompatibleOrderWithVariables,
                form,
                MessageContractSourceFactory.OrderContracts,
                VariableSetup),
            Incompatible("SubmitOrder", "Id")),
        Pair(
            "recursive-contract-missing",
            form => MessageContractSourceFactory.Publish("Foo", "new { }", form, RecursiveContract),
            Missing("Foo", "Children", "Bar"),
            Init("Children.Children", "Array.Empty<Foo>()"),
            Init("Children.Bar", "new { }"),
            Init("Bar", "new { }")),
        Single(
            "object-erased-generic-publish",
            _ => ObjectErasedGenericPublish()),
        Single(
            "custom-extension-method",
            _ => CustomPublishBack()),
    ];

    public static MessageContractScenario Get(string key) =>
        All.Single(scenario => string.Equals(scenario.Key, key, StringComparison.Ordinal));

    private static MessageContractScenario Pair(
        string key,
        Func<MessageSourceForm, string> sourceFactory,
        ExpectedMessageContractDiagnostic? diagnostic = null,
        params ExpectedInitializer[] addedInitializers) =>
        new(
            key,
            true,
            sourceFactory,
            diagnostic,
            addedInitializers);

    private static MessageContractScenario Single(
        string key,
        Func<MessageSourceForm, string> sourceFactory,
        ExpectedMessageContractDiagnostic? diagnostic = null,
        params ExpectedInitializer[] addedInitializers) =>
        new(key, false, sourceFactory, diagnostic, addedInitializers);

    private static ExpectedMessageContractDiagnostic Missing(string contract, params string[] properties) =>
        new(
            MissingId,
            DiagnosticSeverity.Info,
            $"Message values for contract '{contract}' are missing properties: {string.Join(", ", properties)}");

    private static ExpectedMessageContractDiagnostic Incompatible(string contract, params string[] properties) =>
        new(
            IncompatibleId,
            DiagnosticSeverity.Error,
            $"Message values do not map to contract '{contract}'; incompatible properties: {string.Join(", ", properties)}");

    private static ExpectedInitializer Init(string path, string expression) => new(path, expression);

    private static string InferredOrder(MessageSourceForm form, bool missing, bool incompatible)
    {
        var payload = missing
            ? "new { order.Id, OrderItems = order.OrderItems.Select(item => new { item.Id, Product = new { item.Product.Name }, item.Quantity }).ToList() }"
            : "new { order.Id, order.CustomerId, OrderItems = order.OrderItems.Select(item => new { item.Id, Product = new { item.Product.Name, item.Product.Category }, item.Quantity, item.Price }).ToList() }";
        var category = incompatible ? "1" : "\"category:general\"";
        var setup = $$"""
            var order = new OrderDto
            {
                Id = NewId.NextGuid(),
                CustomerId = "Customer",
                OrderItems =
                {
                    new OrderItemDto
                    {
                        Id = NewId.NextGuid(),
                        Product = new ProductDto { Name = "Product", Category = {{category}} },
                        Quantity = 10,
                        Price = 10.0m
                    }
                }
            };
            """;

        return MessageContractSourceFactory.Publish(
            "OrderSubmitted",
            payload,
            form,
            MessageContractSourceFactory.OrderContracts + (incompatible ? IncompatibleOrderDtos : OrderDtos),
            setup);
    }

    private static string ObjectErasedGenericPublish() => MessageContractSourceFactory.Usings + $$"""
        namespace ConsoleApplication1
        {
            public interface INotification
            {
                Guid StreamId { get; }
            }

            class Program
            {
                static Task PublishNotification<T>(object message) where T : class, INotification
                {
                    var bus = Bus.Factory.CreateUsingInMemory(cfg => { });
                    return bus.PublishAsync<T>(message);
                }

                static Task Run() => PublishNotification<INotification>(
                    {{MessageContractSourceFactory.Mark("new { StreamId = Guid.NewGuid() }")}});
            }
        }
        """;

    private static string CustomPublishBack() => MessageContractSourceFactory.Usings + $$"""
        namespace ConsoleApplication1
        {
            public interface INotification;
            public interface INotified;

            public class NotificationConsumer : IConsumer<INotification>
            {
                public Task ConsumeAsync(ConsumeContext<INotification> context)
                {
                    var message = {{MessageContractSourceFactory.Mark("new { }")}};
                    return context.PublishBack<INotified>(message);
                }
            }

            public static class Extensions
            {
                public static Task PublishBack<TMessage>(this ConsumeContext<INotification> context, object message)
                    where TMessage : class => context.Advanced().PublishAsync<TMessage>(message);
            }
        }
        """;

    private const string MissingNestedOrderPayload = """
        new
        {
            Id = NewId.NextGuid(),
            OrderItems = new[]
            {
                new
                {
                    Id = NewId.NextGuid(),
                    Product = new { Name = "Product" },
                    Quantity = 10
                }
            }
        }
        """;

    private const string OrderWithoutProductPayload = """
        new
        {
            Id = NewId.NextGuid(),
            CustomerId = "Customer",
            OrderItems = new[]
            {
                new { Id = NewId.NextGuid(), Quantity = 10, Price = 10.0m }
            }
        }
        """;

    private const string OrderWithoutCustomerPayload = """
        new
        {
            Id = NewId.NextGuid(),
            OrderItems = new[]
            {
                new
                {
                    Id = NewId.NextGuid(),
                    Product = new { Name = "Product", Category = "category:general" },
                    Quantity = 10,
                    Price = 10.0m
                }
            }
        }
        """;

    private const string OrderWithoutPricePayload = """
        new
        {
            Id = NewId.NextGuid(),
            CustomerId = "Customer",
            OrderItems = new[]
            {
                new
                {
                    Id = NewId.NextGuid(),
                    Product = new { Name = "Product", Category = "category:general" },
                    Quantity = 10
                }
            }
        }
        """;

    private const string OrderWithoutCategoryPayload = """
        new
        {
            Id = NewId.NextGuid(),
            CustomerId = "Customer",
            OrderItems = new[]
            {
                new
                {
                    Id = NewId.NextGuid(),
                    Product = new { Name = "Product" },
                    Quantity = 10,
                    Price = 10.0m
                }
            }
        }
        """;

    private const string RootIdIncompatiblePayload = """
        new
        {
            Id = 42,
            CustomerId = "Customer",
            OrderItems = new[]
            {
                new
                {
                    Id = NewId.NextGuid(),
                    Product = new { Name = "Product", Category = "category:general" },
                    Quantity = 10,
                    Price = 10.0m
                }
            }
        }
        """;

    private const string MultipleIncompatiblePayload = """
        new
        {
            Id = NewId.NextGuid(),
            CustomerId = 27,
            OrderItems = new[]
            {
                new
                {
                    Id = NewId.NextGuid(),
                    Product = new { Name = "Product", Category = 1 },
                    Quantity = 10,
                    Price = 10.0
                }
            }
        }
        """;

    private const string CompleteOrderWithVariables = """
        new
        {
            Id,
            CustomerId,
            OrderItems = new[]
            {
                new
                {
                    Id = ItemId,
                    Product = new { Name, Category },
                    Quantity,
                    Price
                }
            }
        }
        """;

    private const string IncompatibleOrderWithVariables = """
        new
        {
            Id = Timestamp,
            CustomerId,
            OrderItems = new[]
            {
                new
                {
                    Id = ItemId,
                    Product = new { Name, Category },
                    Quantity,
                    Price
                }
            }
        }
        """;

    private const string VariableSetup = """
        var Id = NewId.NextGuid();
        var Timestamp = NewId.Next().Timestamp;
        var CustomerId = "Customer";
        var ItemId = NewId.NextGuid();
        var Name = "Product";
        var Category = "category:general";
        var Quantity = 10;
        var Price = 10.0m;
        """;

    private const string OrderWithHeaderPayload = """
        new
        {
            __TimeToLive = 15000,
            Id = NewId.NextGuid(),
            CustomerId = "Customer",
            OrderItems = new[]
            {
                new
                {
                    Id = NewId.NextGuid(),
                    Product = new { Name = "Product", Category = "category:general" },
                    Quantity = 10,
                    Price = 10.0m
                }
            }
        }
        """;

    private const string OrderWithUnknownRootPayload = """
        new
        {
            Id = NewId.NextGuid(),
            CustomerId = "Customer",
            OrderItems = new[]
            {
                new
                {
                    Id = NewId.NextGuid(),
                    Product = new { Name = "Product", Category = "category:general" },
                    Quantity = 10,
                    Price = 10.0m
                }
            },
            Amount = 100.0m
        }
        """;
}
