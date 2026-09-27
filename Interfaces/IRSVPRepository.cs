using System.Collections.Generic;
using System.Threading.Tasks;
using EventEase.Models;

namespace EventEase.Interfaces
{
    public interface IRSVPRepository
    {
        Task<IEnumerable<RSVP>> GetByEventIdAsync(int eventId);
        Task<RSVP?> GetByIdAsync(int id);
        Task<RSVP?> GetByIdWithDetailsAsync(int id);
        Task<bool> ExistsByEmailAsync(int eventId, string email);
        Task<RSVP> AddAsync(RSVP rsvp, IEnumerable<CustomFieldResponse>? responses = null);
        Task<int> GetCountByEventIdAsync(int eventId);
        Task<int> GetCountByStatusAsync(int eventId, string status);
    }
}
