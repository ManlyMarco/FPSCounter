using System;
using System.Runtime.InteropServices;

namespace FPSCounter
{
    /// <summary>
    /// Provides information about system memory status
    /// </summary>
    internal static class MemoryInfo
    {
        private static readonly IntPtr _currentProcessHandle = GetCurrentProcess();
        private static readonly MEMORYSTATUSEX _memorystatusex = new MEMORYSTATUSEX();
        private static readonly PROCESS_MEMORY_COUNTERS _memoryCounters = new PROCESS_MEMORY_COUNTERS();

        public static MEMORYSTATUSEX QuerySystemMemStatus()
        {
            if (GlobalMemoryStatusEx(_memorystatusex))
                return _memorystatusex;

            throw new Exception("GlobalMemoryStatusEx returned false. Error Code is " + Marshal.GetLastWin32Error());
        }

        public static PROCESS_MEMORY_COUNTERS QueryProcessMemStatus()
        {
            if (GetProcessMemoryInfo(_currentProcessHandle, _memoryCounters, _memoryCounters.cb))
                return _memoryCounters;

            throw new Exception("GetProcessMemoryInfo returned false. Error Code is " + Marshal.GetLastWin32Error());
        }

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx([In] [Out] MEMORYSTATUSEX lpBuffer);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentProcess();

        [DllImport("psapi.dll", SetLastError = true)]
        private static extern bool GetProcessMemoryInfo(IntPtr hProcess, [In] [Out] PROCESS_MEMORY_COUNTERS counters, uint size);

        /// <summary>
        /// contains information about the current state of both physical and virtual memory, including extended memory
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class MEMORYSTATUSEX
        {
            /// <summary>
            /// Size of the structure, in bytes. You must set this member before calling GlobalMemoryStatusEx.
            /// </summary>
            public uint dwLength;

            /// <summary>
            /// Number between 0 and 100 that specifies the approximate percentage of physical memory that is in use (0 indicates
            /// no memory use and 100 indicates full memory use).
            /// </summary>
            public uint dwMemoryLoad;

            /// <summary>
            /// Total size of physical memory, in bytes.
            /// </summary>
            public ulong ullTotalPhys;

            /// <summary>
            /// Size of physical memory available, in bytes.
            /// </summary>
            public ulong ullAvailPhys;

            /// <summary>
            /// Size of the committed memory limit, in bytes. This is physical memory plus the size of the page file, minus a small
            /// overhead.
            /// </summary>
            public ulong ullTotalPageFile;

            /// <summary>
            /// Size of available memory to commit, in bytes. The limit is ullTotalPageFile.
            /// </summary>
            public ulong ullAvailPageFile;

            /// <summary>
            /// Total size of the user mode portion of the virtual address space of the calling process, in bytes.
            /// </summary>
            public ulong ullTotalVirtual;

            /// <summary>
            /// Size of unreserved and uncommitted memory in the user mode portion of the virtual address space of the calling
            /// process, in bytes.
            /// </summary>
            public ulong ullAvailVirtual;

            /// <summary>
            /// Size of unreserved and uncommitted memory in the extended portion of the virtual address space of the calling
            /// process, in bytes.
            /// </summary>
            public ulong ullAvailExtendedVirtual;

            /// <summary>
            /// Initializes a new instance of the <see cref="T:MEMORYSTATUSEX" /> class.
            /// </summary>
            public MEMORYSTATUSEX()
            {
                dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            }
        }

        [StructLayout(LayoutKind.Sequential, Size = 72)]
        public class PROCESS_MEMORY_COUNTERS
        {
            /// <summary>
            /// The size of the structure, in bytes (DWORD).
            /// </summary>
            public uint cb;

            /// <summary>
            /// The number of page faults (DWORD).
            /// </summary>
            public uint PageFaultCount;

            /// <summary>
            /// The peak working set size, in bytes (SIZE_T).
            /// </summary>
            public ulong PeakWorkingSetSize;

            /// <summary>
            /// The current working set size, in bytes (SIZE_T).
            /// </summary>
            public ulong WorkingSetSize;

            /// <summary>
            /// The peak paged pool usage, in bytes (SIZE_T).
            /// </summary>
            public ulong QuotaPeakPagedPoolUsage;

            /// <summary>
            /// The current paged pool usage, in bytes (SIZE_T).
            /// </summary>
            public ulong QuotaPagedPoolUsage;

            /// <summary>
            /// The peak nonpaged pool usage, in bytes (SIZE_T).
            /// </summary>
            public ulong QuotaPeakNonPagedPoolUsage;

            /// <summary>
            /// The current nonpaged pool usage, in bytes (SIZE_T).
            /// </summary>
            public ulong QuotaNonPagedPoolUsage;

            /// <summary>
            /// The Commit Charge value in bytes for this process (SIZE_T). Commit Charge is the total amount of memory that the
            /// memory manager has committed for a running process.
            /// </summary>
            public ulong PagefileUsage;

            /// <summary>
            /// The peak value in bytes of the Commit Charge during the lifetime of this process (SIZE_T).
            /// </summary>
            public ulong PeakPagefileUsage;

            public PROCESS_MEMORY_COUNTERS()
            {
                cb = (uint)Marshal.SizeOf(typeof(PROCESS_MEMORY_COUNTERS));
            }
        }
    }
}