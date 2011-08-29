using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProcStub.Showcase
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            ProcStub stub = ProcStub.Register("procstub-showcase", new DummyProc());
            ProcStub stub2 = ProcStub.Register("procstub-showcase2", new DummyProc());

            stub.ServiceType = ServiceTypes.ServiceWin32ShareProcess;
            stub2.ServiceType = ServiceTypes.ServiceWin32ShareProcess;

            if (ProcStub.RunServices())
                return;

            Console.WriteLine("ProcStub showcase");
            Console.WriteLine("Commands: 'r' to run, 'i' to install as server, 'u' to uninstall, 'q' to quit");

            while (true)
            {
                string line = Console.ReadLine();

                if (string.IsNullOrEmpty(line))
                    continue;

                switch (line[0])
                {
                    case 'r':
                        using (var s = new CancellationTokenSource())
                        {
                            Task.Factory.StartNew(() => stub.Proc.Run(s.Token));

                            Console.WriteLine("Press enter to simulate stopping the service");
                            Console.ReadLine();
                            s.Cancel();
                        }
                        break;
                    case 'i':
                        stub.Install();
                        stub2.Install();
                        break;
                    case 'u':
                        stub.Uninstall();
                        stub2.Uninstall();
                        break;
                    case 'q':
                        return;
                }
            }
        }
    }
}