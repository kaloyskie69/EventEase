using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventEase.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace EventEase.Data
{
    /// <summary>
    /// Seeds default organizer accounts, demo events with embedded custom fields,
    /// RSVPs, and attendance records into the NoSQL database on application startup.
    /// </summary>
    public static class DbInitializer
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // 1. Seed default Organizer account if not exists
            var organizerEmail = "organizer@eventease.com";
            var organizer = await userManager.FindByEmailAsync(organizerEmail);
            if (organizer == null)
            {
                organizer = new ApplicationUser
                {
                    Id = Guid.NewGuid().ToString(),
                    UserName = organizerEmail,
                    Email = organizerEmail,
                    FullName = "Alex Morgan",
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow.AddMonths(-3)
                };

                var createResult = await userManager.CreateAsync(organizer, "Password123!");
                if (!createResult.Succeeded)
                {
                    throw new Exception("Failed to seed default organizer: " + string.Join(", ", createResult.Errors.Select(e => e.Description)));
                }
            }

            // 2. Seed events if no events exist in the NoSQL database
            var existingEvents = await context.Events.FindAllAsync();
            if (!existingEvents.Any())
            {
                var today = DateTime.Today;

                // Event 1: Tech Innovation Summit (Upcoming)
                var event1 = new Event
                {
                    Id = 1,
                    OrganizerId = organizer.Id,
                    Title = "Tech Innovation Summit 2026",
                    Description = "Explore cutting-edge advancements in cloud computing, artificial intelligence, and cybersecurity. Connect with industry pioneers, founders, and engineers.",
                    Venue = "Grand Convention Center, Hall A",
                    Date = today.AddDays(3),
                    Time = "09:00 AM - 05:00 PM",
                    Status = "Upcoming",
                    CreatedAt = DateTime.UtcNow.AddDays(-15),
                    CustomFields = new List<CustomField>
                    {
                        new CustomField { Id = 1, EventId = 1, Label = "Dietary Restriction", FieldType = "Dropdown", Required = true, Options = "None,Vegetarian,Vegan,Halal,Gluten-Free" },
                        new CustomField { Id = 2, EventId = 1, Label = "Organization / Company", FieldType = "Text", Required = true },
                        new CustomField { Id = 3, EventId = 1, Label = "T-Shirt Size", FieldType = "Dropdown", Required = false, Options = "S,M,L,XL,XXL" }
                    }
                };

                // Event 2: Today's Event - Annual Charity Gala
                var event2 = new Event
                {
                    Id = 2,
                    OrganizerId = organizer.Id,
                    Title = "Annual Charity Gala & Silent Auction",
                    Description = "An evening of celebration, live music, and giving back to support educational scholarships for underprivileged youth in our community.",
                    Venue = "Riverside Waterfront Ballroom",
                    Date = today,
                    Time = "06:30 PM - 10:30 PM",
                    Status = "Upcoming",
                    CreatedAt = DateTime.UtcNow.AddDays(-20),
                    CustomFields = new List<CustomField>
                    {
                        new CustomField { Id = 4, EventId = 2, Label = "Food Preference", FieldType = "Dropdown", Required = true, Options = "Salmon Fillet,Herb-Crusted Filet Mignon,Truffle Risotto (Vegan)" },
                        new CustomField { Id = 5, EventId = 2, Label = "Table Seating Group", FieldType = "Text", Required = false }
                    }
                };

                // Event 3: University Hackathon 2026
                var event3 = new Event
                {
                    Id = 3,
                    OrganizerId = organizer.Id,
                    Title = "Intercollegiate Code Sprint Hackathon",
                    Description = "A 24-hour collaborative sprint where collegiate programmers and designers build scalable web applications to solve community challenges.",
                    Venue = "Science & Technology Complex, Room 401",
                    Date = today.AddDays(14),
                    Time = "08:00 AM - Next Day 08:00 AM",
                    Status = "Upcoming",
                    CreatedAt = DateTime.UtcNow.AddDays(-5),
                    CustomFields = new List<CustomField>
                    {
                        new CustomField { Id = 6, EventId = 3, Label = "Course / Major", FieldType = "Text", Required = true },
                        new CustomField { Id = 7, EventId = 3, Label = "Student Number", FieldType = "Text", Required = true },
                        new CustomField { Id = 8, EventId = 3, Label = "Equipment Needed", FieldType = "Dropdown", Required = false, Options = "None,External Monitor,Extension Cord,Soldering Kit" }
                    }
                };

                // Event 4: Past Completed Event
                var event4 = new Event
                {
                    Id = 4,
                    OrganizerId = organizer.Id,
                    Title = "AI & Machine Learning Workshop",
                    Description = "Hands-on masterclass building neural networks and transformer architectures using PyTorch and cloud containers.",
                    Venue = "Metro Innovation Hub, Studio B",
                    Date = today.AddMonths(-1),
                    Time = "01:00 PM - 05:00 PM",
                    Status = "Completed",
                    CreatedAt = DateTime.UtcNow.AddMonths(-2)
                };

                await context.Events.InsertOneAsync(event1);
                await context.Events.InsertOneAsync(event2);
                await context.Events.InsertOneAsync(event3);
                await context.Events.InsertOneAsync(event4);

                // Seed RSVPs and Attendance for Event 2 (Gala - Today's Event)
                var galaRsvps = new List<(string Name, string Email, string Phone, string Status, string Food, string Seating, bool CheckedIn)>
                {
                    ("Elena Rostova", "elena.rostova@example.com", "+1 555-0192", "Going", "Salmon Fillet", "Table 1 - Sponsors", true),
                    ("Marcus Chen", "marcus.chen@techventures.io", "+1 555-0144", "Going", "Herb-Crusted Filet Mignon", "Table 1 - Sponsors", true),
                    ("Sophia Rodriguez", "sophia.rodriguez@greenearth.org", "+1 555-0178", "Going", "Truffle Risotto (Vegan)", "Table 2 - Guests", true),
                    ("David Kim", "david.kim@acmeprime.com", "+1 555-0123", "Going", "Herb-Crusted Filet Mignon", "Table 3", false),
                    ("Chloe Bennett", "chloe.b@creativelab.com", "+1 555-0189", "Going", "Salmon Fillet", "Table 2 - Guests", false),
                    ("Liam O'Connor", "liam.oconnor@financegroup.com", "+1 555-0167", "Going", "Salmon Fillet", "Table 3", false),
                    ("Zara Al-Mansoor", "zara.almansoor@globalhealth.org", "+1 555-0199", "Maybe", "Truffle Risotto (Vegan)", "Unassigned", false),
                    ("Thomas Vance", "tvance@industrialcorp.com", "+1 555-0131", "Not Going", "None", "", false)
                };

                var rsvpId = 1;
                foreach (var item in galaRsvps)
                {
                    var rsvp = new RSVP
                    {
                        Id = rsvpId,
                        EventId = event2.Id,
                        FullName = item.Name,
                        Email = item.Email,
                        Phone = item.Phone,
                        Status = item.Status,
                        SubmittedAt = DateTime.UtcNow.AddDays(-7).AddHours(item.Name.Length),
                        CustomFieldResponses = new List<CustomFieldResponse>
                        {
                            new CustomFieldResponse { Id = 1, RSVPId = rsvpId, CustomFieldId = 4, Response = item.Food },
                            new CustomFieldResponse { Id = 2, RSVPId = rsvpId, CustomFieldId = 5, Response = item.Seating }
                        },
                        Attendance = new Attendance
                        {
                            Id = rsvpId,
                            RSVPId = rsvpId,
                            EventId = event2.Id,
                            CheckedIn = item.CheckedIn,
                            CheckedInTime = item.CheckedIn ? DateTime.UtcNow.AddMinutes(-30) : null
                        }
                    };

                    await context.RSVPs.InsertOneAsync(rsvp);
                    await context.Attendances.InsertOneAsync(rsvp.Attendance);
                    rsvpId++;
                }

                // Seed RSVPs for Event 3 (Hackathon)
                var hackathonRsvps = new List<(string Name, string Email, string Status, string Major, string StudentNo, string Equip)>
                {
                    ("Jordan Miller", "jmiller@university.edu", "Going", "Computer Science", "CS-2023-8911", "External Monitor"),
                    ("Aaliyah Patel", "apatel@university.edu", "Going", "Software Engineering", "SE-2024-1022", "Extension Cord"),
                    ("Brian O'Reilly", "boreilly@techcol.edu", "Going", "Information Technology", "IT-2022-4412", "Soldering Kit"),
                    ("Samantha Wu", "swu@polytech.edu", "Maybe", "Electrical Engineering", "EE-2023-5590", "None")
                };

                foreach (var item in hackathonRsvps)
                {
                    var rsvp = new RSVP
                    {
                        Id = rsvpId,
                        EventId = event3.Id,
                        FullName = item.Name,
                        Email = item.Email,
                        Status = item.Status,
                        SubmittedAt = DateTime.UtcNow.AddDays(-3).AddHours(item.Name.Length),
                        CustomFieldResponses = new List<CustomFieldResponse>
                        {
                            new CustomFieldResponse { Id = 1, RSVPId = rsvpId, CustomFieldId = 6, Response = item.Major },
                            new CustomFieldResponse { Id = 2, RSVPId = rsvpId, CustomFieldId = 7, Response = item.StudentNo },
                            new CustomFieldResponse { Id = 3, RSVPId = rsvpId, CustomFieldId = 8, Response = item.Equip }
                        },
                        Attendance = new Attendance
                        {
                            Id = rsvpId,
                            RSVPId = rsvpId,
                            EventId = event3.Id,
                            CheckedIn = false,
                            CheckedInTime = null
                        }
                    };

                    await context.RSVPs.InsertOneAsync(rsvp);
                    await context.Attendances.InsertOneAsync(rsvp.Attendance);
                    rsvpId++;
                }
            }
        }
    }
}
