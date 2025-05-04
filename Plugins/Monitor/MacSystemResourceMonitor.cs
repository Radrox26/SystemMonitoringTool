using Domain.Interfaces;
using Domain.Models;
using Plugins.Logger;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Plugins.Monitors
{
    public class MacSystemResourceMonitor : ISystemResourceMonitor
    {
        private FileLoggerPlugin _fileLoggerPlugin;

        public MacSystemResourceMonitor()
        {
            _fileLoggerPlugin = new FileLoggerPlugin();
        }

        public async Task<SystemMetricsDto> GetMetrics()
        {
            return new SystemMetricsDto
            {
                CpuUsage = await GetCpuUsage(),
                RamUsedMb = await GetUsedMemoryInMb(),
                DiskUsedMb = await GetUsedDiskInMb()
            };
        }

        private async Task<float> GetCpuUsage()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = "-c \"top -l 1 | grep 'CPU usage'\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                var idleStr = output.Split(',').FirstOrDefault(x => x.Contains("idle"));
                if (idleStr != null && float.TryParse(idleStr.Replace("% idle", "").Trim(), out float idle))
                {
                    return 100f - idle;
                }
            }
            catch(Exception ex)
            {
                await _fileLoggerPlugin.OtherExceptionCollected($"Error in fetching CPU Usage: {ex.Message}");
                Console.WriteLine($"Error in fetching CPU Usage: {ex.Message}");
            }
            return 0f;
        }

        private async Task<float> GetUsedMemoryInMb()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = "-c \"vm_stat\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                const int pageSize = 4096;
                var lines = output.Split('\n');

                long pagesActive = ParseVmStatValue(lines, "Pages active");
                long pagesInactive = ParseVmStatValue(lines, "Pages inactive");
                long pagesWired = ParseVmStatValue(lines, "Pages wired down");

                long usedPages = pagesActive + pagesInactive + pagesWired;
                long usedBytes = usedPages * pageSize;

                return usedBytes / (1024f * 1024f);
            }
            catch(Exception ex ) 
            {
                await _fileLoggerPlugin.OtherExceptionCollected($"Error in fetching Memory Usage: {ex.Message}");
                Console.WriteLine($"Error in fetching Memory Usage: {ex.Message}");
                return 0f;
            }
        }

        private long ParseVmStatValue(string[] lines, string key)
        {
            var line = lines.FirstOrDefault(l => l.StartsWith(key));
            if (line == null) return 0;

            var parts = line.Split(':');
            if (parts.Length < 2) return 0;

            var number = parts[1].Trim().TrimEnd('.');
            return long.TryParse(number, out long result) ? result : 0;
        }

        private async Task<float> GetUsedDiskInMb()
        {
            try
            {
                var drive = new DriveInfo("/");
                if (!drive.IsReady) return 0;

                long used = drive.TotalSize - drive.TotalFreeSpace;
                return used / (1024f * 1024f);
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error in fetching Disk Usage: {ex.Message}");
                await _fileLoggerPlugin.OtherExceptionCollected($"Error in fetching Disk Usage: {ex.Message}");
                return 0f;
            }
        }
    }
}
