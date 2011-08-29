using System;
using System.ServiceProcess;
using System.Threading;

namespace ProcStub
{
    public class ProcService : ServiceBase
    {
        private readonly IProc _proc;
        private readonly Thread _thread;
        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();

        public ProcService(IProc proc)
        {
            _proc = proc;
            _thread = new Thread(Impl) {IsBackground = true};
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
            _proc.Run(_tokenSource.Token);
        }
    }
}