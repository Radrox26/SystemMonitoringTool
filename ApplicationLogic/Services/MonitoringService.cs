using Domain.Interfaces;
using Domain.Models;
using Plugins.Logger;

namespace ApplicationLogic.Services
{
    public class MonitoringService
    {
        private readonly ISystemResourceMonitor _monitor;
        private readonly IEnumerable<IMonitorPlugin> _plugins;
        private readonly int _intervalSeconds;
        private FileLoggerPlugin _fileLoggingPlugin;

        public MonitoringService(ISystemResourceMonitor monitor, IEnumerable<IMonitorPlugin> plugins, int intervalSeconds)
        {
            _monitor = monitor;
            _plugins = plugins;
            _intervalSeconds = intervalSeconds;
            _fileLoggingPlugin = new FileLoggerPlugin();
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    SystemMetricsDto metrics = await _monitor.GetMetrics();

                    Console.WriteLine($"[{DateTime.Now}] CPU: {metrics.CpuUsage:F1}%, RAM: {metrics.RamUsedMb:F1}MB, Disk: {metrics.DiskUsedMb:F1}MB");

                    foreach (var plugin in _plugins)
                    {
                        try
                        {
                            await plugin.OnMetricsCollected(metrics);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Plugin Error] {plugin.GetType().Name}: {ex.Message}");
                            await _fileLoggingPlugin.OtherExceptionCollected($"[Plugin Error] {plugin.GetType().Name}: {ex.Message}");
                        }
                    }

                    await Task.Delay(TimeSpan.FromSeconds(_intervalSeconds), cancellationToken);
                }
            }
            catch(OperationCanceledException)
            {
                Console.WriteLine("Monitoring stopped.");
                await _fileLoggingPlugin.MonitoringFinished("Monitoring stopped.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Monitoring Error] {ex.Message}");
                await _fileLoggingPlugin.OtherExceptionCollected($"[Monitoring Error] {ex.Message}");
            }
        }
    }
}
