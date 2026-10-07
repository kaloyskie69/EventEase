using System;
using EventEase.Data;
using EventEase.Interfaces;
using EventEase.Models;
using EventEase.Repositories;
using EventEase.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Register MongoDB convention pack to ignore extra elements globally
var conventionPack = new MongoDB.Bson.Serialization.Conventions.ConventionPack
{
    new MongoDB.Bson.Serialization.Conventions.IgnoreExtraElementsConvention(true)
};
MongoDB.Bson.Serialization.Conventions.ConventionRegistry.Register("IgnoreExtraElements", conventionPack, t => true);

// 1. Configure NoSQL MongoDB Context (Connects to MongoDB or local document store)
builder.Services.AddSingleton<MongoDbContext>();

// 2. Configure ASP.NET Identity with NoSQL MongoDB Stores
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;

    // User settings
    options.User.RequireUniqueEmail = true;
})
.AddUserStore<MongoUserStore>()
.AddRoleStore<MongoRoleStore>()
.AddDefaultTokenProviders();

// 3. Configure Cookie Settings
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
});

// 4. Register Dependency Injection Repositories (Repository Pattern on NoSQL)
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IRSVPRepository, RSVPRepository>();
builder.Services.AddScoped<IAttendanceRepository, AttendanceRepository>();

// 5. Register Dependency Injection Services (Service Layer)
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IRSVPService, RSVPService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

// 6. Configure Antiforgery to support AJAX headers (e.g. Live Check-In)
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
});

// 7. Add MVC Controllers and Views
builder.Services.AddControllersWithViews();

var app = builder.Build();

// 7. Seed NoSQL Database on Startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // Self-heal: align MongoDB ID counters with existing data before any seeding/inserts
        var context = services.GetRequiredService<MongoDbContext>();
        await context.SynchronizeCountersAsync();

        await DbInitializer.InitializeAsync(services);

        // Re-sync after seeding: the seeder inserts demo data with hardcoded IDs,
        // so raise the counters to the actual max IDs to avoid duplicate _id errors.
        await context.SynchronizeCountersAsync();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the NoSQL database.");
    }
}

// 8. Configure HTTP Request Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
