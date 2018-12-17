using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AzurePipelines.TestLogger
{
    internal class ApiClient : IApiClient
    {
        private static readonly HttpClient _client = new HttpClient();

        private readonly string _baseUrl;

        public ApiClient(string accessToken, string collectionUri, string teamProject)
        {
            _baseUrl = $"{collectionUri}{teamProject}/_apis/test/runs";

            // The : character delimits username (which should be empty here) and password in basic auth headers
            _client.DefaultRequestHeaders.Authorization
                 = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($":{accessToken}")));
        }

        public async Task<string> SendAsync(HttpMethod method, string endpoint, string apiVersion, string body, CancellationToken cancellationToken)
        {
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            if (apiVersion == null)
            {
                throw new ArgumentNullException(nameof(apiVersion));
            }

            string requestUri = $"{_baseUrl}{endpoint}?api-version={apiVersion}";
            HttpRequestMessage request = new HttpRequestMessage(method, requestUri);
            if (body != null)
            {
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");
            }
            HttpResponseMessage response = await _client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error from AzurePipelines logger while sending {method} to {requestUri}\nBody:\n{body}\nException:\n{ex}");
                throw;
            }
            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }
    }
}