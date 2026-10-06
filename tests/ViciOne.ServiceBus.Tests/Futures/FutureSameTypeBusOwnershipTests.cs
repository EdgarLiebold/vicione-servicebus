using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Futures;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Futures;

public sealed class FutureSameTypeBusOwnershipTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-REGISTRATION", "same-future-type-dispatches-to-each-selected-bus-companion")]
    public async Task SameFutureType_DispatchesToTheCompanionOwnedByEachTypedBusAsync()
    {
        string unique = NewId.NextGuid().ToString("N");
        var services = new ServiceCollection();
        var observations = new CompanionObservations();
        services.AddSingleton(observations);
        services.AddViciOneServiceBus(registration =>
        {
            registration.Limits(MessageLimits.Conservative);
            registration.UsingInMemory((_, _) => { });
        });
        services.AddViciOneServiceBus<IFirstFutureBus>(registration =>
        {
            registration.Limits(MessageLimits.Conservative);
            registration.SetInMemorySagaRepositoryProvider();
            registration.AddFutureRequestConsumer<RequestConsumerFuture<Command, Reply>, FirstCompanion, Command, Reply>();
            registration.UsingInMemory((context, bus) =>
            {
                bus.Host(new Uri($"loopback://localhost/same-future-first-{unique}"));
                bus.ConfigureEndpoints(context);
            });
        });
        services.AddViciOneServiceBus<ISecondFutureBus>(registration =>
        {
            registration.Limits(MessageLimits.Conservative);
            registration.SetInMemorySagaRepositoryProvider();
            registration.AddFutureRequestConsumer<RequestConsumerFuture<Command, Reply>, SecondCompanion, Command, Reply>();
            registration.UsingInMemory((context, bus) =>
            {
                bus.Host(new Uri($"loopback://localhost/same-future-second-{unique}"));
                bus.ConfigureEndpoints(context);
            });
        });
        await using ServiceProvider provider = services.BuildServiceProvider(validateScopes: true);
        IBusControl first = Assert.IsAssignableFrom<IBusControl>(provider.GetRequiredService<IFirstFutureBus>());
        IBusControl second = Assert.IsAssignableFrom<IBusControl>(provider.GetRequiredService<ISecondFutureBus>());
        var firstDefinition = provider.GetRequiredService<Bind<IFirstFutureBus,
            IFutureDefinition<RequestConsumerFuture<Command, Reply>>>>().Value;
        var secondDefinition = provider.GetRequiredService<Bind<ISecondFutureBus,
            IFutureDefinition<RequestConsumerFuture<Command, Reply>>>>().Value;
        Uri firstEndpoint = new(first.Address, firstDefinition.GetEndpointName(DefaultEndpointNameFormatter.Instance));
        Uri secondEndpoint = new(second.Address, secondDefinition.GetEndpointName(DefaultEndpointNameFormatter.Instance));
        Assert.NotEqual(firstEndpoint, secondEndpoint);
        using var operations = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        operations.CancelAfter(Timeout);
        Guid firstId = NewId.NextGuid();
        Guid secondId = NewId.NextGuid();
        try
        {
            await first.StartAsync(operations.Token);
            await second.StartAsync(operations.Token);
            Reply firstReply = await RequestAsync(first, firstEndpoint, new Command(firstId), operations.Token);
            Assert.Equal(new Reply(firstId, "first"), firstReply);
            Assert.Equal(new[] { new Reply(firstId, "first") }, observations.Messages.ToArray());

            Reply secondReply = await RequestAsync(second, secondEndpoint, new Command(secondId), operations.Token);
            Assert.Equal(new Reply(secondId, "second"), secondReply);
            Assert.Equal(new[] { new Reply(firstId, "first"), new Reply(secondId, "second") }, observations.Messages.ToArray());
        }
        finally
        {
            operations.Cancel();
            await Task.WhenAll(first.StopAsync(CancellationToken.None), second.StopAsync(CancellationToken.None))
                .WaitAsync(Timeout, CancellationToken.None);
        }
    }

    private static async Task<Reply> RequestAsync(IBus bus, Uri endpoint, Command command, CancellationToken cancellationToken)
    {
        IRequestClient<Command> client = bus.CreateRequestClient<Command>(endpoint, new RequestTimeout(Timeout));
        using RequestHandle<Command> request = client.Create(command, cancellationToken: cancellationToken);
        Task<Response<Reply>> response = request.GetResponseAsync<Reply>(cancellationToken: cancellationToken);
        try
        {
            await Task.WhenAll(request.Message, response);
            return (await response).Message;
        }
        catch (RequestFaultException exception)
        {
            var details = new List<string>();
            foreach (ExceptionInfo root in exception.Fault?.Exceptions ?? [])
            {
                ExceptionInfo? current = root;
                for (int depth = 0; current is not null && depth < 32; depth++)
                {
                    details.Add($"[{depth}] {current.ExceptionType}: {current.Message}\n{current.StackTrace}");
                    current = current.InnerException;
                }
            }

            throw new InvalidOperationException(
                $"Actual request fault at '{endpoint}' for '{command.CorrelationId}':\n"
                    + string.Join(Environment.NewLine, details),
                exception);
        }
    }

    public interface IFirstFutureBus : IBus;
    public interface ISecondFutureBus : IBus;
    public sealed record Command(Guid CorrelationId) : ICorrelatedBy<Guid>
    {
        public Command() : this(Guid.Empty) { }
    }

    public sealed record Reply(Guid CorrelationId, string Owner)
    {
        public Reply() : this(Guid.Empty, string.Empty) { }
    }

    public sealed class CompanionObservations
    {
        public ConcurrentQueue<Reply> Messages { get; } = new();
    }

    public sealed class FirstCompanion(CompanionObservations observations) : IConsumer<Command>
    {
        public Task ConsumeAsync(ConsumeContext<Command> context)
        {
            var reply = new Reply(context.Message.CorrelationId, "first");
            observations.Messages.Enqueue(reply);
            return context.Advanced().RespondAsync(reply);
        }
    }

    public sealed class SecondCompanion(CompanionObservations observations) : IConsumer<Command>
    {
        public Task ConsumeAsync(ConsumeContext<Command> context)
        {
            var reply = new Reply(context.Message.CorrelationId, "second");
            observations.Messages.Enqueue(reply);
            return context.Advanced().RespondAsync(reply);
        }
    }
}
