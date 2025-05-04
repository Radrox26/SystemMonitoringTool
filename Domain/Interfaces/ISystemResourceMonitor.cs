using Domain.Models;

namespace Domain.Interfaces
{
    public interface ISystemResourceMonitor
    {
        Task<SystemMetricsDto> GetMetrics();
    }
}
