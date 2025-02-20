using Datadog.Trace;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// Configure host with shutdown timeout
builder.Host.ConfigureServices(services =>
{
    services.Configure<HostOptions>(options =>
    {
        options.ShutdownTimeout = TimeSpan.FromSeconds(30);
    });
});

// Add Datadog Tracing
builder.Services.AddDatadogTracing();

// Add services to the container
builder.Services.AddControllersWithViews();

// Configure Data Protection with minimum key lifetime
builder.Services.AddDataProtection()
    .SetApplicationName("LoginApp")
    .SetDefaultKeyLifetime(TimeSpan.FromDays(7));

// Add Authentication with secure cookie settings
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
        options.Cookie.Name = "LoginApp.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Add Datadog TracingMiddleware early in the pipeline
app.UseDatadogTracing();

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

// Enhanced application lifetime handling
var logger = app.Services.GetRequiredService<ILogger<Program>>();

app.Lifetime.ApplicationStarted.Register(() =>
{
    logger.LogInformation("Application started at: {time}", DateTimeOffset.UtcNow);
});

app.Lifetime.ApplicationStopping.Register(() =>
{
    logger.LogInformation("Application is stopping at: {time}", DateTimeOffset.UtcNow);
    // Add delay to allow in-flight requests to complete
    Thread.Sleep(5000);
});

app.Run();
