using Microsoft.AspNetCore.Http;
using SPApi.Broker;
using System;
using System.Threading.Tasks;

namespace SPApi
{
    public class Settings
    {
        public string Endpoint { get; set; } = "/api/data";

        public bool EnableHelp { get; set; } = false;

        public string HelpKey { get; set; }

        public bool RequireHttps { get; set; } = true;

        public void UseNotFoundHandler(Func<HttpContext, Task> handler)
            => this.HandleNotFound = handler;
        internal Func<HttpContext, Task> HandleNotFound { get; private set; } = BrokerService.ShowNotFound;

        public void UseErrorHandler(Func<HttpContext, Exception, Task> handler)
            => this.HandleError = handler;
        internal Func<HttpContext, Exception, Task> HandleError { get; private set; } = BrokerService.ShowError;
    }
}
