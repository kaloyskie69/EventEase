using System.Collections.Generic;
using System.Threading.Tasks;
using EventEase.Models;
using EventEase.ViewModels.Event;

namespace EventEase.Interfaces
{
    public interface IEventService
    {
        Task<IEnumerable<EventListItemViewModel>> GetEventsByOrganizerAsync(string organizerId, string? statusFilter = null, string? searchQuery = null);
        Task<EventDetailsViewModel?> GetEventDetailsAsync(int id, string? organizerId = null, string? baseUrl = null);
        Task<EventEditViewModel?> GetEventForEditAsync(int id, string organizerId);
        Task<Event> CreateEventAsync(string organizerId, EventCreateViewModel model);
        Task<bool> UpdateEventAsync(string organizerId, EventEditViewModel model);
        Task<bool> CancelEventAsync(int id, string organizerId);
        Task<bool> DeleteEventAsync(int id, string organizerId);
        Task<bool> IsTitleDuplicateAsync(string organizerId, string title, System.DateTime date, int? excludeId = null);
    }
}
