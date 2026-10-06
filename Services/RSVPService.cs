using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventEase.Interfaces;
using EventEase.Models;
using EventEase.ViewModels.RSVP;
using EventEase.Repositories;

namespace EventEase.Services
{
    public class RSVPService : IRSVPService
    {
        private readonly IEventRepository _eventRepository;
        private readonly IRSVPRepository _rsvpRepository;

        public RSVPService(IEventRepository eventRepository, IRSVPRepository rsvpRepository)
        {
            _eventRepository = eventRepository;
            _rsvpRepository = rsvpRepository;
        }

        public async Task<PublicEventLandingViewModel?> GetPublicLandingAsync(int eventId)
        {
            var ev = await _eventRepository.GetByIdWithDetailsAsync(eventId);
            if (ev == null) return null;

            var goingCount = ev.RSVPs.Count(r => r.Status == "Going");

            var answers = ev.CustomFields.Select(cf => new CustomFieldAnswerViewModel
            {
                CustomFieldId = cf.Id,
                Label = cf.Label,
                FieldType = cf.FieldType,
                Required = cf.Required,
                Options = cf.Options,
                Response = string.Empty
            }).ToList();

            var submitForm = new RSVPSubmitViewModel
            {
                EventId = ev.Id,
                Status = "Going",
                Answers = answers
            };

            return new PublicEventLandingViewModel
            {
                EventId = ev.Id,
                Title = ev.Title,
                Description = ev.Description,
                Venue = ev.Venue,
                Date = ev.Date,
                Time = ev.Time,
                Status = ev.Status,
                OrganizerName = ev.Organizer?.FullName ?? "Event Organizer",
                GoingCount = goingCount,
                SubmitForm = submitForm
            };
        }

        public async Task<(bool Success, string? ErrorMessage, int RsvpId)> SubmitRSVPAsync(RSVPSubmitViewModel model)
        {
            var ev = await _eventRepository.GetByIdAsync(model.EventId);
            if (ev == null)
            {
                return (false, "The requested event could not be found.", 0);
            }

            if (ev.Status == "Cancelled")
            {
                return (false, "This event has been cancelled by the organizer.", 0);
            }

            // Check duplicate RSVP using email
            var isDuplicate = await _rsvpRepository.ExistsByEmailAsync(model.EventId, model.Email);
            if (isDuplicate)
            {
                return (false, $"An RSVP with email '{model.Email}' has already been submitted for this event.", 0);
            }

            var rsvp = new RSVP
            {
                EventId = model.EventId,
                FullName = model.FullName.Trim(),
                Email = model.Email.Trim().ToLower(),
                Phone = model.Phone?.Trim(),
                Status = model.Status,
                SubmittedAt = DateTime.UtcNow
            };

            var responses = new List<CustomFieldResponse>();
            if (model.Answers != null && model.Answers.Any())
            {
                foreach (var ans in model.Answers)
                {
                    responses.Add(new CustomFieldResponse
                    {
                        CustomFieldId = ans.CustomFieldId,
                        Response = ans.Response?.Trim()
                    });
                }
            }

            try
            {
                var createdRsvp = await _rsvpRepository.AddAsync(rsvp, responses);
                return (true, null, createdRsvp.Id);
            }
            catch (DuplicateRsvpException)
            {
                return (false, $"An RSVP with email '{rsvp.Email}' has already been submitted for this event.", 0);
            }
        }

        public async Task<RSVPConfirmationViewModel?> GetConfirmationAsync(int rsvpId)
        {
            var rsvp = await _rsvpRepository.GetByIdWithDetailsAsync(rsvpId);
            if (rsvp == null || rsvp.Event == null) return null;

            var responses = new Dictionary<string, string>();
            foreach (var r in rsvp.CustomFieldResponses)
            {
                if (r.CustomField != null)
                {
                    responses[r.CustomField.Label] = r.Response ?? "—";
                }
            }

            return new RSVPConfirmationViewModel
            {
                RsvpId = rsvp.Id,
                EventId = rsvp.EventId,
                EventTitle = rsvp.Event.Title,
                Venue = rsvp.Event.Venue,
                EventDate = rsvp.Event.Date,
                EventTime = rsvp.Event.Time,
                FullName = rsvp.FullName,
                Email = rsvp.Email,
                Status = rsvp.Status,
                SubmittedAt = rsvp.SubmittedAt,
                CustomResponses = responses
            };
        }

        public async Task<bool> IsDuplicateEmailAsync(int eventId, string email)
        {
            return await _rsvpRepository.ExistsByEmailAsync(eventId, email);
        }
    }
}
