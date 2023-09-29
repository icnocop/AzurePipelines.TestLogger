using System;
using System.Net.Http;

namespace AzurePipelines.TestLogger.Tests
{
    public class ClientMessage : IEquatable<ClientMessage>
    {
        public HttpMethod Method { get; }

        public string Endpoint { get; }

        public string ApiVersion { get; }

        public string Body { get; }

        public ClientMessage(HttpMethod method, string endpoint, string apiVersion, string body)
        {
            Method = method;
            Endpoint = endpoint;
            ApiVersion = apiVersion;
            Body = body;
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = (hash * 31) + Method.GetHashCode();
            if (Endpoint != null)
            {
                hash = (hash * 31) + Endpoint.GetHashCode();
            }
            hash = (hash * 31) + ApiVersion.GetHashCode();
            if (Body != null)
            {
                hash = (hash * 31) + RemoveWhiteSpace(Body).GetHashCode();
            }
            return hash;
        }

        public override bool Equals(object obj) => Equals(obj as ClientMessage);

        public bool Equals(ClientMessage other) =>
            other?.Method.Equals(Method) == true
            && ((other.Endpoint == null && Endpoint == null) || other.Endpoint.Equals(Endpoint))
            && other.ApiVersion.Equals(ApiVersion)
            && ((other.Body == null && Body == null) || RemoveWhiteSpace(other.Body).Equals(RemoveWhiteSpace(Body)));

        private static string RemoveWhiteSpace(string str) =>
            string.Concat(str.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));

        public override string ToString()
        {
            return $"Method: {Method}, Endpoint: {Endpoint}, ApiVersion: {ApiVersion}, Body: {Body}";
        }
    }
}
