namespace ViciOne.ServiceBus.Tests.Courier
{
    using System;
    using System.Text.Json;
    using System.Threading.Tasks;
    using ViciOne.ServiceBus.Courier.Contracts;
    using ViciOne.ServiceBus.Serialization;
    using ViciOne.ServiceBus.Testing;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;
    using Scenario;
    using TestFramework;
    using TestFramework.Courier;


    public static class TestExtensionsForJson
    {
        public static string ToJsonString(this RoutingSlip routingSlip)
        {
            return JsonSerializer.Serialize(routingSlip);
        }

        public static RoutingSlip GetRoutingSlip(string json)
        {
            return JsonSerializer.Deserialize<RoutingSlip>(json, SystemTextJsonMessageSerializer.Options);
        }
    }


    [TestFixture]
    public class A_routing_slip_event_subscription
    {
        [Test]
        public void Should_serialize_properly()
        {
            var builder = new RoutingSlipBuilder(Guid.NewGuid());
            builder.AddActivity("a", new Uri("loopback://locahost/execute_a"));

            builder.AddSubscription(new Uri("loopback://localhost/events"), RoutingSlipEvents.Completed | RoutingSlipEvents.Faulted);

            var routingSlip = builder.Build();

            var jsonString = routingSlip.ToJsonString();

            var loaded = TestExtensionsForJson.GetRoutingSlip(jsonString);

            Assert.That(loaded.Subscriptions[0].Events, Is.EqualTo(RoutingSlipEvents.Completed | RoutingSlipEvents.Faulted));
        }
    }


    [TestFixture]
    public class Subscription_without_variables :
        InMemoryActivityTestFixture
    {
        [Test]
        public async Task Should_not_receive_the_routing_slip_activity_log()
        {
            ConsumeContext<RoutingSlipActivityCompleted> context = await _activityCompleted;

            Assert.That(context.Message.Data.ContainsKey("OriginalValue"), Is.False);
        }

        [Test]
        public async Task Should_not_receive_the_routing_slip_activity_variable()
        {
            ConsumeContext<RoutingSlipActivityCompleted> context = await _activityCompleted;

            Assert.That(context.Message.Variables.ContainsKey("Variable"), Is.False);
        }

        [Test]
        public async Task Should_receive_the_routing_slip_activity_completed_event()
        {
            ConsumeContext<RoutingSlipActivityCompleted> context = await _activityCompleted;

            Assert.That(context.Message.TrackingNumber, Is.EqualTo(_trackingNumber));
        }

        [Test]
        public void Should_serialize_and_deserialize_properly()
        {
            var builder = new RoutingSlipBuilder(Guid.NewGuid());
            builder.AddActivity("a", new Uri("loopback://locahost/execute_a"));

            builder.AddSubscription(new Uri("loopback://localhost/events"), RoutingSlipEvents.Completed | RoutingSlipEvents.Faulted);

            var routingSlip = builder.Build();

            var jsonString = routingSlip.ToJsonString();

            var loaded = TestExtensionsForJson.GetRoutingSlip(jsonString);

            Assert.That(loaded.Subscriptions[0].Events, Is.EqualTo(RoutingSlipEvents.Completed | RoutingSlipEvents.Faulted));
        }

        #pragma warning disable NUnit1032
        Task<ConsumeContext<RoutingSlipActivityCompleted>> _activityCompleted;
        #pragma warning restore NUnit1032
        Guid _trackingNumber;

        [OneTimeSetUp]
        public async Task Should_publish_the_completed_event()
        {
            _ = SubscribeHandler<RoutingSlipCompleted>();
            _activityCompleted = SubscribeHandler<RoutingSlipActivityCompleted>();

            _trackingNumber = NewId.NextGuid();
            var builder = new RoutingSlipBuilder(_trackingNumber);
            builder.AddSubscription(Bus.Address, RoutingSlipEvents.ActivityCompleted, RoutingSlipEventContents.None);

            var testActivity = GetActivityContext<TestActivity>();
            builder.AddActivity(testActivity.Name, testActivity.ExecuteUri, new { Value = "Hello" });

            builder.AddVariable("Variable", "Knife");

            await Bus.Execute(builder.Build());
        }

        protected override void SetupActivities(BusTestHarness testHarness)
        {
            AddActivityContext<TestActivity, TestArguments, TestLog>(() => new TestActivity());
        }
    }


    [TestFixture(typeof(Json))]
    [TestFixture(typeof(RawJson))]
    public class Adding_a_custom_routing_slip_event_subscription<T>
        where T : new()
    {
        [Test]
        public async Task Should_be_sent()
        {
            await using var provider = new ServiceCollection()
                .AddViciOneServiceBusTestHarness(x =>
                {
                    x.AddHandler<RegistrationCompleted>();
                    x.AddActivity<TestActivity, TestArguments, TestLog>();

                    x.UsingInMemory(((context, cfg) =>
                    {
                        _configuration?.ConfigureBus(context, cfg);

                        cfg.ConfigureEndpoints(context);
                    }));
                })
                .BuildServiceProvider(true);

            var harness = await provider.StartTestHarness();

            var trackingNumber = NewId.NextGuid();
            var builder = new RoutingSlipBuilder(trackingNumber);
            await builder.AddSubscription(harness.GetHandlerAddress<RegistrationCompleted>(), RoutingSlipEvents.Completed, RoutingSlipEventContents.All,
                x => x.Send<RegistrationCompleted>(new { Value = "Secret Value" }));

            builder.AddActivity("TestActivity", harness.GetExecuteActivityAddress<TestActivity, TestArguments>(), new { Value = "Hello" });

            await harness.Bus.Execute(builder.Build());

            IReceivedMessage<RegistrationCompleted> completed = await harness.Consumed
                .SelectAsync<RegistrationCompleted>(x => x.Context.Message.TrackingNumber == trackingNumber).FirstOrDefault();
            Assert.That(completed, Is.Not.Null);

            Assert.That(completed.Context.Message.Value, Is.EqualTo("Secret Value"));
        }

        readonly ITestBusConfiguration _configuration;

        public Adding_a_custom_routing_slip_event_subscription()
        {
            _configuration = new T() as ITestBusConfiguration
                ?? throw new ArgumentException(
                    $"{typeof(T).Name} does not implement {nameof(ITestBusConfiguration)}, so this fixture "
                    + "would configure no serializer at all.", nameof(T));
        }


        public interface RegistrationCompleted :
            RoutingSlipCompleted
        {
            string Value { get; }
        }
    }
}
