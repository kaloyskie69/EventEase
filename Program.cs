using System;
using EventEase.Data;
using EventEase.Interfaces;
using EventEase.Models;
using EventEase.Repositories;
using EventEase.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

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

// 6. Add MVC Controllers and Views
builder.Services.AddControllersWithViews();
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("rsvp-submit", context =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            // Keep the partition count bounded if a client spoofs or rotates source IPs.
            (StringComparer.Ordinal.GetHashCode(context.Connection.RemoteIpAddress?.ToString() ?? "unknown") & 1023).ToString(),
            _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0
            }));
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "text/html; charset=utf-8";
        await context.HttpContext.Response.WriteAsync(
            "<!doctype html><html lang=\"en\"><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"><title>Please wait</title><body style=\"font:16px system-ui;max-width:38rem;margin:12vh auto;padding:1rem\"><h1>Please wait before trying again</h1><p>Too many RSVP submissions came from this connection. Wait a few minutes, then return to the event page and try again.</p></body></html>",
            cancellationToken);
    };
});

var app = builder.Build();

// 7. Seed NoSQL Database on Startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await DbInitializer.InitializeAsync(services);
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
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
