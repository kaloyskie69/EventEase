using System.Threading.Tasks;
using EventEase.ViewModels.Attendance;

namespace EventEase.Interfaces
{
    public interface IAttendanceService
    {
        Task<AttendanceCheckInViewModel?> GetCheckInRosterAsync(int eventId, string? searchQuery = null, string? statusFilter = null);
        Task<CheckInResultViewModel> ToggleCheckInAsync(int rsvpId, bool undo = false);
        Task<CheckInResultViewModel> CheckInAttendeeAsync(int rsvpId);
        Task<CheckInResultViewModel> UndoCheckInAttendeeAsync(int rsvpId);
    }
}
