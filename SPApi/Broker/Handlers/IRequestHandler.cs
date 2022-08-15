using Microsoft.AspNetCore.Http;
using SPApi.Models;
using System.Threading.Tasks;

namespace SPApi.Broker.Handlers
{
    public interface IRequestHandler
    {
        Task<bool> CanHandle(DataRequest dataRequest, HttpRequest request);

        Task ProcessRequest(DataRequest dataRequest, HttpResponse response);
    }
}
