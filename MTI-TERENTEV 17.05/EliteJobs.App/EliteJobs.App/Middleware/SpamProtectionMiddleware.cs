using EliteJobs.App.Services;

namespace EliteJobs.App.Middleware
{
    public class SpamProtectionMiddleware
    {
        private readonly RequestDelegate _next;

        public SpamProtectionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Method == "POST" && context.Request.Path.Value?.Contains("/Create") == true)
            {
                var spamDetector = context.RequestServices.GetRequiredService<SpamDetectorService>();
                var form = await context.Request.ReadFormAsync();
                var title = form["Input.Title"].FirstOrDefault() ?? "";
                var description = form["Input.Description"].FirstOrDefault() ?? "";

                if (spamDetector.ContainsBannedContent(title, description))
                {
                    context.Response.Redirect("/Account/Blocked?reason=spam");
                    return;
                }
            }

            await _next(context);
        }
    }
}