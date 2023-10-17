using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace AzurePipelines.TestLogger.Tests
{
    internal class RequestStore : List<HttpRequest>, IRequestStore
    {
    }
}
