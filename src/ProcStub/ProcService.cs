using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.AccessControl;
using System.ServiceProcess;
using System.Threading;

namespace ProcStub
{
    public class ProcService : IDisposable
    {
        private static readonly IList<ProcWinService> AllServices = new List<ProcWinService>();

        private static bool? _isService;

        private ProcService(string serviceName, IProc proc)
        {
            ServiceName = serviceName;
            Proc = proc;

            // defaults
            ServiceAccess = ServiceAccess.ServiceAllAccess;
            ServiceType = ServiceTypes.ServiceWin32OwnProcess;
            ServiceStart = ServiceStart.ServiceAutoStart;
            ServiceError = ServiceError.ServiceErrorNormal;

            Path = Assembly.GetEntryAssembly().Location;
        }

        public string ServiceName { get; private set; }
        public string DisplayName { get; set; }
        public string Path { get; set; }
        public string Params { get; set; }
        public IProc Proc { get; private set; }

        public ServiceAccess ServiceAccess { get; set; }
        public ServiceTypes ServiceType { get; set; }
        public ServiceStart ServiceStart { get; set; }
        public ServiceError ServiceError { get; set; }
        public string Username { get; set; }
        public SecureString Password { get; set; }
        public IEnumerable<string> Dependencies { get; set; }

        public ServiceControllerStatus? ServiceStatus
        {
            get
            {
                using (var controller = new ServiceController(ServiceName))
                {
                    try
                    {
                        return controller.Status;
                    }
                    catch (InvalidOperationException)
                    {
                        return null;
                    }
                }
            }
        }

        public static bool IsServiceHost
        {
            get
            {
                if (_isService == null)
                {
                    _isService = ParentProcessUtilities.GetParentProcess().ProcessName == "services";
                }

                return (bool) _isService;
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (Password != null)
            {
                Password.Dispose();
                Password = null;
            }
        }

        #endregion

        public static ProcService Register(string serviceName, IProc proc)
        {
            var procStub = new ProcService(serviceName, proc);

            AllServices.Add(new ProcWinService(procStub));

            return procStub;
        }

        public bool Install()
        {
            string path = "\"" + Path + "\"";

            if (!string.IsNullOrEmpty(Params))
            {
                path += " " + Params;
            }

            string dep = null;

            if (Dependencies != null)
            {
                string[] deps = Dependencies.ToArray();

                if (deps.Length > 0)
                {
                    dep = "";

                    foreach (string d in deps)
                        dep += d + "\0";

                    dep += "\0";
                }
            }

            string strPassword = null;
            IntPtr ptrPassword = IntPtr.Zero;

            if (Password != null)
            {
                ptrPassword = Marshal.SecureStringToBSTR(Password);
            }

            try
            {
                if (ptrPassword != IntPtr.Zero)
                    strPassword = Marshal.PtrToStringBSTR(ptrPassword);

                return ServiceWrapper.CreateService(ServiceName, DisplayName, ServiceAccess, ServiceType, ServiceStart,
                                                    ServiceError, path, null, null, dep, Username, strPassword);
            }
            finally
            {
                if (ptrPassword != IntPtr.Zero)
                    Marshal.ZeroFreeBSTR(ptrPassword);
            }
        }

        public bool Uninstall()
        {
            return ServiceWrapper.DeleteService(ServiceName);
        }

        public bool Start()
        {
            using (var controller = new ServiceController(ServiceName))
            {
                try
                {
                    controller.Start();
                    return true;
                }
                catch (InvalidOperationException)
                {
                    return false;
                }
            }
        }

        public bool Stop()
        {
            using (var controller = new ServiceController(ServiceName))
            {
                try
                {
                    controller.Stop();
                    return true;
                }
                catch (InvalidOperationException)
                {
                    return false;
                }
            }
        }

        public bool SetAcl(Action<DiscretionaryAcl> applySecurityIdentifier)
        {
            using (var controller = new ServiceController(ServiceName))
            {
                return controller.SetAcl(applySecurityIdentifier);
            }
        }

        public bool AwaitStatus(ServiceControllerStatus status, CancellationToken ct)
        {
            while (!ct.WaitHandle.WaitOne(100))
            {
                if (ServiceStatus == null || ServiceStatus == status)
                    return true;
            }

            return false;
        }

        public static bool RunServices()
        {
            if (!IsServiceHost) return false;

            ServiceBase.Run(AllServices.ToArray());

            return true;
        }
    }
}