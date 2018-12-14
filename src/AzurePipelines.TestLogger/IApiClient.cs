using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AzurePipelines.TestLogger
{
    internal interface IApiClient
    {
        Task<string> SendAsync(HttpMethod method, string endpoint, string apiVersion, string body, CancellationToken cancellationToken);
    }
}