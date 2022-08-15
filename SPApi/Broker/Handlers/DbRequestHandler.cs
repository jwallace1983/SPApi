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
            if (_queryHandlers.Value.ContainsKey(dataRequest.Context ?? string.Empty) == false)
                return false; // No query handler found
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
            using var db = _services.GetService<IDbConnection>();
            var queryResult = _queryHandlers.Value[dataRequest.Context ?? string.Empty](db, dataRequest);
            await WriteResponse(response, queryResult);
        }

        public delegate Task<object> QueryHandler(IDbConnection db, DataRequest dataRequest);
        public static readonly Lazy<Dictionary<string, QueryHandler>> _queryHandlers = new(() =>
            new Dictionary<string, QueryHandler>(StringComparer.OrdinalIgnoreCase)
            {
                { "multiple", GetQueryResultMultiple },
                { "record", GetQueryResultRecord },
                { "scalar", GetQueryResultScalar },
                { string.Empty, GetQueryResultQuery },
            });

        public static async Task<object> GetQueryResultMultiple(IDbConnection db, DataRequest dataRequest)
            => await db.QueryMultipleAsync($"[{dataRequest.Schema}].[{dataRequest.Object}]",
                    param: GetQueryParameters(dataRequest), commandTimeout: dataRequest.CommandTimeout,
                    commandType: CommandType.StoredProcedure);

        public static async Task<object> GetQueryResultRecord(IDbConnection db, DataRequest dataRequest)
            => await db.QueryFirstOrDefaultAsync($"[{dataRequest.Schema}].[{dataRequest.Object}]",
                    param: GetQueryParameters(dataRequest), commandTimeout: dataRequest.CommandTimeout,
                    commandType: CommandType.StoredProcedure);

        public static async Task<object> GetQueryResultScalar(IDbConnection db, DataRequest dataRequest)
            => await db.ExecuteScalarAsync($"[{dataRequest.Schema}].[{dataRequest.Object}]",
                    param: GetQueryParameters(dataRequest), commandTimeout: dataRequest.CommandTimeout,
                    commandType: CommandType.StoredProcedure);

        public static async Task<object> GetQueryResultQuery(IDbConnection db, DataRequest dataRequest)
            => await db.QueryAsync($"[{dataRequest.Schema}].[{dataRequest.Object}]",
                    param: GetQueryParameters(dataRequest), commandTimeout: dataRequest.CommandTimeout,
                    commandType: CommandType.StoredProcedure);

        public static Dictionary<string, object> GetQueryParameters(DataRequest dataRequest)
        {
            var parameters = dataRequest.Parameters.ToDictionary(m => m.Key, m => GetValue((JsonElement)m.Value));
            parameters["_user"] = dataRequest.User;
            parameters["_claims"] = JsonSerializer.Serialize(dataRequest.Claims);
            return parameters;
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

        public static async Task WriteResponse(HttpResponse response, object queryResult)
        {
            response.StatusCode = 200;
            response.ContentType = "application/json";
            await response.WriteAsync(JsonSerializer.Serialize(queryResult));
        }
    }
}
