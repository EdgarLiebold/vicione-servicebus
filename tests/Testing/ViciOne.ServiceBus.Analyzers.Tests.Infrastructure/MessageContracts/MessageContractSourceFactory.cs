namespace ViciOne.ServiceBus.Tests.Infrastructure.Analyzers.MessageContracts;

/// <summary>Builds small binding programs around a single marked message value.</summary>
public static class MessageContractSourceFactory
{
    public const string TargetMarker = "/* VSB_TARGET */";

    public const string MinimalUsings = """
        using System;
        using System.Collections.Generic;
        using System.Linq;
        using System.Threading.Tasks;

        """;

    public const string Usings = MinimalUsings + """
        using ViciOne.ServiceBus; using ViciOne.ServiceBus.Advanced; using ViciOne.ServiceBus.Advanced.Initializers;

        """;

    public const string OrderContracts = """
        namespace ConsoleApplication1
        {
            public interface OrderSubmitted
            {
                Guid Id { get; }
                string CustomerId { get; }
                IReadOnlyList<OrderItem> OrderItems { get; }
            }

            public interface SubmitOrder
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

            public interface CheckOrderStatus
            {
                Guid OrderId { get; }
            }

            public interface OrderStatusResult
            {
                Guid OrderId { get; }
                string Status { get; }
            }
        }

        """;

    public const string CompleteOrderPayload = """
        new
        {
            Id = NewId.NextGuid(),
            CustomerId = "Customer",
            OrderItems = new[]
            {
                new
                {
                    Id = NewId.NextGuid(),
                    Product = new
                    {
                        Name = "Product",
                        Category = "category:general"
                    },
                    Quantity = 10,
                    Price = 10.0m
                }
            }
        }
        """;

    public static string PublishOrder(string payload, MessageSourceForm form) =>
        Publish("OrderSubmitted", payload, form, OrderContracts);

    public static string Publish(
        string contract,
        string payload,
        MessageSourceForm form,
        string declarations,
        string setup = "") =>
        Usings + declarations + $$"""
        namespace ConsoleApplication1
        {
            class Program
            {
                static async Task Main()
                {
                    var bus = Bus.Factory.CreateUsingInMemory(cfg => { });
        {{Indent(setup, 20)}}
        {{Delivery(form, payload, value => $"await bus.PublishAsync<{contract}>({value});", 20)}}
                }
            }
        }
        """;

    public static string EndpointSend(
        string contract,
        string payload,
        MessageSourceForm form,
        string declarations,
        string setup = "") =>
        Usings + declarations + $$"""
        namespace ConsoleApplication1
        {
            class Program
            {
                static async Task Main()
                {
                    var bus = Bus.Factory.CreateUsingInMemory(cfg => { });
                    var endpoint = await bus.GetSendEndpointAsync(null);
        {{Indent(setup, 20)}}
        {{Delivery(form, payload, value => $"await endpoint.SendAsync<{contract}>({value});", 20)}}
                }
            }
        }
        """;

    public static string ConsumerSend(string payload, MessageSourceForm form) =>
        Usings + OrderContracts + $$"""
        namespace ConsoleApplication1
        {
            class SubmitOrderConsumer : IConsumer<SubmitOrder>
            {
                public async Task ConsumeAsync(ConsumeContext<SubmitOrder> context)
                {
                    Uri address = null;
        {{Delivery(form, payload, value => $"await context.Advanced().SendAsync<OrderSubmitted>(address, {value});", 20)}}
                }
            }
        }
        """;

    public static string CreateRequest(string payload, MessageSourceForm form) =>
        Usings + OrderContracts + $$"""
        namespace ConsoleApplication1
        {
            class Program
            {
                static async Task Main()
                {
                    var bus = Bus.Factory.CreateUsingInMemory(cfg => { });
                    var client = bus.CreateRequestClient<CheckOrderStatus>(null);
        {{Delivery(form, payload, value => $$"""
                    using (var request = client.Create({{value}}))
                    {
                        _ = await request.GetResponseAsync<OrderStatusResult>();
                    }
        """, 20)}}
                }
            }
        }
        """;

    public static string GetResponse(string payload, MessageSourceForm form) =>
        Usings + OrderContracts + $$"""
        namespace ConsoleApplication1
        {
            class Program
            {
                static async Task Main()
                {
                    var bus = Bus.Factory.CreateUsingInMemory(cfg => { });
                    var client = bus.CreateRequestClient<CheckOrderStatus>(null);
        {{Delivery(form, payload, value => $"_ = await client.Advanced().GetResponseAsync<OrderStatusResult>({value});", 20)}}
                }
            }
        }
        """;

    public static string GenericPublish(
        string payload,
        MessageSourceForm form,
        string streamIdType = "Guid") =>
        Usings + $$"""
        namespace ConsoleApplication1
        {
            public interface INotification
            {
                {{streamIdType}} StreamId { get; }
            }

            public interface IProjectionUpdatedNotification : INotification;

            class Program
            {
                static Task PublishNotification<T>(Guid streamId)
                    where T : class, INotification
                {
                    var bus = Bus.Factory.CreateUsingInMemory(cfg => { });
        {{Delivery(form, payload, value => $"return bus.PublishAsync<T>({value});", 20)}}
                }
            }
        }
        """;

    public static string GenericRequest(string payload, MessageSourceForm form) =>
        Usings + """
        namespace ConsoleApplication1
        {
            public interface Up<T> where T : class
            {
                Guid InstanceId { get; }
                Uri InstanceAddress { get; }
            }

            public interface Link<T> where T : class
            {
                Guid ClientId { get; }
            }

        """ + $$"""
            class Program
            {
                static async Task Main<TMessage>() where TMessage : class
                {
                    var bus = Bus.Factory.CreateUsingInMemory(cfg => { });
                    var client = bus.CreateRequestClient<Link<TMessage>>();
        {{Delivery(form, payload, value => $"_ = await client.Advanced().GetResponseAsync<Up<TMessage>>({value});", 20)}}
                }
            }
        }
        """;

    public static string Mark(string payload) => $"{TargetMarker} {payload}";

    private static string Delivery(
        MessageSourceForm form,
        string payload,
        Func<string, string> invocation,
        int indentation) =>
        form switch
        {
            MessageSourceForm.DirectArgument => Indent(invocation(Mark(payload)), indentation),
            MessageSourceForm.LocalVariable =>
                Indent($"var message = {Mark(payload)};\n{invocation("message")}", indentation),
            _ => throw new ArgumentOutOfRangeException(nameof(form), form, null),
        };

    private static string Indent(string value, int spaces)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var padding = new string(' ', spaces);
        return string.Join(
            Environment.NewLine,
            value.Replace("\r\n", "\n", StringComparison.Ordinal)
                .Replace('\r', '\n')
                .Split('\n')
                .Select(line => line.Length == 0 ? string.Empty : padding + line));
    }
}
