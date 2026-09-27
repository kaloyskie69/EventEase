using System;
using System.Collections.Generic;

namespace EventEase.ViewModels.RSVP
{
    public class RSVPConfirmationViewModel
    {
        public int RsvpId { get; set; }
        public int EventId { get; set; }
        public string EventTitle { get; set; } = string.Empty;
        public string Venue { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string EventTime { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Status { get; set; } = "Going"; // Going, Maybe, Not Going
        public DateTime SubmittedAt { get; set; }

        public Dictionary<string, string> CustomResponses { get; set; } = new Dictionary<string, string>();
    }
}
