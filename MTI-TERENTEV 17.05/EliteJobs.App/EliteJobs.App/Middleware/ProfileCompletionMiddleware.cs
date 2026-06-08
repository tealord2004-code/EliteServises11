using EliteJobs.App.Data;
using EliteJobs.App.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EliteJobs.App.Middleware
{
    public class ProfileCompletionMiddleware
    {
        private readonly RequestDelegate _next;

        public ProfileCompletionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var user = await userManager.GetUserAsync(context.User);
                if (user != null)
                {
                    var path = context.Request.Path.Value?.ToLower() ?? "";

                    var alwaysAllowed = new[]
                    {
                        "/profile/", "/account/", "/css/", "/js/", "/lib/", "/api/",
                        "/identity/", "/index", "/details", "/chat/"
                    };

                    if (alwaysAllowed.Any(p => path.StartsWith(p)))
                    {
                        await _next(context);
                        return;
                    }

                    // Проверка истечения подписки
                    var sub = await dbContext.Subscriptions.FirstOrDefaultAsync(s => s.UserId == user.Id);
                    if (sub != null && !sub.IsActive && sub.Tier != SubscriptionTier.Free)
                    {
                        sub.Tier = SubscriptionTier.Free;
                        sub.ExpiryDate = null;
                        sub.ServicesUsed = 0;
                        await dbContext.SaveChangesAsync();
                    }

                    // Проверка базового профиля
                    bool basicProfileComplete = !string.IsNullOrEmpty(user.FirstName) &&
                                                !string.IsNullOrEmpty(user.LastName);
                    if (!basicProfileComplete)
                    {
                        context.Response.Redirect("/Profile/WorkerEdit");
                        return;
                    }

                    // Проверка ролей и их требований
                    var roles = await userManager.GetRolesAsync(user);

                    if (roles.Contains("ProviderIndividual"))
                    {
                        var hasResume = await dbContext.Resumes.AnyAsync(r => r.WorkerId == user.Id && r.IsActive);
                        if (!hasResume)
                        {
                            context.Response.Redirect("/Profile/ResumeEdit");
                            return;
                        }
                    }

                    if (roles.Contains("ProviderCompany"))
                    {
                        var hasCompany = await dbContext.Companies.AnyAsync(c => c.EmployerId == user.Id);
                        if (!hasCompany)
                        {
                            context.Response.Redirect("/Profile/CompanyEdit");
                            return;
                        }
                    }

                    // Проверка лимитов подписки при создании услуги
                    if (path == "/create" && context.Request.Method == "GET")
                    {
                        var tier = sub?.Tier ?? SubscriptionTier.Free;
                        var isActive = sub?.IsActive ?? true;

                        if (!isActive && tier != SubscriptionTier.Free)
                        {
                            tier = SubscriptionTier.Free;
                        }

                        var activeServices = await dbContext.Services
                            .CountAsync(s => s.ProviderId == user.Id && s.IsActive);
                        var maxServices = SubscriptionLimits.MaxServices(tier);

                        if (activeServices >= maxServices && tier != SubscriptionTier.Premium)
                        {
                            context.Response.Redirect("/Profile/Upgrade?limit=services");
                            return;
                        }
                    }
                }
            }

            await _next(context);
        }
    }
}