using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AzurePipelines.TestLogger
{
    internal interface IApiClient
    {
        bool Verbose { get; set; }

        string BuildRequestedFor { get; set; }

        IApiClient WithAccessToken(string accessToken);

        IApiClient WithDefaultCredentials();

        Task<int> AddTestRun(TestRun testRun, CancellationToken cancellationToken);

        Task UpdateTestResults(int testRunId, Dictionary<string, TestResultParent> parents, IEnumerable<IGrouping<string, ITestResult>> testResultsByParent, CancellationToken cancellationToken);

        Task UpdateTestResults(int testRunId, VstpTestRunComplete testRunComplete, CancellationToken cancellationToken);

        Task<int[]> AddTestCases(int testRunId, string[] testCaseNames, DateTime startedDate, string source, CancellationToken cancellationToken);

        Task MarkTestRunCompleted(int testRunId, bool aborted, DateTime completedDate, CancellationToken cancellationToken);
    }
}