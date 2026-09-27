using System.Collections.Generic;
using System.Threading.Tasks;
using EventEase.Models;

namespace EventEase.Interfaces
{
    public interface IEventRepository
    {
        Task<IEnumerable<Event>> GetAllByOrganizerAsync(string organizerId);
        Task<Event?> GetByIdAsync(int id);
        Task<Event?> GetByIdWithDetailsAsync(int id);
        Task<Event?> GetByIdWithCustomFieldsAsync(int id);
        Task<Event> AddAsync(Event ev);
        Task UpdateAsync(Event ev);
        Task DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<bool> ExistsWithTitleAndDateAsync(string organizerId, string title, System.DateTime date, int? excludeId = null);
        Task AddCustomFieldsAsync(IEnumerable<CustomField> customFields);
        Task DeleteCustomFieldsByEventIdAsync(int eventId);
    }
}
