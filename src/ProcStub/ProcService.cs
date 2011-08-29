using System;
using System.ServiceProcess;
using System.Threading;

namespace ProcStub
{
    public class ProcService : ServiceBase
    {
        private readonly ProcStub _procStub;
        private readonly Thread _thread;
        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();

        public ProcService(ProcStub procStub)
        {
            _procStub = procStub;
            _thread = new Thread(Impl) {IsBackground = true};

            // copy to parent object for scm
            ServiceName = _procStub.ServiceName;
        }

        protected override void OnStart(string[] args)
        {
            _thread.Start();

            base.OnStart(args);
        }

        protected override void OnStop()
        {
            _tokenSource.Cancel();

            base.OnStop();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _tokenSource.Dispose();
            }

            base.Dispose(disposing);
        }

        private void Impl()
        {
            _procStub.Proc.Run(_tokenSource.Token);
        }
    }
}