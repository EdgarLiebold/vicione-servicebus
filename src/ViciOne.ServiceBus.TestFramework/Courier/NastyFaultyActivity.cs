// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.TestFramework.Courier
{
    using System;
    using System.Threading.Tasks;


    public class NastyFaultyActivity :
        IActivity<FaultyArguments, FaultyLog>
    {
        public Task<ExecutionResult> Execute(ExecuteContext<FaultyArguments> context)
        {
            Console.WriteLine("NastyFaultyActivity: Execute");
            Console.WriteLine("NastyFaultyActivity: About to blow this up!");

            throw new InvalidOperationException("Things that make you go boom!");
        }

        public async Task<CompensationResult> Compensate(CompensateContext<FaultyLog> context)
        {
            return context.Compensated();
        }
    }
}
