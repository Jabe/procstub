using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.ServiceProcess;

namespace ProcStub
{
    public static class ServiceWrapper
    {
        private const int ErrorInsufficientBuffer = 0x007A;
        private const uint ServiceConfigDescription = 0x01;

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

        [DllImport("advapi32.dll", EntryPoint = "ChangeServiceConfig2W", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ChangeServiceConfig2(
            IntPtr hService,
            uint dwInfoLevel,
            [In] ServiceDescription config);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseServiceHandle(
            IntPtr hSCObject);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool QueryServiceObjectSecurity(
            SafeHandle serviceHandle, SecurityInfos secInfo, byte[] lpSecDesrBuf, uint bufSize, out uint bufSizeNeeded);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceObjectSecurity(
            SafeHandle serviceHandle, SecurityInfos secInfos, byte[] lpSecDesrBuf);

        public static bool CreateService(string serviceName, string displayName, ServiceAccess access,
            ServiceTypes type, ServiceStart start, ServiceError error, string path,
            string orderGroup, string tagId, string dep, string username, string password, string server = null)
        {
            IntPtr manager = IntPtr.Zero;
            IntPtr service = IntPtr.Zero;

            try
            {
                manager = OpenSCManager(server, null, ScmAccess.ScManagerAllAccess);

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

        public static bool DeleteService(string serviceName, string server = null)
        {
            IntPtr manager = IntPtr.Zero;
            IntPtr service = IntPtr.Zero;

            try
            {
                manager = OpenSCManager(server, null, ScmAccess.ScManagerAllAccess);

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

        public static bool ServiceExists(string serviceName, string server = null)
        {
            IntPtr manager = IntPtr.Zero;
            IntPtr service = IntPtr.Zero;

            try
            {
                manager = OpenSCManager(server, null, ScmAccess.ScManagerAllAccess);

                if (manager != IntPtr.Zero)
                {
                    service = OpenService(manager, serviceName, ServiceAccess.ServiceAllAccess);

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

        public static bool SetServiceDescription(string serviceName, string description, string server = null)
        {
            IntPtr manager = IntPtr.Zero;
            IntPtr service = IntPtr.Zero;

            try
            {
                manager = OpenSCManager(server, null, ScmAccess.ScManagerAllAccess);

                if (manager != IntPtr.Zero)
                {
                    service = OpenService(manager, serviceName, ServiceAccess.ServiceAllAccess);

                    if (service != IntPtr.Zero)
                    {
                        var config = new ServiceDescription
                        {
                            lpDescription = description
                        };

                        return ChangeServiceConfig2(service, ServiceConfigDescription, config);
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

        public static bool SetAcl(this ServiceController controller, Action<DiscretionaryAcl> fn)
        {
            // from http://pinvoke.net/default.aspx/advapi32/QueryServiceObjectSecurity.html (thx!)

            using (SafeHandle handle = controller.ServiceHandle)
            {
                var psd = new byte[0];
                uint bufSizeNeeded;

                bool success = QueryServiceObjectSecurity(handle, SecurityInfos.DiscretionaryAcl, psd, 0,
                    out bufSizeNeeded);

                if (!success)
                {
                    int error = Marshal.GetLastWin32Error();

                    if (error == ErrorInsufficientBuffer)
                    {
                        psd = new byte[bufSizeNeeded];
                        success = QueryServiceObjectSecurity(handle, SecurityInfos.DiscretionaryAcl, psd, bufSizeNeeded,
                            out bufSizeNeeded);
                    }
                    else
                    {
                        return false;
                    }
                }

                if (!success)
                {
                    return false;
                }

                // get security descriptor via raw into DACL form so ACE
                // ordering checks are done for us.
                var rsd = new RawSecurityDescriptor(psd, 0);
                var dacl = new DiscretionaryAcl(false, false, rsd.DiscretionaryAcl);

                fn(dacl);

                // convert discretionary ACL back to raw form; looks like via byte[] is only way
                var rawdacl = new byte[dacl.BinaryLength];
                dacl.GetBinaryForm(rawdacl, 0);
                rsd.DiscretionaryAcl = new RawAcl(rawdacl, 0);

                // set raw security descriptor on service again
                var rawsd = new byte[rsd.BinaryLength];
                rsd.GetBinaryForm(rawsd, 0);

                success = SetServiceObjectSecurity(handle, SecurityInfos.DiscretionaryAcl, rawsd);

                return success;
            }
        }

        [Flags]
        private enum ScmAccess : uint
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

        [StructLayout(LayoutKind.Sequential)]
        public class ServiceDescription
        {
            [MarshalAs(UnmanagedType.LPWStr)] public string lpDescription;
        }
    }
}