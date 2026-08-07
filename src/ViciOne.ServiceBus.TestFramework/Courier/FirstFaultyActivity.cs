// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.TestFramework.Courier
{
    using System;
    using System.Threading.Tasks;


    public class FirstFaultyActivity :
        IActivity<FaultyArguments, FaultyLog>
    {
        public async Task<ExecutionResult> Execute(ExecuteContext<FaultyArguments> context)
        {
            Console.WriteLine("FirstFaultyActivity: Execute");

            if (context.GetRetryAttempt() > 0 || context.GetRedeliveryCount() > 0)
                return context.Completed(new { });

            Console.WriteLine("FirstFaultyActivity: About to blow this up!");

            throw new IntentionalTestException("Things that make you go boom!");
        }

        public async Task<CompensationResult> Compensate(CompensateContext<FaultyLog> context)
        {
            return context.Compensated();
        }
    }
}
