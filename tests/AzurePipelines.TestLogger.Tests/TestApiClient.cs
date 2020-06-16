using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AzurePipelines.TestLogger.Tests
{
    internal class TestApiClient : ApiClient
    {
        private const string _apiVersion = "5.0";
        private readonly Func<string, string> _responseFunc;

        public List<ClientMessage> Messages { get; } = new List<ClientMessage>();

        public TestApiClient()
            : base(string.Empty, string.Empty, _apiVersion)
        {
        }

        public TestApiClient(Func<string, string> responseFunc)
            : this()
        {
            _responseFunc = responseFunc;
        }

        internal override Task<string> SendAsync(HttpMethod method, string endpoint, string body, CancellationToken cancellationToken, string apiVersion)
        {
            Messages.Add(new ClientMessage(method, endpoint, apiVersion ?? _apiVersion, body));
            return Task.FromResult(_responseFunc == null ? string.Empty : _responseFunc(body));
        }

        internal override string GetTestCasesAsCompleted(IEnumerable<TestResultParent> testCases, DateTime completedDate)
        {
            throw new NotImplementedException();
        }

        internal override string GetTestResults(Dictionary<string, TestResultParent> testCaseTestResults, IEnumerable<IGrouping<string, ITestResult>> testResultsByParent, DateTime completedDate)
        {
            throw new NotImplementedException();
        }
    }
}
