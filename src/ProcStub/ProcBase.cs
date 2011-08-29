using System;
using System.Threading;

namespace ProcStub
{
    public abstract class ProcBase : IProc
    {
        #region IProc Members

        public void Run()
        {
            Run(CancellationToken.None);
        }

        public abstract void Run(CancellationToken token);

        #endregion
    }
}