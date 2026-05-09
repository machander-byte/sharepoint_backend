using ZMS.Core.Models;

namespace ZMS.Application.Contracts;

public interface IDashboardService
{
    Task<DashboardSummary> GetSummaryAsync(CancellationToken cancellationToken);
}
