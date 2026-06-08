using EliteJobs.App.Data;
using EliteJobs.App.Middleware;
using EliteJobs.App.Models;
using EliteJobs.App.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;


Console.OutputEncoding = Encoding.UTF8;
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var builder = WebApplication.CreateBuilder(args);

// Переменные окружения для разработки
Environment.SetEnvironmentVariable("RECAPTCHA_SITE_KEY", "6LdIee4sAAAAAPXomE7hulK7kpcCLNv6HNYlMBcy");
Environment.SetEnvironmentVariable("RECAPTCHA_SECRET_KEY", "6LdIee4sAAAAACU102bxMV6IvUm-D7lPnoP0i2sH");

// ========== СЕРВИСЫ ==========
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Admin", "RequireAdminRole");
    options.Conventions.AllowAnonymousToPage("/Index");
    options.Conventions.AllowAnonymousToPage("/Account/Register");
    options.Conventions.AllowAnonymousToPage("/Account/Login");
    options.Conventions.AllowAnonymousToPage("/Account/ExternalLogin");
    options.Conventions.AllowAnonymousToPage("/Account/AccessDenied");
    options.Conventions.AllowAnonymousToPage("/Account/ForgotPassword");
    options.Conventions.AllowAnonymousToPage("/Account/ResetPassword");
    options.Conventions.AllowAnonymousToPage("/Details");
});

builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IEncryptionService, EncryptionService>();
builder.Services.AddScoped<IDirectoryService, DirectoryService>();
builder.Services.AddScoped<ICityValidationService, CityValidationService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<SpamDetectorService>();
builder.Services.AddScoped<CompetitorDataCollector>();
builder.Services.AddScoped<CompetitorAnalysisService>();
builder.Services.AddHttpClient("scraper", client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
    client.Timeout = TimeSpan.FromSeconds(3);
});
builder.Services.AddHttpClient<RecaptchaService>();

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredUniqueChars = 3;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders()
.AddRoles<IdentityRole>()
.AddErrorDescriber<RussianIdentityErrorDescriber>();

// Политики
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
});

// Cookie
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
});

// Antiforgery — ОТКЛЮЧАЕМ ГЛОБАЛЬНО для Register/Login
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = ".AspNetCore.Antiforgery.Elite";
    options.HeaderName = "X-CSRF-TOKEN";
});

// База данных
var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrEmpty(connectionString))
    throw new InvalidOperationException("Connection string not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
        npgsqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)));

builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Information);
});
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/root/.aspnet/DataProtection-Keys"))
    .SetApplicationName("EliteServices");
var app = builder.Build();

// ========== ИНИЦИАЛИЗАЦИЯ БД ==========
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        var retryCount = 0;
        while (!dbContext.Database.CanConnect() && retryCount < 10) { Thread.Sleep(3000); retryCount++; }

        if (dbContext.Database.CanConnect())
        {
            dbContext.Database.EnsureCreated();

            string[] roles = { "Admin", "ProviderIndividual", "ProviderCompany", "CustomerIndividual", "CustomerCompany" };
            foreach (var role in roles)
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));

            var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL") ?? "admin@elitejobs.ru";
            var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD");

            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                // Если пароль не задан в .env — генерируем случайный
                if (string.IsNullOrEmpty(adminPassword))
                {
                    adminPassword = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)) + "!Aa1";
                    Console.WriteLine("========================================");
                    Console.WriteLine("!!! IMPORTANT: Admin password generated !!!");
                    Console.WriteLine($"Admin Email: {adminEmail}");
                    Console.WriteLine($"Admin Password: {adminPassword}");
                    Console.WriteLine("SAVE THIS PASSWORD. It will NOT be shown again.");
                    Console.WriteLine("Or set ADMIN_PASSWORD in .env file.");
                    Console.WriteLine("========================================");
                }

                var admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FirstName = "Admin",
                    LastName = "System",
                    RegisteredDate = DateTime.UtcNow,
                    IsActive = true,
                    SecurityStamp = Guid.NewGuid().ToString()
                };

                var result = await userManager.CreateAsync(admin, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Admin");
                    if (!await dbContext.Subscriptions.AnyAsync(s => s.UserId == admin.Id))
                        dbContext.Subscriptions.Add(new Subscription { UserId = admin.Id, Tier = SubscriptionTier.Premium });

                    Console.WriteLine("Admin account created successfully.");
                }
                else
                {
                    Console.WriteLine($"ERROR creating admin: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }

            await dbContext.SaveChangesAsync();
        }
    }
    catch (Exception ex) { logger.LogError(ex, "DB init error"); }
}

// ========== MIDDLEWARE ==========
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";

    // CSP — разрешаем Google reCAPTCHA
    context.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://www.google.com https://www.gstatic.com; " +
        "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://fonts.googleapis.com; " +
        "font-src 'self' https://fonts.gstatic.com; " +
        "img-src 'self' data: https:; " +
        "connect-src 'self' https://www.google.com; " +
        "frame-src 'self' https://www.google.com";

    await next();
});

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
app.MapControllers();

app.Run();