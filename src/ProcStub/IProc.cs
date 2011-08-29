using System;
using System.Threading;

namespace ProcStub
{
    public interface IProc
    {
        void Run();
        void Run(CancellationToken token);
    }
}