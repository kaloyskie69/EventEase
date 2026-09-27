using System.Threading.Tasks;
using EventEase.ViewModels.Dashboard;
using EventEase.ViewModels.Reports;

namespace EventEase.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardOverviewViewModel> GetDashboardOverviewAsync(string organizerId);
        Task<EventReportViewModel> GetReportAnalyticsAsync(string organizerId);
    }
}
