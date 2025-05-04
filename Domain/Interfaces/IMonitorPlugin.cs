using Domain.Models;

namespace Domain.Interfaces
{
    public interface IMonitorPlugin
    {
        Task OnMetricsCollected(SystemMetricsDto metrics);
    }
}
