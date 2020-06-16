using System;
using System.Collections.Generic;
using System.Linq;
using AzurePipelines.TestLogger.Json;

namespace AzurePipelines.TestLogger
{
    internal class ApiClientV3 : ApiClient
    {
        public ApiClientV3(string collectionUri, string teamProject, string apiVersionString)
            : base(collectionUri, teamProject, apiVersionString)
        {
        }

        internal override string GetTestCasesAsCompleted(IEnumerable<TestResultParent> testCases, DateTime completedDate)
        {
            // https://docs.microsoft.com/en-us/azure/devops/integrate/previous-apis/test/results?view=tfs-2015#add-test-results-to-a-test-run
            return "[ " + string.Join(", ", testCases.Select(x =>
                $@"{{
                ""TestResult"": {{ ""Id"": {x.Id} }},
                ""testCase"": {{ ""id"": {x.Id} }},
                ""state"": ""Completed"",
                ""startedDate"": ""{x.StartedDate.ToString(_dateFormatString)}"",
                ""completedDate"": ""{completedDate.ToString(_dateFormatString)}""
            }}")) + " ]";
        }

        internal override string GetTestResults(
            Dictionary<string, TestResultParent> testCaseTestResults,
            IEnumerable<IGrouping<string, ITestResult>> testResultsByParent,
            DateTime completedDate)
        {
            // https://docs.microsoft.com/en-us/azure/devops/integrate/previous-apis/test/results?view=tfs-2015#update-test-results-for-a-test-run
            return "[ " + string.Join(", ", testResultsByParent.Select(x =>
            {
                TestResultParent parent = testCaseTestResults[x.Key];
                return string.Join(", ", x.Select(y =>
                {
                    Dictionary<string, object> testResultProperties = GetTestResultProperties(y);
                    testResultProperties.Add("TestResult", new Dictionary<string, object> { { "Id", parent.Id } });
                    testResultProperties.Add("id", y.Id);

                    return testResultProperties.ToJson();
                }));
            })) + " ]";
        }
    }
}