using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace ProcStub
{
    public static class ServiceWrapper
    {
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr CreateService(
            IntPtr hSCManager,
            string lpServiceName,
            string lpDisplayName,
            uint dwDesiredAccess,
            uint dwServiceType,
            uint dwStartType,
            uint dwErrorControl,
            string lpBinaryPathName,
            string lpLoadOrderGroup,
            string lpdwTagId,
            string lpDependencies,
            string lpServiceStartName,
            string lpPassword);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteService(
            IntPtr hService);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr OpenService(
            IntPtr hSCManager,
            string lpServiceName,
            ServiceAccess dwDesiredAccess);

        [DllImport("advapi32.dll", EntryPoint = "OpenSCManagerW", ExactSpelling = true, CharSet = CharSet.Unicode,
            SetLastError = true)]
        private static extern IntPtr OpenSCManager(
            string machineName,
            string databaseName,
            ScmAccess dwDesiredAccess);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseServiceHandle(
            IntPtr hSCObject);

        public static bool CreateService(string serviceName, string displayName, ServiceAccess access,
                                         ServiceTypes type, ServiceStart start, ServiceError error, string path,
                                         string orderGroup, string tagId, string dep, string username, string password)
        {
            IntPtr manager = IntPtr.Zero;
            IntPtr service = IntPtr.Zero;

            try
            {
                manager = OpenSCManager(null, null, ScmAccess.ScManagerAllAccess);

                if (manager != IntPtr.Zero)
                {
                    service = CreateService(manager, serviceName, displayName, (uint) access, (uint) type, (uint) start,
                                            (uint) error, path, orderGroup, tagId, dep, username, password);

                    if (service != IntPtr.Zero)
                    {
                        return true;
                    }
                }
            }
            finally
            {
                if (service != IntPtr.Zero) CloseServiceHandle(service);
                if (manager != IntPtr.Zero) CloseServiceHandle(manager);
            }

            return false;
        }

        public static bool DeleteService(string serviceName)
        {
            IntPtr manager = IntPtr.Zero;
            IntPtr service = IntPtr.Zero;

            try
            {
                manager = OpenSCManager(null, null, ScmAccess.ScManagerAllAccess);

                if (manager != IntPtr.Zero)
                {
                    service = OpenService(manager, serviceName, ServiceAccess.ServiceAllAccess);

                    if (service != IntPtr.Zero)
                    {
                        return DeleteService(service);
                    }
                }
            }
            finally
            {
                if (service != IntPtr.Zero) CloseServiceHandle(service);
                if (manager != IntPtr.Zero) CloseServiceHandle(manager);
            }

            return false;
        }

        #region Nested type: ScmAccess

        [Flags]
        internal enum ScmAccess : uint
        {
            StandardRightsRequired = 0xF0000,
            ScManagerConnect = 0x00001,
            ScManagerCreateService = 0x00002,
            ScManagerEnumerateService = 0x00004,
            ScManagerLock = 0x00008,
            ScManagerQueryLockStatus = 0x00010,
            ScManagerModifyBootConfig = 0x00020,

            ScManagerAllAccess = StandardRightsRequired |
                                 ScManagerConnect |
                                 ScManagerCreateService |
                                 ScManagerEnumerateService |
                                 ScManagerLock |
                                 ScManagerQueryLockStatus |
                                 ScManagerModifyBootConfig
        }

        #endregion
    }
}