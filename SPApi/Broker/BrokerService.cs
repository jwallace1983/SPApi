using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SPApi.Broker.Handlers;
using SPApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SPApi.Broker
{
    public interface IBrokerService
    {
        Task Process(HttpContext context, Func<Task> next); 
    }

    public class BrokerService : IBrokerService
    {
        private readonly Settings _settings;
        private readonly IEnumerable<IRequestHandler> _handlers;
        
        public BrokerService(Settings settings, IServiceProvider services)
        {
            _settings = settings;
            _handlers = services.GetServices<IRequestHandler>();
        }

        public async Task Process(HttpContext context, Func<Task> next)
        {
            if (!this.ValidateRequest(context.Request))
                await next(); // Guard: do not process
            try
            {
                // Use handler to process request
                var dataRequest = await GetDbRequest(context.Request, context.User);
                foreach (var handler in _handlers)
                {
                    if (await handler.CanHandle(dataRequest, context.Request))
                    {
                        await handler.ProcessRequest(dataRequest, context.Response);
                        return; // Stop processing
                    }
                }

                // No handler matched, so show not found
                await _settings.HandleNotFound(context);
            }
            catch (Exception ex)
            {
                // Display the error message
                await _settings.HandleError(context, ex);
            }
        }

        public bool ValidateRequest(HttpRequest httpRequest)
        {
            const string POST = "POST";
            return POST.Equals(httpRequest.Method, StringComparison.OrdinalIgnoreCase) // Post only
                && (!_settings.RequireHttps || httpRequest.IsHttps) // Require https if configured
                && _settings.Endpoint.Equals(httpRequest.Path.Value, StringComparison.OrdinalIgnoreCase); // Match path
        }

        public static async Task<DataRequest> GetDbRequest(HttpRequest httpRequest, ClaimsPrincipal principal)
        {
            var request = await httpRequest.ReadFromJsonAsync<DataRequest>();
            if (string.IsNullOrEmpty(request.Schema))
                request.Schema = "dbo";
            if (principal.Identity.IsAuthenticated)
            {
                request.User = principal.Identity.Name;
                request.Claims = principal.Claims.ToDictionary(c => c.Type, c => c.Value ?? string.Empty);
            }
            else
            {
                request.User = null;
                request.Claims = Array.Empty<KeyValuePair<string, string>>();
            }
            return request;
        }

        public static Task ShowNotFound(HttpContext context)
        {
            context.Response.StatusCode = 404;
            return Task.CompletedTask;
        }

        public static async Task ShowError(HttpContext context, Exception ex)
        {
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync("Application Error");
            Console.WriteLine(ex.Message);
        }

    }
}
