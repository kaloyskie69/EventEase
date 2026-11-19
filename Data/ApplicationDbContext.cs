using EventEase.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EventEase.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Event> Events => Set<Event>();
        public DbSet<RSVP> RSVPs => Set<RSVP>();
        public DbSet<Attendance> Attendances => Set<Attendance>();
        public DbSet<CustomField> CustomFields => Set<CustomField>();
        public DbSet<CustomFieldResponse> CustomFieldResponses => Set<CustomFieldResponse>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Unique index: one RSVP per email per event
            builder.Entity<RSVP>()
                .HasIndex(r => new { r.EventId, r.Email })
                .IsUnique();

            // Event -> RSVP (Cascade)
            builder.Entity<RSVP>()
                .HasOne(r => r.Event)
                .WithMany(e => e.RSVPs)
                .HasForeignKey(r => r.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-one: RSVP <-> Attendance (Cascade when RSVP is deleted)
            builder.Entity<Attendance>()
                .HasOne(a => a.RSVP)
                .WithOne(r => r.Attendance)
                .HasForeignKey<Attendance>(a => a.RSVPId)
                .OnDelete(DeleteBehavior.Cascade);

            // Attendance -> Event (NoAction to prevent multiple cascade paths in SQL Server)
            builder.Entity<Attendance>()
                .HasOne(a => a.Event)
                .WithMany()
                .HasForeignKey(a => a.EventId)
                .OnDelete(DeleteBehavior.NoAction);

            // Event -> CustomField (Cascade)
            builder.Entity<CustomField>()
                .HasOne(cf => cf.Event)
                .WithMany(e => e.CustomFields)
                .HasForeignKey(cf => cf.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            // RSVP -> CustomFieldResponse (Cascade)
            builder.Entity<CustomFieldResponse>()
                .HasOne(cfr => cfr.RSVP)
                .WithMany(r => r.CustomFieldResponses)
                .HasForeignKey(cfr => cfr.RSVPId)
                .OnDelete(DeleteBehavior.Cascade);

            // CustomFieldResponse -> CustomField (NoAction to prevent multiple cascade paths in SQL Server)
            builder.Entity<CustomFieldResponse>()
                .HasOne(cfr => cfr.CustomField)
                .WithMany()
                .HasForeignKey(cfr => cfr.CustomFieldId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
