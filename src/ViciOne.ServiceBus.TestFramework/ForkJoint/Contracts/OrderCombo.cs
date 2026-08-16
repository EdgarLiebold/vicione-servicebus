namespace ViciOne.ServiceBus.TestFramework.ForkJoint.Contracts
{
    public interface OrderCombo :
        OrderLine
    {
        int Number { get; }
    }
}
