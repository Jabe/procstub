using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace ProcStub.Showcase
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            ProcService srv1 = ProcService.Register("procstub-showcase1", new DummyProc());
            ProcService srv2 = ProcService.Register("procstub-showcase2", new DummyProc());

            srv1.ServiceType = ServiceTypes.ServiceWin32ShareProcess;
            srv2.ServiceType = ServiceTypes.ServiceWin32ShareProcess;

            if (ProcService.RunServices())
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
                            Task.Factory.StartNew(() => srv1.Proc.Run(s.Token));

                            Console.WriteLine("Press enter to simulate stopping the service");
                            Console.ReadLine();
                            s.Cancel();
                        }
                        break;
                    case 'i':
                        srv1.Install();
                        srv2.Install();

                        srv1.SetAcl(AuthUserStartStop);
                        srv2.SetAcl(AuthUserStartStop);
                        break;
                    case 'u':
                        srv1.Uninstall();
                        srv2.Uninstall();
                        break;
                    case 'q':
                        return;
                }
            }
        }

        private static void AuthUserStartStop(DiscretionaryAcl dacl)
        {
            var sid = new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null);

            dacl.SetAccess(AccessControlType.Allow, sid, (int) (ServiceAccess.ServiceStart | ServiceAccess.ServiceStop),
                           InheritanceFlags.None, PropagationFlags.None);
        }
    }
}