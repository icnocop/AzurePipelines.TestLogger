using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace AzurePipelines.TestLogger.Tests
{
    public class CaptureRequestsMiddleware
    {
        private readonly RequestDelegate _next;

        public CaptureRequestsMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            // Capture the incoming request and store it
            IRequestStore requestStore = context.RequestServices.GetService<IRequestStore>();
            requestStore.Add(context.Request);

            // Call the next middleware in the pipeline
            await _next(context);
        }
    }
}