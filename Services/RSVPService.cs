using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
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
            var eventTask = _eventRepository.GetByIdWithOrganizerAsync(eventId);
            var goingCountTask = _rsvpRepository.GetCountByStatusAsync(eventId, "Going");
            await Task.WhenAll(eventTask, goingCountTask);
            var ev = eventTask.Result;
            if (ev == null) return null;

            var goingCount = goingCountTask.Result;

            var answers = (ev.CustomFields ?? new List<CustomField>()).Select(cf => new CustomFieldAnswerViewModel
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
                Capacity = ev.Capacity,
                SubmitForm = submitForm
            };
        }

        public async Task<(bool Success, string? ErrorMessage, int RsvpId, string? ConfirmationToken)> SubmitRSVPAsync(RSVPSubmitViewModel model)
        {
            var ev = await _eventRepository.GetByIdWithCustomFieldsAsync(model.EventId);
            if (ev == null)
            {
                return (false, "The requested event could not be found.", 0, null);
            }

            if (ev.Status != "Upcoming" || GetEventStart(ev) < DateTime.Now)
            {
                return (false, ev.Status == "Cancelled"
                    ? "This event has been cancelled by the organizer."
                    : "This event is no longer accepting RSVPs.", 0, null);
            }

            if (!new[] { "Going", "Maybe", "Not Going" }.Contains(model.Status))
            {
                return (false, "Please choose Going, Maybe, or Not Going.", 0, null);
            }

            var submittedAnswers = model.Answers ?? new List<CustomFieldAnswerViewModel>();
            var eventFields = ev.CustomFields ?? new List<CustomField>();
            if (submittedAnswers.Count > eventFields.Count || submittedAnswers.Any(a => a == null))
            {
                return (false, "The submitted answers do not match this event's questions. Please reload the page and try again.", 0, null);
            }

            var duplicateFieldIds = submittedAnswers.GroupBy(a => a.CustomFieldId).Any(g => g.Count() > 1);
            if (duplicateFieldIds || submittedAnswers.Any(a => eventFields.All(f => f.Id != a.CustomFieldId)))
            {
                return (false, "The submitted answers do not match this event's questions. Please reload the page and try again.", 0, null);
            }

            foreach (var field in eventFields)
            {
                var answer = submittedAnswers.FirstOrDefault(a => a.CustomFieldId == field.Id);
                var response = answer?.Response?.Trim();
                if (field.Required && string.IsNullOrWhiteSpace(response))
                {
                    return (false, $"'{field.Label}' is required.", 0, null);
                }

                if (response?.Length > 2000)
                {
                    return (false, $"'{field.Label}' cannot exceed 2000 characters.", 0, null);
                }

                if (!string.IsNullOrWhiteSpace(response) && field.FieldType == "Dropdown" &&
                    !(field.Options ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Contains(response, StringComparer.Ordinal))
                {
                    return (false, $"Choose a valid option for '{field.Label}'.", 0, null);
                }

                if (!string.IsNullOrWhiteSpace(response) && field.FieldType == "Checkbox" && response != "Yes")
                {
                    return (false, $"Choose a valid response for '{field.Label}'.", 0, null);
                }

                if (!string.IsNullOrWhiteSpace(response) && field.FieldType == "Number" &&
                    !decimal.TryParse(response, NumberStyles.Number, CultureInfo.CurrentCulture, out _))
                {
                    return (false, $"Enter a valid number for '{field.Label}'.", 0, null);
                }
            }

            // Check duplicate RSVP using email
            var isDuplicate = await _rsvpRepository.ExistsByEmailAsync(model.EventId, model.Email);
            if (isDuplicate)
            {
                return (false, $"An RSVP with email '{model.Email}' has already been submitted for this event.", 0, null);
            }

            var rsvp = new RSVP
            {
                EventId = model.EventId,
                FullName = model.FullName.Trim(),
                Email = model.Email.Trim().ToLowerInvariant(),
                Phone = model.Phone?.Trim(),
                Status = model.Status,
                ConfirmationToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)),
                SubmittedAt = DateTime.UtcNow
            };

            var responses = new List<CustomFieldResponse>();
            if (submittedAnswers.Count > 0)
            {
                foreach (var field in eventFields)
                {
                    var ans = submittedAnswers.FirstOrDefault(a => a.CustomFieldId == field.Id);
                    if (ans == null) continue;
                    responses.Add(new CustomFieldResponse
                    {
                        CustomFieldId = field.Id,
                        Response = ans.Response?.Trim()
                    });
                }
            }

            try
            {
                var createdRsvp = await _rsvpRepository.AddAsync(rsvp, responses, ev.Capacity);
                return (true, null, createdRsvp.Id, createdRsvp.ConfirmationToken);
            }
            catch (DuplicateRsvpException)
            {
                return (false, $"An RSVP with email '{rsvp.Email}' has already been submitted for this event.", 0, null);
            }
        }

        public async Task<RSVPConfirmationViewModel?> GetConfirmationAsync(int rsvpId, string? confirmationToken)
        {
            var tokenRecord = await _rsvpRepository.GetByIdAsync(rsvpId);
            if (tokenRecord == null || string.IsNullOrWhiteSpace(confirmationToken) || string.IsNullOrEmpty(tokenRecord.ConfirmationToken)) return null;
            var expectedToken = Encoding.UTF8.GetBytes(tokenRecord.ConfirmationToken);
            var providedToken = Encoding.UTF8.GetBytes(confirmationToken);
            if (expectedToken.Length != providedToken.Length || !CryptographicOperations.FixedTimeEquals(expectedToken, providedToken)) return null;

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
                IsWaitlisted = rsvp.IsWaitlisted == true,
                SubmittedAt = rsvp.SubmittedAt,
                CustomResponses = responses
            };
        }

        public async Task<bool> IsDuplicateEmailAsync(int eventId, string email)
        {
            return await _rsvpRepository.ExistsByEmailAsync(eventId, email);
        }

        private static DateTime GetEventStart(Event ev)
        {
            var time = ev.Time ?? string.Empty;
            var startText = time.Split(new[] { '-', '–', '—' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim();
            if (!string.IsNullOrWhiteSpace(startText) && DateTime.TryParse(startText, out var parsed))
            {
                return ev.Date.Date.Add(parsed.TimeOfDay);
            }

            if (TimeSpan.TryParse(time, out var timeSpan))
            {
                return ev.Date.Date.Add(timeSpan);
            }

            return ev.Date.Date.AddHours(9);
        }
    }
}
