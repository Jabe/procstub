using System;
using System.Threading;

namespace ProcStub
{
    public abstract class ProcBase : IProc
    {
        protected ProcBase(string serviceName)
        {
            ServiceName = serviceName;
        }

        #region IProc Members

        public string ServiceName { get; private set; }

        public void Run()
        {
            Run(CancellationToken.None);
        }

        public abstract void Run(CancellationToken token);

        #endregion
    }
}