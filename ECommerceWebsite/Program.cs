using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using ECommerceWebsite.Data;
using ECommerceWebsite.Models;
using ECommerceWebsite.Repository;
using ECommerceWebsite.Services;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc.Razor;
using System.Data.Common;
using Microsoft.AspNetCore.SignalR;
using ECommerceWebsite.Hubs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.ResponseCompression;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql =>
        {
            sql.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null);
        }
    ));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;

    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Register notification and email services
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<INewsletterService, NewsletterService>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// App cache abstraction (uses IDistributedCache under the hood)
builder.Services.AddScoped<ICacheService, RedisCacheService>();

// Distributed cache: use Redis in non-development, memory cache in development
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddControllersWithViews()
    .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
    .AddDataAnnotationsLocalization();

// SignalR for real-time notifications
builder.Services.AddSignalR();

// Background email queue and worker
builder.Services.AddSingleton<IEmailBackgroundQueue, EmailBackgroundQueue>();
builder.Services.AddHostedService<EmailBackgroundWorker>();

// Inventory service for concurrency-safe stock updates
builder.Services.AddScoped<IInventoryService, InventoryService>();

// Cookie policy and consent
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.Lax;
    options.HttpOnly = HttpOnlyPolicy.Always;
    options.Secure = CookieSecurePolicy.SameAsRequest;
    options.ConsentCookie = new CookieBuilder
    {
        Name = ".Consent",
        HttpOnly = true,
        IsEssential = true,
        SameSite = SameSiteMode.Lax
    };
    options.CheckConsentNeeded = context =>
    {
        // Respect our front-end consent flag stored in localStorage via a header hint if provided
        var header = context.Request.Headers["X-Cookie-Consent"].ToString();
        return string.IsNullOrEmpty(header) || !string.Equals(header, "accepted", StringComparison.OrdinalIgnoreCase);
    };
});

// Rate limiting policies
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("global", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            }
        ));

    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: (httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon") + ":login",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            }
        ));

    options.AddPolicy("upload", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: (httpContext.User.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon") + ":upload",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            }
        ));
});

// Health checks
builder.Services.AddHealthChecks();

// Response compression (gzip/br) for text assets
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
    {
        "image/svg+xml"
    });
});

var supportedCultures = new[] { new CultureInfo("en-US"), new CultureInfo("ar-SA") };
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("en-US");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.RequestCultureProviders.Insert(0, new CookieRequestCultureProvider());
});

var app = builder.Build();

// Configure static file caching per environment
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        if (app.Environment.IsDevelopment())
        {
            ctx.Context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
            ctx.Context.Response.Headers.Append("Pragma", "no-cache");
            ctx.Context.Response.Headers.Append("Expires", "0");
        }
        else
        {
            // Cache static assets aggressively in non-development environments
            // 1 year max-age with immutable since file names are typically fingerprinted
            ctx.Context.Response.Headers.Append("Cache-Control", "public, max-age=31536000, immutable");
            ctx.Context.Response.Headers.Remove("Pragma");
            ctx.Context.Response.Headers.Remove("Expires");
        }
    }
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

var localizationOptions = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<RequestLocalizationOptions>>().Value;
app.UseRequestLocalization(localizationOptions);

app.UseRouting();

// Enforce cookie policy prior to session/auth
app.UseCookiePolicy();

// Apply global rate limiting
app.UseRateLimiter();

// Enable compression before serving static files and MVC
app.UseResponseCompression();

// Fallback for missing product images under /images/products/* -> serve default image
app.Use(async (context, next) =>
{
    await next.Invoke();
    if (context.Response.StatusCode == 404 &&
        context.Request.Path.HasValue &&
        context.Request.Path.Value!.StartsWith("/images/products/", StringComparison.OrdinalIgnoreCase))
    {
        var defaultPath = "/images/products/phone1.jpg";
        var fileInfo = app.Environment.WebRootFileProvider.GetFileInfo(defaultPath);
        if (fileInfo.Exists)
        {
            context.Response.StatusCode = 200;
            context.Response.ContentType = "image/jpeg";
            await context.Response.SendFileAsync(fileInfo);
        }
    }
});

// Enable session before auth if it's used during requests
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

// Map SignalR hubs
app.MapHub<NotificationHub>("/notificationHub");

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Health endpoints
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = _ => true
});

// Apply pending migrations automatically, then seed database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();
        await DbInitializer.SeedAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while applying migrations or seeding the database.");
    }
}

app.Run();
