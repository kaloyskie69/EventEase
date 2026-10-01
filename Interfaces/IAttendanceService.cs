using System.Threading.Tasks;
using EventEase.ViewModels.Attendance;

namespace EventEase.Interfaces
{
    public interface IAttendanceService
    {
        Task<AttendanceCheckInViewModel?> GetCheckInRosterAsync(int eventId, string organizerId, string? searchQuery = null, string? statusFilter = null);
        Task<CheckInResultViewModel> ToggleCheckInAsync(int rsvpId, string organizerId, bool undo = false);
        Task<CheckInResultViewModel> CheckInAttendeeAsync(int rsvpId, string organizerId);
        Task<CheckInResultViewModel> UndoCheckInAttendeeAsync(int rsvpId, string organizerId);
    }
}
