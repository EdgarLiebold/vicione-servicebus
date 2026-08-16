namespace ViciOne.ServiceBus.TestFramework.ForkJoint.Contracts
{
    public interface OrderBurger :
        OrderLine
    {
        Burger Burger { get; }
    }
}
