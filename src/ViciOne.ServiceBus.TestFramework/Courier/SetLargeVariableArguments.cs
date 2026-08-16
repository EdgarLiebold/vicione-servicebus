namespace ViciOne.ServiceBus.TestFramework.Courier
{
    public interface SetLargeVariableArguments
    {
        string Key { get; }
        MessageData<string> Value { get; }
    }
}
