using System;

namespace ProcStub
{
    [Flags]
    public enum ServiceTypes : uint
    {
        ServiceKernelDriver = 0x00000001,
        ServiceFileSystemDriver = 0x00000002,
        ServiceWin32OwnProcess = 0x00000010,
        ServiceWin32ShareProcess = 0x00000020,
        ServiceInteractiveProcess = 0x00000100
    }
}