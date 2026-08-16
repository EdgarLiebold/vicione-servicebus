namespace ViciOne.ServiceBus.TestFramework.ForkJoint.Contracts
{
    public interface BurgerCompleted :
        OrderLineCompleted
    {
        Burger Burger { get; }
    }
}
