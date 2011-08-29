using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration.Install;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;

namespace ProcStub
{
    public class ProcStub
    {
        private static bool? _isService;
        private static bool _initialized;

        private readonly Assembly _actionAssembly;

        public ProcStub(IProc proc)
        {
            CheckInit();

            Proc = proc;
            _actionAssembly = Proc.GetType().Assembly;
        }

        public static string CurrentServiceName { get; set; }

        public IProc Proc { get; private set; }

        public static bool IsService
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

        public ServiceControllerStatus? ServiceStatus
        {
            get
            {
                using (var controller = new ServiceController(Proc.ServiceName))
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

        public static string[] InitArgs(string[] args)
        {
            InitArgs(ref args);
            return args;
        }

        public static void InitArgs(ref string[] args)
        {
            _initialized = true;

            if (!IsService)
                return;

            if (args.Length == 0)
                throw new ArgumentException("Unable to find service name in args.");

            CurrentServiceName = args[0];

            var newArgs = new string[args.Length - 1];
            Array.Copy(args, 1, newArgs, 0, newArgs.Length);
            args = newArgs;
        }

        public void Install()
        {
            var args = new string[0];

            using (AssemblyInstaller installer = GetInstaller(args))
            {
                IDictionary state = new Hashtable();

                try
                {
                    installer.Install(state);
                    installer.Commit(state);
                }
                catch
                {
                    try
                    {
                        installer.Rollback(state);
                    }
                    catch
                    {
                    }

                    throw;
                }
            }
        }

        public void Uninstall()
        {
            var args = new string[0];

            using (AssemblyInstaller installer = GetInstaller(args))
            {
                IDictionary state = new Hashtable();

                try
                {
                    installer.Uninstall(state);
                }
                catch
                {
                    try
                    {
                        installer.Rollback(state);
                    }
                    catch
                    {
                    }

                    throw;
                }
            }
        }

        public bool RunService()
        {
            CheckInit();

            if (!IsService)
                return false;

            if (CurrentServiceName != Proc.ServiceName)
                return false;

            var service = new ProcService(Proc);
            ServiceBase.Run(service);

            return true;
        }

        public bool Start()
        {
            using (var controller = new ServiceController(Proc.ServiceName))
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
            using (var controller = new ServiceController(Proc.ServiceName))
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

        private AssemblyInstaller GetInstaller(string[] args)
        {
            var installer = new AssemblyInstaller(typeof (ProcStubInstaller).Assembly, args)
                                {
                                    Context = new InstallContext(null, args),
                                    UseNewContext = false,
                                };

            string path = string.Format("\"{0}\" \"{1}\"", _actionAssembly.Location, Proc.ServiceName);
            installer.Context.Parameters["assemblypath"] = path;
            installer.Context.Parameters["servicename"] = Proc.ServiceName;

            return installer;
        }

        private void CheckInit()
        {
            if (!_initialized)
                throw new InvalidOperationException("ProcStub has not been initialized. Call InitArgs first.");
        }
    }
}