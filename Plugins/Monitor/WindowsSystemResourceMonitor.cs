using Domain.Interfaces;
using Domain.Models;
using Plugins.Logger;
using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading;

namespace Plugins.Monitors
{
    public class WindowsSystemResourceMonitor : ISystemResourceMonitor
    {
        private TimeSpan _prevTotalProcessorTime;
        private DateTime _prevTime;
        private FileLoggerPlugin _fileLoggerPlugin;

        public WindowsSystemResourceMonitor()
        {
            _prevTotalProcessorTime = Process.GetCurrentProcess().TotalProcessorTime;
            _prevTime = DateTime.UtcNow;
            _fileLoggerPlugin = new FileLoggerPlugin();
        }

        public async Task<SystemMetricsDto> GetMetrics()
        {
            return new SystemMetricsDto
            {
                CpuUsage = await GetCpuUsage(),
                RamUsedMb = await GetRamUsage(),
                DiskUsedMb = await GetDiskUsage()
            };
        }

        private async Task<double> GetCpuUsage()
        {
            try
            {
                var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
                foreach (var queryObj in searcher.Get())
                {
                    var loadPercentage = queryObj["LoadPercentage"];
                    if (loadPercentage != null)
                        return Convert.ToSingle(loadPercentage);
                }
                return 0f;
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error in fetching CPU Usage: {ex.Message}");
                await _fileLoggerPlugin.OtherExceptionCollected($"Error in fetching CPU Usage: {ex.Message}");
                return 0;
            }
        }

        private async Task<double> GetRamUsage()
        {
            try
            {
                MEMORYSTATUSEX memStatus = new MEMORYSTATUSEX();
                if (GlobalMemoryStatusEx(ref memStatus))
                {
                    ulong total = memStatus.ullTotalPhys / (1024 * 1024);
                    ulong avail = memStatus.ullAvailPhys / (1024 * 1024);
                    ulong used = total - avail;
                    return (double)used;
                }
                return 0;
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error in fetching Memory Usage: {ex.Message}");
                await _fileLoggerPlugin.OtherExceptionCollected($"Error in fetching Memory Usage: {ex.Message}");
                return 0;
            }
        }

        private async Task<float> GetDiskUsage()
        {
            try
            {
                DriveInfo drive = new DriveInfo(Path.GetPathRoot(Environment.SystemDirectory));
                if (drive.IsReady)
                {
                    long totalBytes = drive.TotalSize;
                    long freeBytes = drive.AvailableFreeSpace;
                    long usedBytes = totalBytes - freeBytes;

                    float usedMb = usedBytes / (1024f * 1024f);
                    return usedMb;
                }
                return 0;
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error in fetching Disk Usage: {ex.Message}");
                await _fileLoggerPlugin.OtherExceptionCollected($"Error in fetching Disk Usage: {ex.Message}");
                return 0;
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;

            public MEMORYSTATUSEX()
            {
                dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
                dwMemoryLoad = 0;
                ullTotalPhys = 0;
                ullAvailPhys = 0;
                ullTotalPageFile = 0;
                ullAvailPageFile = 0;
                ullTotalVirtual = 0;
                ullAvailVirtual = 0;
                ullAvailExtendedVirtual = 0;
            }
        }
    }
}
