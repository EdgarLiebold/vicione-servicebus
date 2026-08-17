namespace ViciOne.ServiceBus.Tests.ContainerTests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using ViciOne.ServiceBus.DependencyInjection;
    using NUnit.Framework;


    /// <summary>
    /// Two buses registered at once must each keep their own scoped consume context. The two are told
    /// apart by deliberately different markers: the bus interfaces differ, the message types differ and
    /// the payload each message carries differs, so a swapped context is visible as a wrong marker rather
    /// than as a missing one.
    /// <para>
    /// Cross contamination is what these tests look for, and they can see it: while one bus is consuming,
    /// the consumer reads the scope provider of the *other* bus as well and reports what it holds. A
    /// shared or untyped provider would show the consuming bus's message in the other bus's provider, and
    /// the assertion names which direction it leaked.
    /// </para>
    /// </summary>
    [TestFixture]
    public class Registering_two_buses_with_different_scope_providers
    {
        [Test]
        public void Should_give_each_bus_its_own_scope_provider()
        {
            using var provider = Build(new Reports());
            using var scope = provider.CreateScope();

            var untyped = scope.ServiceProvider.GetRequiredService<IScopedConsumeContextProvider>();
            var alpha = scope.ServiceProvider.GetRequiredService<Bind<IBusAlpha, IScopedConsumeContextProvider>>().Value;
            var beta = scope.ServiceProvider.GetRequiredService<Bind<IBusBeta, IScopedConsumeContextProvider>>().Value;

            Assert.Multiple(() =>
            {
                Assert.That(alpha, Is.Not.SameAs(beta), "both buses share one scope provider");
                Assert.That(alpha, Is.Not.SameAs(untyped), "the first bus uses the untyped provider directly");
                Assert.That(beta, Is.Not.SameAs(untyped), "the second bus uses the untyped provider directly");
            });
        }

        [Test]
        public void Should_give_each_bus_its_own_bound_setter()
        {
            using var provider = Build(new Reports());

            var alpha = provider.GetRequiredService<Bind<IBusAlpha, ISetScopedConsumeContext>>().Value;
            var beta = provider.GetRequiredService<Bind<IBusBeta, ISetScopedConsumeContext>>().Value;

            Assert.That(alpha, Is.Not.SameAs(beta), "both buses share one bound setter");
        }

        [Test]
        public async Task Should_not_leak_a_consume_context_into_the_other_bus()
        {
            var reports = new Reports();

            await using var provider = Build(reports);

            var alpha = provider.GetRequiredService<IBusAlpha>();
            var beta = provider.GetRequiredService<IBusBeta>();

            await ((IBusControl)alpha).StartAsync(CancellationToken.None);
            await ((IBusControl)beta).StartAsync(CancellationToken.None);
            try
            {
                await alpha.Publish(new AlphaMessage { Marker = "alpha-marker" }, CancellationToken.None);

                var seen = await reports.Alpha.Task.WaitAsync(TimeSpan.FromSeconds(30));

                Assert.Multiple(() =>
                {
                    Assert.That(seen.Own, Is.EqualTo("alpha-marker"),
                        "the consuming bus did not find its own message on its own scope provider");
                    Assert.That(seen.Other, Is.EqualTo(Nothing),
                        "the message of the consuming bus leaked into the other bus's scope provider");
                });
            }
            finally
            {
                await ((IBusControl)beta).StopAsync(CancellationToken.None);
                await ((IBusControl)alpha).StopAsync(CancellationToken.None);
            }
        }

        [Test]
        public async Task Should_not_leak_in_the_other_direction_either()
        {
            var reports = new Reports();

            await using var provider = Build(reports);

            var alpha = provider.GetRequiredService<IBusAlpha>();
            var beta = provider.GetRequiredService<IBusBeta>();

            await ((IBusControl)alpha).StartAsync(CancellationToken.None);
            await ((IBusControl)beta).StartAsync(CancellationToken.None);
            try
            {
                await beta.Publish(new BetaMessage { Marker = "beta-marker" }, CancellationToken.None);

                var seen = await reports.Beta.Task.WaitAsync(TimeSpan.FromSeconds(30));

                Assert.Multiple(() =>
                {
                    Assert.That(seen.Own, Is.EqualTo("beta-marker"),
                        "the consuming bus did not find its own message on its own scope provider");
                    Assert.That(seen.Other, Is.EqualTo(Nothing),
                        "the message of the consuming bus leaked into the other bus's scope provider");
                });
            }
            finally
            {
                await ((IBusControl)beta).StopAsync(CancellationToken.None);
                await ((IBusControl)alpha).StopAsync(CancellationToken.None);
            }
        }

        const string Nothing = "nothing";

        static ServiceProvider Build(Reports reports)
        {
            var collection = new ServiceCollection();

            // One singleton carrying both completion sources. Registering two TaskCompletionSource<string>
            // instances instead would leave only the last one resolvable, and every consumer would report
            // into the same source.
            collection.AddSingleton(reports);

            collection.AddViciOneServiceBus<IBusAlpha>(x =>
            {
                x.AddConsumer<AlphaConsumer>();
                x.UsingInMemory((context, cfg) =>
                {
                    cfg.Host(new Uri("loopback://alpha/"));
                    cfg.ConfigureEndpoints(context);
                });
            });

            collection.AddViciOneServiceBus<IBusBeta>(x =>
            {
                x.AddConsumer<BetaConsumer>();
                x.UsingInMemory((context, cfg) =>
                {
                    cfg.Host(new Uri("loopback://beta/"));
                    cfg.ConfigureEndpoints(context);
                });
            });

            return collection.BuildServiceProvider(true);
        }

        static string Read<T>(IScopedConsumeContextProvider provider)
            where T : class, IMarked
        {
            var context = provider.GetContext();
            if (context == null)
                return Nothing;

            return context.TryGetMessage<T>(out var message) ? message.Message.Marker : $"other:{context.GetType().Name}";
        }


        public interface IBusAlpha :
            IBus
        {
        }


        public interface IBusBeta :
            IBus
        {
        }


        public interface IMarked
        {
            string Marker { get; }
        }


        public class AlphaMessage :
            IMarked
        {
            public string Marker { get; set; }
        }


        public class BetaMessage :
            IMarked
        {
            public string Marker { get; set; }
        }


        public class Observation
        {
            public string Own { get; init; }
            public string Other { get; init; }
        }


        public class Reports
        {
            public TaskCompletionSource<Observation> Alpha { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
            public TaskCompletionSource<Observation> Beta { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        }


        class AlphaConsumer :
            IConsumer<AlphaMessage>
        {
            readonly Bind<IBusAlpha, IScopedConsumeContextProvider> _own;
            readonly Bind<IBusBeta, IScopedConsumeContextProvider> _other;
            readonly Reports _reports;

            public AlphaConsumer(Bind<IBusAlpha, IScopedConsumeContextProvider> own, Bind<IBusBeta, IScopedConsumeContextProvider> other,
                Reports reports)
            {
                _own = own;
                _other = other;
                _reports = reports;
            }

            public Task Consume(ConsumeContext<AlphaMessage> context)
            {
                _reports.Alpha.TrySetResult(new Observation
                {
                    Own = Read<AlphaMessage>(_own.Value),
                    Other = Read<AlphaMessage>(_other.Value)
                });

                return Task.CompletedTask;
            }
        }


        class BetaConsumer :
            IConsumer<BetaMessage>
        {
            readonly Bind<IBusAlpha, IScopedConsumeContextProvider> _other;
            readonly Bind<IBusBeta, IScopedConsumeContextProvider> _own;
            readonly Reports _reports;

            public BetaConsumer(Bind<IBusBeta, IScopedConsumeContextProvider> own, Bind<IBusAlpha, IScopedConsumeContextProvider> other,
                Reports reports)
            {
                _own = own;
                _other = other;
                _reports = reports;
            }

            public Task Consume(ConsumeContext<BetaMessage> context)
            {
                _reports.Beta.TrySetResult(new Observation
                {
                    Own = Read<BetaMessage>(_own.Value),
                    Other = Read<BetaMessage>(_other.Value)
                });

                return Task.CompletedTask;
            }
        }
    }
}
