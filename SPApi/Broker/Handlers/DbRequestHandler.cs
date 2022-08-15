using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SPApi.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SPApi.Broker.Handlers
{
    public class DbRequestHandler : IRequestHandler
    {
        private readonly IServiceProvider _services;

        public DbRequestHandler(IServiceProvider services)
        {
            _services = services;
        }

        public async Task<bool> CanHandle(DataRequest dataRequest, HttpRequest request)
        {
            const string SQL = @"
SELECT COUNT(*) FROM sys.extended_properties WHERE name = 'api'
    AND major_id = object_id(N'[' + @Schema + N'].[' + @Object + N']')";
            using var db = _services.GetService<IDbConnection>();
            var rowCount = await db.ExecuteScalarAsync<int>(SQL, new
            {
                dataRequest.Schema,
                dataRequest.Object,
            });
            return rowCount > 0;
        }

        public async Task ProcessRequest(DataRequest dataRequest, HttpResponse response)
        {
            var queryResults = await this.GetQueryResults(dataRequest);
            await WriteResponse(response, queryResults);
        }

        public async Task<IEnumerable<object>> GetQueryResults(DataRequest dataRequest)
        {
            using var db = _services.GetService<IDbConnection>();
            var parameters = dataRequest.Parameters.ToDictionary(m => m.Key, m => GetValue((JsonElement)m.Value));
            parameters["_user"] = dataRequest.User;
            parameters["_claims"] = JsonSerializer.Serialize(dataRequest.Claims);
            return await db.QueryAsync($"[{dataRequest.Schema}].[{dataRequest.Object}]",
                param: parameters,
                commandTimeout: dataRequest.CommandTimeout,
                commandType: CommandType.StoredProcedure);
        }

        public static object GetValue(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Number => element.TryGetInt64(out long longValue) ? longValue
                    : element.TryGetDecimal(out decimal decimalValue) ? decimalValue
                    : element.TryGetDouble(out double doubleValue) ? doubleValue
                    : null,
                _ => element.GetRawText(),
            };
        }

        public static async Task WriteResponse(HttpResponse response, IEnumerable<object> queryResults)
        {
            response.StatusCode = 200;
            response.ContentType = "application/json";
            await response.WriteAsync(JsonSerializer.Serialize(queryResults));
        }
    }
}
