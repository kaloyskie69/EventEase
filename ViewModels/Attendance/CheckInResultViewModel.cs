namespace EventEase.ViewModels.Attendance
{
    public class CheckInToggleRequest
    {
        public int RsvpId { get; set; }
        public bool Undo { get; set; } = false;
    }

    public class CheckInResultViewModel
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int RsvpId { get; set; }
        public bool CheckedIn { get; set; }
        public string? CheckedInTime { get; set; }
        public int TotalRSVPs { get; set; }
        public int TotalGoing { get; set; }
        public int TotalCheckedIn { get; set; }
        public double AttendancePercentage { get; set; }
    }
}
