// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.TestFramework.Courier
{
    using System;
    using System.Threading.Tasks;


    public class ReviseItineraryActivity :
        IActivity<TestArguments, TestLog>
    {
        readonly Action<IItineraryBuilder> _callback;

        public ReviseItineraryActivity(Action<IItineraryBuilder> callback)
        {
            _callback = callback;
        }

        public async Task<ExecutionResult> Execute(ExecuteContext<TestArguments> context)
        {
            Console.WriteLine("ReviseToEmptyItineraryActivity: Execute: {0}", context.Arguments.Value);

            return context.ReviseItinerary(_callback);
        }

        public async Task<CompensationResult> Compensate(CompensateContext<TestLog> context)
        {
            return context.Compensated();
        }
    }
}
