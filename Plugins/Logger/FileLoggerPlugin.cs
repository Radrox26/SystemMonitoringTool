using Domain.Interfaces;
using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugins.Logger
{
    public class FileLoggerPlugin : IMonitorPlugin
    {
        private readonly string _logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "system_metrics_log.txt");

        public async Task OnMetricsCollected(SystemMetricsDto metrics)
        {
            var logLine = $"{DateTime.Now:u} | CPU: {metrics.CpuUsage}% | RAM: {metrics.RamUsedMb}MB | Disk: {metrics.DiskUsedMb}MB";

            try
            {
                await File.AppendAllTextAsync(_logFilePath, logLine + Environment.NewLine);
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error writing to log file: {ex.Message}");
            }
        }

        public async Task OtherExceptionCollected(string message)
        {
            var logLine = $"{DateTime.Now:u} | ERROR: {message}";

            try
            {
                await File.AppendAllTextAsync(_logFilePath, logLine + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing to log file: {ex.Message}");
            }
        }

        public async Task MonitoringFinished(string message)
        {
            var logLine = $"{DateTime.Now:u} | {message}";

            try
            {
                await File.AppendAllTextAsync(_logFilePath, logLine + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing to log file: {ex.Message}");
            }
        }
    }
}
