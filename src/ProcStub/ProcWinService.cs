using System;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

namespace ProcStub
{
    internal class ProcWinService : ServiceBase
    {
        private readonly ProcService _procService;
        private readonly Thread _thread;
        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();

        public ProcWinService(ProcService procService)
        {
            _procService = procService;
            _thread = new Thread(Impl) {IsBackground = true};

            // copy to parent object for scm
            ServiceName = _procService.ServiceName;
        }

        protected override void OnStart(string[] args)
        {
            _thread.Start();

            base.OnStart(args);
        }

        protected override void OnStop()
        {
            _tokenSource.Cancel();
            _thread.Join();

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
            _procService.Proc.Run(_tokenSource.Token);

            // proc ended, we should stop the service as well.
            Task.Factory.StartNew(Stop);
        }
    }
}