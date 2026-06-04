namespace KurisuAPI.Middleware
{
    public sealed class ApiKeyMiddleware(RequestDelegate next)
    {
        private const string _apiKeyHeader = "X-Api-Key";

        public async Task InvokeAsync(HttpContext ctx)
        {
            if (ctx.Request.Path.StartsWithSegments("/health"))
            {
                await next(ctx);

                return;
            }

            string? expectedKey = Environment.GetEnvironmentVariable("ADMIN_API_KEY");

            if (!ctx.Request.Headers.TryGetValue(_apiKeyHeader, out var key) || key != expectedKey)
            {
                ctx.Response.StatusCode = 401;
                await ctx.Response.WriteAsync("Unauthorized");

                return;
            }

            await next(ctx);
        }
    }
}
