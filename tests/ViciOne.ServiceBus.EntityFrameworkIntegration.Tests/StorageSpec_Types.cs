// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.EntityFrameworkIntegration.Tests
{
    using System;


    /// <summary>
    /// Why to exit the door to go shopping
    /// </summary>
    public class GirlfriendYelling
    {
        public Guid CorrelationId { get; set; }
    }


    public class GotHitByACar
    {
        public Guid CorrelationId { get; set; }
    }


    public class SodOff
    {
        public Guid CorrelationId { get; set; }
    }


    public class ShoppingChore :
        SagaStateMachineInstance
    {
        protected ShoppingChore()
        {
        }

        public ShoppingChore(Guid correlationId)
        {
            CorrelationId = correlationId;
        }

        public string CurrentState { get; set; }
        public int Everything { get; set; }
        public bool Screwed { get; set; }

        public Guid CorrelationId { get; set; }
    }


    public sealed class SuperShopper :
        ViciOneServiceBusStateMachine<ShoppingChore>
    {
        public SuperShopper()
        {
            InstanceState(x => x.CurrentState);

            Event(() => ExitFrontDoor, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => GotHitByCar, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => JustSodOff, x => x.CorrelateById(context => context.Message.CorrelationId));

            CompositeEvent(() => EndOfTheWorld, x => x.Everything, CompositeEventOptions.IncludeInitial, ExitFrontDoor, GotHitByCar);

            Initially(
                When(ExitFrontDoor)
                    .Then(context => Console.Write("Leaving!"))
                    .TransitionTo(OnTheWayToTheStore));

            During(OnTheWayToTheStore,
                When(GotHitByCar)
                    .Then(context => Console.WriteLine("Ouch!!"))
                    .TransitionTo(Dead));

            DuringAny(
                When(EndOfTheWorld)
                    .Then(context => Console.WriteLine("Screwed!!"))
                    .Then(context => context.Instance.Screwed = true));

            DuringAny(
                When(JustSodOff)
                    .Finalize());

            SetCompletedWhenFinalized();
        }

        public Event<GirlfriendYelling> ExitFrontDoor { get; private set; }
        public Event<GotHitByACar> GotHitByCar { get; private set; }
        public Event<SodOff> JustSodOff { get; private set; }
        public Event EndOfTheWorld { get; private set; }
        public State OnTheWayToTheStore { get; private set; }
        public State Dead { get; private set; }
    }
}
