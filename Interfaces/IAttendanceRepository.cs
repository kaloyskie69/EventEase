using System.Collections.Generic;
using System.Threading.Tasks;
using EventEase.Models;

namespace EventEase.Interfaces
{
    public interface IAttendanceRepository
    {
        Task<Attendance?> GetByRsvpIdAsync(int rsvpId);
        Task<Attendance> CheckInAsync(int rsvpId);
        Task<Attendance> UndoCheckInAsync(int rsvpId);
        Task<int> GetCheckedInCountByEventIdAsync(int eventId);
        Task<IEnumerable<Attendance>> GetAttendancesByEventIdAsync(int eventId);
    }
}
