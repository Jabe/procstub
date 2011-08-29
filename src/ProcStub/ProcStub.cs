using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.ServiceProcess;

namespace ProcStub
{
    public class ProcStub : IDisposable
    {
        private static readonly IList<ProcService> AllServices = new List<ProcService>();

        private static bool? _isService;

        private ProcStub(string serviceName, IProc proc)
        {
            ServiceName = serviceName;
            Proc = proc;

            // defaults
            ServiceAccess = ServiceAccess.ServiceAllAccess;
            ServiceType = ServiceTypes.ServiceWin32OwnProcess;
            ServiceStart = ServiceStart.ServiceAutoStart;
            ServiceError = ServiceError.ServiceErrorNormal;
        }

        public string ServiceName { get; private set; }
        public string DisplayName { get; set; }
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

        public static ProcStub Register(string serviceName, IProc proc)
        {
            var procStub = new ProcStub(serviceName, proc);

            AllServices.Add(new ProcService(procStub));

            return procStub;
        }

        public bool Install()
        {
            string path = "\"" + Proc.GetType().Assembly.Location + "\"";

            string dep = null;

            if (Dependencies != null )
            {
                var deps = Dependencies.ToArray();

                if (deps.Length > 0)
                {
                    dep = "";

                    foreach (var d in deps)
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

        public static bool RunServices()
        {
            if (!IsServiceHost) return false;

            ServiceBase.Run(AllServices.ToArray());

            return true;
        }
    }
}