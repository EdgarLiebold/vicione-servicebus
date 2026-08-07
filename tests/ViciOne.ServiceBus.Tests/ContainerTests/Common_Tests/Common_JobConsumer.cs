// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Tests.ContainerTests.Common_Tests
{
    using System;
    using System.Threading.Tasks;
    using JobConsumerComponents;
    using JobConsumerContracts;
    using ViciOne.ServiceBus.Contracts.JobService;
    using NUnit.Framework;
    using TestFramework;


    public class Common_JobConsumer :
        InMemoryContainerTestFixture
    {
        [Test]
        public async Task Should_resolve_an_use_the_service()
        {
            IRequestClient<CrunchTheNumbers> client = GetRequestClient<CrunchTheNumbers>();

            Response<JobSubmissionAccepted> response = await client.GetResponse<JobSubmissionAccepted>(new { Duration = TimeSpan.FromSeconds(1) });

            var expected = new Uri(InMemoryTestHarness.BaseAddress, KebabCaseEndpointNameFormatter.Instance.Consumer<CrunchTheNumbersConsumer>());
            Assert.That(response.SourceAddress, Is.EqualTo(expected));
        }

        protected override void ConfigureInMemoryBus(IInMemoryBusFactoryConfigurator configurator)
        {
            var options = new ServiceInstanceOptions()
                .EnableJobServiceEndpoints();

            configurator.ConfigureServiceEndpoints(BusRegistrationContext, options);
        }

        protected override void ConfigureViciOneServiceBus(IBusRegistrationConfigurator configurator)
        {
            configurator.SetKebabCaseEndpointNameFormatter();

            configurator.AddConsumersFromNamespaceContaining<CrunchTheNumbersConsumer>();

            configurator.AddRequestClient<CrunchTheNumbers>();
        }
    }


    namespace JobConsumerContracts
    {
        public interface CrunchTheNumbers
        {
            TimeSpan Duration { get; }
        }
    }


    namespace JobConsumerComponents
    {
        using JobConsumerContracts;


        public class CrunchTheNumbersConsumer :
            IJobConsumer<CrunchTheNumbers>
        {
            public async Task Run(JobContext<CrunchTheNumbers> context)
            {
                await Task.Delay(context.Job.Duration);
            }
        }
    }
}
