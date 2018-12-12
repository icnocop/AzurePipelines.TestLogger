using AzurePipelines.TestLogger.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AzurePipelines.TestLogger
{
    internal class ApiClient
    {
        private const string ApiVersion = "5.0-preview.2";

        private static readonly HttpClient _client = new HttpClient();

        private readonly string _baseUrl;

        public ApiClient(string accessToken, string collectionUri, string teamProject)
        {
            _baseUrl = $"{collectionUri}{teamProject}/_apis/test/runs";

            // The : character delimits username (which should be empty here) and password in basic auth headers
            _client.DefaultRequestHeaders.Authorization
                 = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($":{ accessToken }")));
        }

        public async Task<JsonObject> PostAsync(string json, CancellationToken cancellationToken, string endpoint = null)
        {
            HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
            string requestUri = $"{ _baseUrl }{ endpoint }?api-version={ ApiVersion }";
            HttpResponseMessage response = await _client.PostAsync(requestUri, content, cancellationToken);
            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch(Exception)
            {
                Console.WriteLine("POST" + Environment.NewLine + requestUri + Environment.NewLine + json);
                throw;
            }
            string responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            using (StringReader sr = new StringReader(responseString))
            {
                return JsonDeserializer.Deserialize(sr) as JsonObject;
            }
        }
    }
}