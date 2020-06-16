using Semver;

namespace AzurePipelines.TestLogger
{
    internal class ApiClientFactory : IApiClientFactory
    {
        public IApiClient CreateWithAccessToken(string accessToken, string collectionUri, string teamProject, string apiVersionString)
        {
            return CreateApiClient(collectionUri, teamProject, apiVersionString)
                .WithAccessToken(accessToken);
        }

        public IApiClient CreateWithDefaultCredentials(string collectionUri, string teamProject, string apiVersionString)
        {
            return CreateApiClient(collectionUri, teamProject, apiVersionString)
                .WithDefaultCredentials();
        }

        private IApiClient CreateApiClient(string collectionUri, string teamProject, string apiVersionString)
        {
            SemVersion apiVersion = SemVersion.Parse(apiVersionString);

            if (apiVersion < new SemVersion(5, 0))
            {
                return new ApiClientV3(collectionUri, teamProject, apiVersionString);
            }
            else
            {
                return new ApiClientV5(collectionUri, teamProject, apiVersionString);
            }
        }
    }
}
