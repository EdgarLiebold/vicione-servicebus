// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.TestFramework.Courier
{
    public interface SetLargeVariableArguments
    {
        string Key { get; }
        MessageData<string> Value { get; }
    }
}
