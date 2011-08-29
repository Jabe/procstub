using System;
using System.Threading;

namespace ProcStub
{
    public interface IProc
    {
        string ServiceName { get; }

        void Run();
        void Run(CancellationToken token);
    }
}