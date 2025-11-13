using System.Reflection;

namespace TuringMachinesAPI.Middlewares
{
    public class ApplicationInformationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string _version;

        public ApplicationInformationMiddleware(RequestDelegate next)
        {
            _next = next;
            _version = Assembly
                .GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion?.Split('+')[0] ?? "unknown";
        }

        public async Task InvokeAsync(HttpContext context)
        {
            context.Response.OnStarting(() =>
            {
                if (!context.Response.Headers.ContainsKey("Application-Version"))
                    context.Response.Headers.Append("Application-Version", _version);
                return Task.CompletedTask;
            });

            await _next(context);
        }
    }

    public static class ApplicationInformationMiddlewareExtensions
    {
        public static IApplicationBuilder UseApplicationInformation(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ApplicationInformationMiddleware>();
        }
    }
}