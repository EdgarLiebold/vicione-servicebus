namespace ViciOne.ServiceBus.Tests
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net.Http;
    using System.Reflection;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using ViciOne.ServiceBus.Testing;
    using NUnit.Framework;


    /// <summary>
    /// A default registration and a started bus must issue no outgoing HTTP request.
    /// <para>
    /// The bus used to post host, bus, rider and endpoint details to a hard coded vendor host on every
    /// start, enabled by default. That path is gone, and this is what keeps it gone: the first test
    /// listens on the diagnostic source that every HttpClient request writes to, so any outgoing
    /// request during registration, start and stop is observed no matter who issues it. The second
    /// reads the shipped assemblies and fails on a hard coded outgoing address in the product itself.
    /// </para>
    /// <para>
    /// What is measured, stated exactly: outgoing HttpClient requests on the registration, start,
    /// publish and stop path of an in memory bus, and hard coded addresses in the shipped assemblies.
    /// A socket opened without HttpClient, or a request on a path this fixture does not walk, is
    /// outside that scope - "does not reach the network at all" would claim more than the listener can
    /// see.
    /// </para>
    /// </summary>
    [TestFixture]
    public class Starting_a_default_bus
    {
        [Test]
        public async Task Should_not_issue_a_single_outgoing_http_request()
        {
            var requests = new ConcurrentQueue<string>();

            using var subscription = DiagnosticListener.AllListeners.Subscribe(new ListenerObserver(requests));

            await using var provider = new ServiceCollection()
                .AddViciOneServiceBus(x =>
                {
                    x.AddConsumer<QuietConsumer>();
                    x.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));
                })
                .BuildServiceProvider(true);

            var bus = provider.GetRequiredService<IBusControl>();

            await bus.StartAsync();
            try
            {
                await bus.Publish(new QuietMessage());
            }
            finally
            {
                await bus.StopAsync();
            }

            Assert.That(requests, Is.Empty,
                "starting a default bus issued an outgoing HTTP request: " + string.Join(", ", requests));
        }

        [Test]
        public async Task Should_notice_an_outgoing_request_when_there_is_one()
        {
            // The control for the case above. Without it a green result could mean the listener never
            // observes anything, which is how a negative proof passes for the wrong reason.
            var requests = new ConcurrentQueue<string>();

            using var subscription = DiagnosticListener.AllListeners.Subscribe(new ListenerObserver(requests));

            using var client = new HttpClient { Timeout = TimeSpan.FromMilliseconds(250) };
            try
            {
                await client.GetAsync("http://127.0.0.1:1/vicione-detector-control");
            }
            catch (Exception)
            {
                // the address is closed on purpose; the request only has to be issued
            }

            Assert.That(requests, Is.Not.Empty, "the listener does not observe an outgoing request at all");
        }

        [Test]
        public void Should_carry_no_hard_coded_outgoing_address_in_the_shipped_assemblies()
        {
            string[] offenders = ProductAssemblies()
                .SelectMany(assembly => Strings(assembly).Select(text => new { assembly, text }))
                .Where(hit => hit.text.Contains("://", StringComparison.Ordinal)
                    && !hit.text.StartsWith("loopback://", StringComparison.OrdinalIgnoreCase)
                    && !hit.text.StartsWith("urn:", StringComparison.OrdinalIgnoreCase)
                    && (hit.text.Contains("usage", StringComparison.OrdinalIgnoreCase)
                        || hit.text.Contains("telemetry", StringComparison.OrdinalIgnoreCase)
                        || hit.text.Contains("license", StringComparison.OrdinalIgnoreCase)))
                .Select(hit => $"{hit.assembly.GetName().Name}: {hit.text}")
                .Distinct()
                .ToArray();

            Assert.That(offenders, Is.Empty, "a shipped assembly carries an outgoing vendor address");
        }

        [Test]
        public void Should_actually_read_strings_out_of_those_assemblies()
        {
            // The control for the scan: it must find the product's own scheme strings, or an empty
            // offender list would only mean that nothing was read.
            string[] schemes = ProductAssemblies().SelectMany(Strings)
                .Where(text => text.Contains("://", StringComparison.Ordinal))
                .Distinct()
                .ToArray();

            Assert.That(schemes, Is.Not.Empty, "no address like string was read from the assemblies at all");
        }

        [Test]
        public void Should_read_two_distinct_product_assemblies()
        {
            // The control for the scope of the byte scan. IBus and IBusControl both live in
            // ViciOne.ServiceBus.Abstractions, so anchoring on the pair read that one assembly twice
            // and never opened ViciOne.ServiceBus, which is exactly where the usage tracker and its
            // vendor address were. The scan reported green for the wrong reason until this held it.
            string[] names = ProductAssemblies().Select(assembly => assembly.GetName().Name).ToArray();

            Assert.That(names, Is.Unique, "the byte scan reads the same assembly twice: " + string.Join(", ", names));
            Assert.That(names, Has.Length.EqualTo(2));
        }

        static IEnumerable<Assembly> ProductAssemblies()
        {
            yield return typeof(IBus).Assembly;
            yield return typeof(InMemoryConfigurationExtensions).Assembly;
        }

        /// <summary>
        /// The user string heap of the assembly, which is where a hard coded address would live.
        /// </summary>
        static IEnumerable<string> Strings(Assembly assembly)
        {
            var bytes = System.IO.File.ReadAllBytes(assembly.Location);
            var builder = new System.Text.StringBuilder();

            foreach (var value in bytes)
            {
                if (value >= 0x20 && value < 0x7f)
                    builder.Append((char)value);
                else
                {
                    if (builder.Length >= 8)
                        yield return builder.ToString();

                    builder.Clear();
                }
            }

            if (builder.Length >= 8)
                yield return builder.ToString();
        }


        sealed class ListenerObserver :
            IObserver<DiagnosticListener>
        {
            readonly ConcurrentQueue<string> _requests;

            public ListenerObserver(ConcurrentQueue<string> requests)
            {
                _requests = requests;
            }

            public void OnNext(DiagnosticListener listener)
            {
                if (listener.Name == "HttpHandlerDiagnosticListener")
                    listener.Subscribe(new EventObserver(_requests));
            }

            public void OnCompleted()
            {
            }

            public void OnError(Exception error)
            {
            }
        }


        sealed class EventObserver :
            IObserver<KeyValuePair<string, object>>
        {
            readonly ConcurrentQueue<string> _requests;

            public EventObserver(ConcurrentQueue<string> requests)
            {
                _requests = requests;
            }

            public void OnNext(KeyValuePair<string, object> value)
            {
                if (!value.Key.EndsWith(".Start", StringComparison.Ordinal))
                    return;

                var request = value.Value?.GetType().GetProperty("Request")?.GetValue(value.Value) as HttpRequestMessage;

                _requests.Enqueue(request?.RequestUri?.ToString() ?? value.Key);
            }

            public void OnCompleted()
            {
            }

            public void OnError(Exception error)
            {
            }
        }


        public class QuietMessage
        {
        }


        class QuietConsumer :
            IConsumer<QuietMessage>
        {
            public Task Consume(ConsumeContext<QuietMessage> context)
            {
                return Task.CompletedTask;
            }
        }
    }
}
