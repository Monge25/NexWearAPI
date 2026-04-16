namespace NexWearAPI.Extensions
{
    public static class HttpContextExtensions
    {
        public static string? GetIpAddress(this HttpContext context)
        {
            // Soporta proxies (Railway usa proxy)
            var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwarded))
                return forwarded.Split(',')[0].Trim();

            return context.Connection.RemoteIpAddress?.ToString();
        }

        public static string? GetUserAgent(this HttpContext context)
        {
            return context.Request.Headers["User-Agent"].FirstOrDefault();
        }
    }
}
