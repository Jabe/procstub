using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace ProcStub.Showcase
{
    public class DummyProc : ProcBase
    {
        public override void Run(CancellationToken token)
        {
            Debug.WriteLine(" proc: running");

            while (!token.WaitHandle.WaitOne(1000))
            {
                Debug.WriteLine(" proc: pong");
            }

            Debug.WriteLine(" proc: stopping (takes 3s)");
            Thread.Sleep(3000);
            Debug.WriteLine(" proc: stopped");
        }
    }
}