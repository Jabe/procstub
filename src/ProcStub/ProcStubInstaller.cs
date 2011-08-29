using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace ProcStub
{
    [RunInstaller(true)]
    public sealed class ProcStubInstaller : Installer
    {
        public override void Install(IDictionary stateSaver)
        {
            CallForMagicUnicorns();
            base.Install(stateSaver);
        }

        public override void Rollback(IDictionary savedState)
        {
            CallForMagicUnicorns();
            base.Rollback(savedState);
        }

        public override void Uninstall(IDictionary savedState)
        {
            CallForMagicUnicorns();
            base.Uninstall(savedState);
        }

        private void CallForMagicUnicorns()
        {
            if (Installers.Count > 0)
                return;

            string serviceName = Context.Parameters["servicename"];

            var processInstaller = new ServiceProcessInstaller {Account = ServiceAccount.LocalSystem};
            Installers.Add(processInstaller);

            var installer = new ServiceInstaller
                                {
                                    Description = "",
                                    DisplayName = "",
                                    ServiceName = serviceName,
                                    StartType = ServiceStartMode.Automatic,
                                };

            processInstaller.Installers.Add(installer);
        }
    }
}