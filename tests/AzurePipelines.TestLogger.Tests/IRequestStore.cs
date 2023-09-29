using Microsoft.AspNetCore.Http;

namespace AzurePipelines.TestLogger.Tests
{
    internal interface IRequestStore
    {
        void Add(HttpRequest item);
    }
}