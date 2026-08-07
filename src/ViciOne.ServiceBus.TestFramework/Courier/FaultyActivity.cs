// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.TestFramework.Courier
{
    using System;
    using System.Threading.Tasks;


    public class FaultyActivity :
        IActivity<FaultyArguments, FaultyLog>,
        IDisposable
    {
        public async Task<ExecutionResult> Execute(ExecuteContext<FaultyArguments> context)
        {
            Console.WriteLine("FaultyActivity: Execute");
            Console.WriteLine("FaultyActivity: About to blow this up!");

            throw new IntentionalTestException("Things that make you go boom!");
        }

        public async Task<CompensationResult> Compensate(CompensateContext<FaultyLog> context)
        {
            return context.Compensated();
        }

        public void Dispose()
        {
        }
    }
}
