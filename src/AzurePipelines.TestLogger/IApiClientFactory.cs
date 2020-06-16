namespace AzurePipelines.TestLogger
{
    internal interface IApiClientFactory
    {
        IApiClient CreateWithDefaultCredentials(string collectionUri, string teamProject, string apiVersionString);
        IApiClient CreateWithAccessToken(string accessToken, string collectionUri, string teamProject, string apiVersionString);
    }
}
