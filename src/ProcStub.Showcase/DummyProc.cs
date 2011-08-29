using System;
using System.Threading;

namespace ProcStub.Showcase
{
    public class DummyProc : ProcBase
    {
        public DummyProc(string serviceName) : base(serviceName)
        {
        }

        public override void Run(CancellationToken token)
        {
            Console.WriteLine("-> Run");

            for (int i = 0; i < 100000; i++)
            {
                Console.WriteLine("Hello, my name is " + ServiceName);

                // wait for signal and break -or- 100 ms and continue
                if (token.WaitHandle.WaitOne(100))
                {
                    Console.WriteLine("Cancelled");
                    break;
                }
            }

            Console.WriteLine("<- Run");
        }
    }
}