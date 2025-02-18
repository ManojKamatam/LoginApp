using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Configure host with shutdown timeout
builder.Host.ConfigureServices(services =>
{
    services.Configure<HostOptions>(options =>
    {
        options.ShutdownTimeout = TimeSpan.FromSeconds(30);
    });
});

// Add health checks
builder.Services.AddHealthChecks();

// Add services to the container
builder.Services.AddControllersWithViews();

// Configure Data Protection without persistence
builder.Services.AddDataProtection()
    .DisableAutomaticKeyGeneration();

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

// Configure error handling and logging
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    
    app.Use(async (context, next) =>
    {
        try
        {
            await next();
        }
        catch (Exception ex)
        {
            var logger = context.RequestServices
                .GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Unhandled exception occurred");
            throw;
        }
    });
}

// Add health check endpoint
app.MapHealthChecks("/health", new HealthCheckOptions
{
    AllowCachingResponses = false
});

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

// Configure Kestrel with connection management
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.AddServerHeader = false;
    serverOptions.Limits.MaxRequestBodySize = 10 * 1024 * 1024;
    serverOptions.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
    serverOptions.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
    serverOptions.ListenAnyIP(80);
});

// Enhanced application lifetime handling
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

lifetime.ApplicationStarted.Register(() =>
{
    logger.LogInformation("Application started at: {time}", DateTimeOffset.UtcNow);
});

lifetime.ApplicationStopping.Register(() =>
{
    logger.LogInformation("Application is stopping at: {time}", DateTimeOffset.UtcNow);
    // Add delay to allow in-flight requests to complete
    Thread.Sleep(5000);
});

lifetime.ApplicationStopped.Register(() =>
{
    logger.LogInformation("Application stopped at: {time}", DateTimeOffset.UtcNow);
});

// Add this to your task definition in buildspec.yml
var taskDef = @"
{
  ""containerDefinitions"": [{
    ""name"": ""loginapp"",
    ""healthCheck"": {
      ""command"": [ ""CMD-SHELL"", ""curl -f http://localhost/health || exit 1"" ],
      ""interval"": 30,
      ""timeout"": 5,
      ""retries"": 3,
      ""startPeriod"": 60
    },
    ""stopTimeout"": 30
  }]
}";

app.Run();
