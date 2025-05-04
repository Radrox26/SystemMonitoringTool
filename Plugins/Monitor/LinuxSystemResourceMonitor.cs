using Domain.Interfaces;
using Domain.Models;
using Plugins.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugins.Monitor
{
    public class LinuxSystemResourceMonitor : ISystemResourceMonitor
    {
        private FileLoggerPlugin _fileLoggerPlugin;

        public LinuxSystemResourceMonitor()
        {
            _fileLoggerPlugin = new FileLoggerPlugin();
        }

        private float _lastTotal = 0;
        private float _lastIdle = 0;

        public async Task<SystemMetricsDto> GetMetrics()
        {
            return new SystemMetricsDto
            {
                CpuUsage = await GetCpuUsage(),
                RamUsedMb = await GetRamUsage(),
                DiskUsedMb = await GetDiskUsage()
            };
        }

        private async Task<float> GetCpuUsage()
        {
            try
            {
                var lines = File.ReadAllLines("/proc/stat");
                var cpuLine = lines.FirstOrDefault(l => l.StartsWith("cpu "));
                if (cpuLine == null) return 0;

                var values = cpuLine.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                    .Skip(1)
                                    .Select(float.Parse)
                                    .ToArray();

                float idle = values[3];
                float total = values.Sum();

                float deltaIdle = idle - _lastIdle;
                float deltaTotal = total - _lastTotal;

                _lastIdle = idle;
                _lastTotal = total;

                if (deltaTotal == 0) return 0;
                return 100f * (1 - deltaIdle / deltaTotal);
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error in fetching CPU Usage: {ex.Message}");
                await _fileLoggerPlugin.OtherExceptionCollected($"Error in fetching CPU Usage: {ex.Message}");
                return 0;
            }
        }

        private async Task<float> GetRamUsage()
        {
            try
            {
                var lines = File.ReadAllLines("/proc/meminfo");
                float total = ParseMemValue(lines, "MemTotal");
                float available = ParseMemValue(lines, "MemAvailable");
                return total - available;
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error in fetching Memory Usage: {ex.Message}");
                await _fileLoggerPlugin.OtherExceptionCollected($"Error in fetching Memory Usage: {ex.Message}");
                return 0;
            }
        }

        private float ParseMemValue(string[] lines, string key)
        {
            var line = lines.FirstOrDefault(l => l.StartsWith(key));
            if (line == null) return 0;

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return float.Parse(parts[1]) / 1024f; // kB to MB
        }

        private async Task<float> GetDiskUsage()
        {
            try
            {
                var drive = new DriveInfo("/");
                if (!drive.IsReady) return 0;

                long used = drive.TotalSize - drive.TotalFreeSpace;
                return used / (1024f * 1024f); // bytes to MB
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error in fetching Disk Usage: {ex.Message}");
                await _fileLoggerPlugin.OtherExceptionCollected($"Error in fetching Disk Usage: {ex.Message}");
                return 0;
            }
        }
    }
}
