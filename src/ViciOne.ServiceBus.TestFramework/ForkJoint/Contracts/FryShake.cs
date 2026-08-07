// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.TestFramework.ForkJoint.Contracts
{
    using System;


    public class FryShake
    {
        public Guid FryShakeId { get; set; }
        public string Flavor { get; set; }
        public Size Size { get; set; }

        public override string ToString()
        {
            return $"{Size} {Flavor} FryShake";
        }
    }
}
