using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SPApi.Models;
using System;
using System.Data;
using System.Threading.Tasks;

namespace SPApi.Broker.Handlers
{
    public class HelpHandler : IRequestHandler
    {
        private readonly Settings _settings;
        private readonly IServiceProvider _services;

        public HelpHandler(Settings settings, IServiceProvider services)
        {
            _settings = settings;
            _services = services;
        }

        public Task<bool> CanHandle(DataRequest dataRequest, HttpRequest request)
        {
            const string CONTEXT = "help";
            return Task.FromResult(this.IsHelpEnabled(request.Headers)
                && CONTEXT.Equals(dataRequest.Context, StringComparison.OrdinalIgnoreCase));
        }

        public bool IsHelpEnabled(IHeaderDictionary headers)
        {
            if (!_settings.EnableHelp)
                return false; // Help disabled
            else if (string.IsNullOrEmpty(_settings.HelpKey))
                return true; // Help enabled, but no key specified
            else
                return headers.TryGetValue("x-spapi-key", out var helpKeyStringValues)
                    && _settings.HelpKey.Equals(helpKeyStringValues.ToString());
        }

        public async Task ProcessRequest(DataRequest dataRequest, HttpResponse response)
        {
            const string SQL = @"
SELECT [value] FROM sys.extended_properties WHERE name = 'api'
    AND major_id = object_id(N'[' + @Schema + N'].[' + @Object + N']')";
            using var db = _services.GetService<IDbConnection>();
            var helpText = await db.ExecuteScalarAsync<string>(SQL, new
            {
                dataRequest.Schema,
                dataRequest.Object,
            });
            response.StatusCode = 200;
            response.ContentType = "text/plain";
            await response.WriteAsync(helpText ?? "not found");
        }
    }
}
