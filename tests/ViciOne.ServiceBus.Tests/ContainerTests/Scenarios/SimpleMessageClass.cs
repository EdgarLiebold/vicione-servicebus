// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Tests.ContainerTests.Scenarios
{
    public class SimpleMessageClass :
        SimpleMessageInterface
    {
        public SimpleMessageClass(string name)
        {
            Name = name;
        }

        public string Name { get; set; }
    }
}
