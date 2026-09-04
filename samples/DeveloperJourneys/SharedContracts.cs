using Microsoft.EntityFrameworkCore;
using ViciOne.ServiceBus.EntityFrameworkCoreIntegration;

namespace ViciOne.ServiceBus.Samples.DeveloperJourneys;

public sealed record SubmitOrder(Guid OrderId, Guid CustomerId, string Description);

public sealed record OrderSubmitted(Guid OrderId);

public sealed record GetOrder(Guid OrderId);

public sealed record OrderStatus(Guid OrderId, string Value);

public sealed record LargeOrderDocument(Guid OrderId, MessageData<string> Document);

public interface IOrdersBus : IBus;

public interface IBillingBus : IBus;

public sealed class JourneyDbContext(DbContextOptions<JourneyDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.AddTransactionalOutboxEntities();
        modelBuilder.AddViciOneDurableSender();
    }
}

public sealed class SubmitOrderConsumer : IConsumer<SubmitOrder>
{
    public Task ConsumeAsync(ConsumeContext<SubmitOrder> context) => Task.CompletedTask;
}

public sealed class GetOrderConsumer : IConsumer<GetOrder>
{
    public Task ConsumeAsync(ConsumeContext<GetOrder> context) =>
        context.RespondAsync(new OrderStatus(context.Message.OrderId, "accepted"));
}
