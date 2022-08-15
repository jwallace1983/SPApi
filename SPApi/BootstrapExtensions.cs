using Microsoft.Extensions.DependencyInjection;
using SPApi;
using SPApi.Broker;
using SPApi.Broker.Handlers;
using System;

namespace Microsoft.AspNetCore.Builder
{
    public static class BootstrapExtensions
    {
        public static IServiceCollection AddSPApi(this IServiceCollection services, Action<Settings> applySettings = null)
        {
            var settings = new Settings();
            applySettings?.Invoke(settings);
            return services
                .AddTransient<IRequestHandler, HelpHandler>()
                .AddTransient<IRequestHandler, DbRequestHandler>()
                .AddSingleton<IBrokerService, BrokerService>()
                .AddSingleton(settings);
        }

        public static IApplicationBuilder UseSPApi(this IApplicationBuilder app)
        {
            app.Use(async (context, next) =>
            {
                var brokerService = app.ApplicationServices.GetService<IBrokerService>();
                await brokerService.Process(context, next);
            });
            return app;
        }
    }
}
