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
            ProcStub.InitArgs(ref args);

            var stub = new ProcStub(new DummyProc("procstub-showcase"));
            var stub2 = new ProcStub(new DummyProc("procstub-showcase2"));

            // test whether this needs to run as service. only one will execute and then exit Main().
            if (stub.RunService()) return;
            if (stub2.RunService()) return;

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