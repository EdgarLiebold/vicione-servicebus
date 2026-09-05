using Microsoft.EntityFrameworkCore;
using ViciOne.ServiceBus.EntityFrameworkCore;

namespace ViciOne.ServiceBus.Samples.SuiteComposition;

internal sealed record GetSuiteStatus(string Name);

internal sealed record SuiteStatus(string Value);

internal sealed record SuiteDeadline(string Value);

internal sealed class SuiteConsumer(TaskCompletionSource<string> observedSchedule) :
    IConsumer<GetSuiteStatus>,
    IConsumer<SuiteDeadline>
{
    public Task ConsumeAsync(ConsumeContext<GetSuiteStatus> context) =>
        context.RespondAsync(new SuiteStatus($"ready:{context.Message.Name}"));

    public Task ConsumeAsync(ConsumeContext<SuiteDeadline> context)
    {
        observedSchedule.TrySetResult(context.Message.Value);
        return Task.CompletedTask;
    }
}

internal sealed class SuiteDbContext(DbContextOptions<SuiteDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.AddViciOneReliableMessaging();
}
