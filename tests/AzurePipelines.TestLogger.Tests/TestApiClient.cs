using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AzurePipelines.TestLogger.Tests
{
    public class TestApiClient : IApiClient
    {
        private readonly Func<string, string> _responseFunc;

        public List<ClientMessage> Messages { get; } = new List<ClientMessage>();

        public TestApiClient()
        {
        }

        public TestApiClient(Func<string, string> responseFunc)
        {
            _responseFunc = responseFunc;
        }

        public Task<string> SendAsync(HttpMethod method, string endpoint, string apiVersion, string body, CancellationToken cancellationToken)
        {
            Messages.Add(new ClientMessage(method, endpoint, apiVersion, body));
            return Task.FromResult(_responseFunc == null ? string.Empty : _responseFunc(body));
        }
    }
}
