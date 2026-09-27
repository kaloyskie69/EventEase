using System;

namespace EventEase.ViewModels.RSVP
{
    public class PublicEventLandingViewModel
    {
        public int EventId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Venue { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Time { get; set; } = string.Empty;
        public string Status { get; set; } = "Upcoming";
        public string OrganizerName { get; set; } = string.Empty;

        public int GoingCount { get; set; }
        public bool IsEventActive => Status == "Upcoming" && Date.Date >= DateTime.Today;

        public DateTime EventDateTime
        {
            get
            {
                var dt = Date.Date;
                if (!string.IsNullOrWhiteSpace(Time))
                {
                    if (DateTime.TryParse(Time, out var parsed))
                    {
                        return dt.Add(parsed.TimeOfDay);
                    }
                    if (TimeSpan.TryParse(Time, out var ts))
                    {
                        return dt.Add(ts);
                    }
                }
                return dt.AddHours(9);
            }
        }

        public RSVPSubmitViewModel SubmitForm { get; set; } = new RSVPSubmitViewModel();
    }
}
