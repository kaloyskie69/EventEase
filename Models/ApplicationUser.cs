using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace EventEase.Models
{
    /// <summary>
    /// Represents an authenticated Event Organizer in the EventEase system.
    /// Extends IdentityUser for Entity Framework Core Identity.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property for events
        public virtual ICollection<Event> Events { get; set; } = new List<Event>();
    }
}
