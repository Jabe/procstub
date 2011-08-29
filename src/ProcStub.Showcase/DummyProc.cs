using System;
using System.Threading;

namespace ProcStub.Showcase
{
    public class DummyProc : ProcBase
    {
        public override void Run(CancellationToken token)
        {
            Console.WriteLine("-> Run");

            while (true)
            {
                // wait for signal and break -or- 100 ms and continue
                if (token.WaitHandle.WaitOne(0))
                {
                    break;
                }
            }

            Console.WriteLine("<- Run");
        }
    }
}