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
            var dataRequest = await GetDbRequest(context.Request, context.User);
            try
            {
                // Use handler to process request
                foreach (var handler in _handlers)
                {
                    if (await handler.CanHandle(dataRequest, context.Request))
                    {
                        await handler.ProcessRequest(dataRequest, context.Response);
                        return; // Stop processing
                    }
                }

                // No handler matched, so show not found
                ShowNotFound(context.Response);
            }
            catch (Exception ex)
            {
                // Display the error message
                await ShowError(context.Response, ex);
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

        public static void ShowNotFound(HttpResponse response)
        {
            response.Clear();
            response.StatusCode = 404;
        }

        public static async Task ShowError(HttpResponse response, Exception ex)
        {
            response.Clear();
            response.StatusCode = 500;
            await response.WriteAsync("Application Error");
            Console.WriteLine(ex.Message);
        }

    }
}
