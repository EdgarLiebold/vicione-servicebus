namespace ViciOne.ServiceBus.TestFramework.ForkJoint.Contracts
{
    public interface OrderCalculate :
        OrderLine
    {
        int Number { get; }
    }
}
