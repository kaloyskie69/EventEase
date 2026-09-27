using System.Threading.Tasks;
using EventEase.ViewModels.RSVP;

namespace EventEase.Interfaces
{
    public interface IRSVPService
    {
        Task<PublicEventLandingViewModel?> GetPublicLandingAsync(int eventId);
        Task<(bool Success, string? ErrorMessage, int RsvpId)> SubmitRSVPAsync(RSVPSubmitViewModel model);
        Task<RSVPConfirmationViewModel?> GetConfirmationAsync(int rsvpId);
        Task<bool> IsDuplicateEmailAsync(int eventId, string email);
    }
}
