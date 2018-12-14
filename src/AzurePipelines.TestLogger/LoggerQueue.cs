using AzurePipelines.TestLogger.Json;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AzurePipelines.TestLogger
{
    internal class LoggerQueue
    {
        private readonly AsyncProducerConsumerCollection<ITestResult> _queue = new AsyncProducerConsumerCollection<ITestResult>();
        private readonly Task _consumeTask;
        private readonly CancellationTokenSource _consumeTaskCancellationSource = new CancellationTokenSource();


        private readonly IApiClient _apiClient;
        private readonly string _buildId;
        private readonly string _agentName;
        private readonly string _jobName;

        // Internal for testing
        internal Dictionary<string, TestResultParent> Parents { get; } = new Dictionary<string, TestResultParent>();
        internal int RunId { get; set; }
        internal string Source { get; set; }
        internal string TestRunEndpoint { get; set; }

        public LoggerQueue(IApiClient apiClient, string buildId, string agentName, string jobName)
        {
            _apiClient = apiClient;
            _buildId = buildId;
            _agentName = agentName;
            _jobName = jobName;
            
            _consumeTask = ConsumeItemsAsync(_consumeTaskCancellationSource.Token);
        }

        public void Enqueue(ITestResult testResult) => _queue.Add(testResult);

        public void Flush()
        {
            // Cancel any idle consumers and let them return
            _queue.Cancel();

            try
            {
                // Any active consumer will circle back around and batch post the remaining queue
                _consumeTask.Wait(TimeSpan.FromSeconds(60));

                // Update the run and parents to a completed state
                SendTestsCompleted(_consumeTaskCancellationSource.Token).Wait(TimeSpan.FromSeconds(60));

                // Cancel any active HTTP requests if still hasn't finished flushing
                _consumeTaskCancellationSource.Cancel();
                if (!_consumeTask.Wait(TimeSpan.FromSeconds(10)))
                {
                    throw new TimeoutException("Cancellation didn't happen quickly");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private async Task ConsumeItemsAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                ITestResult[] nextItems = await _queue.TakeAsync();

                if (nextItems == null || nextItems.Length == 0)
                {
                    // Queue is canceling and is empty
                    return;      
                }

                await SendResultsAsync(nextItems, cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
            }
        }

        private async Task SendResultsAsync(ITestResult[] testResults, CancellationToken cancellationToken)
        {
            try
            {
                // Create a test run if we need it
                if (TestRunEndpoint == null)
                {
                    Source = GetSource(testResults);
                    RunId = await CreateTestRun(cancellationToken);
                    TestRunEndpoint = $"/{RunId}/results";
                }

                // Group results by their parent
                IEnumerable<IGrouping<string, ITestResult>> testResultsByParent = GroupTestResultsByParent(testResults);

                // Create any required parent nodes
                await CreateParents(testResultsByParent, cancellationToken);

                // Update parents with the test results
                await SendTestResults(testResultsByParent, cancellationToken);
            }
            catch (Exception)
            {
                // Eat any communications exceptions
            }
        }

        // Internal for testing
        internal static string GetSource(ITestResult[] testResults)
        {
            string source = Array.Find(testResults, x => !string.IsNullOrEmpty(x.Source))?.Source;
            if (source != null)
            {
                source = Path.GetFileName(source);
                if (source.EndsWith(".dll"))
                {
                    return source.Substring(0, source.Length - 4);
                }
            }
            return source;
        }

        // Internal for testing
        internal async Task<int> CreateTestRun(CancellationToken cancellationToken)
        {
            string runName = $"{( string.IsNullOrEmpty(Source) ? "Unknown Test Source" : Source)} (OS: { System.Runtime.InteropServices.RuntimeInformation.OSDescription }, Job: { _jobName }, Agent: { _agentName })";
            Dictionary<string, object> request = new Dictionary<string, object>
            {
                { "name", runName },
                { "build", new Dictionary<string, object> { { "id", _buildId } } },
                { "isAutomated", true }
            };
            string responseString = await _apiClient.SendAsync(HttpMethod.Post, null, "5.0-preview.2", request.ToJson(), cancellationToken);
            using (StringReader reader = new StringReader(responseString))
            {
                JsonObject response = JsonDeserializer.Deserialize(reader) as JsonObject;
                return response.ValueAsInt("id");
            }
        }

        // Internal for testing
        internal IEnumerable<IGrouping<string, ITestResult>> GroupTestResultsByParent(ITestResult[] testResults) =>
            testResults.GroupBy(x =>
            {
                string name = x.FullyQualifiedName;
                if (Source != null && name.StartsWith(Source + "."))
                {
                    name = name.Substring(Source.Length + 1);
                }

                // At this point, name should always have at least one '.' to represent the Class.Method
                // We need to start at the opening method if there is one
                int startIndex = name.IndexOf('(');
                if(startIndex < 0)
                {
                    startIndex = name.Length - 1;
                }
                return name.Substring(0, name.LastIndexOfAny(new[] { '.' }, startIndex));
            });

        // Internal for testing
        internal async Task CreateParents(IEnumerable<IGrouping<string, ITestResult>> testResultsByParent, CancellationToken cancellationToken)
        {
            // Find the parents that don't exist
            string[] parentsToAdd = testResultsByParent
                .Select(x => x.Key)
                .Where(x => !Parents.ContainsKey(x))
                .ToArray();

            // Batch an add operation and record the new parent IDs
            if (parentsToAdd.Length > 0)
            {
                string request = "[ " + string.Join(", ", parentsToAdd.Select(x =>
                {
                    Dictionary<string, object> properties = new Dictionary<string, object>
                    {
                        { "testCaseTitle", x },
                        { "automatedTestName", x },
                        { "outcome", "Passed" },  // Start with a passed outcome initially
                        { "state", "InProgress" },
                        { "automatedTestType", "UnitTest" },
                        { "automatedTestTypeId", "13cdc9d9-ddb5-4fa4-a97d-d965ccfc6d4b" }  // This is used in the sample response and also appears in web searches
                    };
                    if (!string.IsNullOrEmpty(Source))
                    {
                        properties.Add("automatedTestStorage", Source);
                    }
                    return properties.ToJson();
                })) + " ]";
                string responseString = await _apiClient.SendAsync(HttpMethod.Post, TestRunEndpoint, "5.0-preview.5", request, cancellationToken);
                using (StringReader reader = new StringReader(responseString))
                {
                    JsonObject response = JsonDeserializer.Deserialize(reader) as JsonObject;
                    JsonArray parents = (JsonArray)response.Value("value");
                    if (parents.Length != parentsToAdd.Length)
                    {
                        throw new Exception("Unexpected number of parents added");
                    }
                    for (int c = 0; c < parents.Length; c++)
                    {
                        int id = ((JsonObject)parents[c]).ValueAsInt("id");
                        Parents.Add(parentsToAdd[c], new TestResultParent(id));
                    }
                }
            }
        }

        private async Task SendTestResults(IEnumerable<IGrouping<string, ITestResult>> testResultsByParent, CancellationToken cancellationToken)
        {
            string request = "[ " + string.Join(", ", testResultsByParent.Select(x =>
            {
                TestResultParent parent = Parents[x.Key];
                string subResults = "[ " + string.Join(", ", x.Select(GetTestResultJson)) + " ]";
                string failedOutcome = x.Any(t => t.Outcome == TestOutcome.Failed) ? "\"outcome\": \"Failed\"," : null;
                parent.Duration += Convert.ToInt64(x.Sum(t => t.Duration.TotalMilliseconds));
                return $@"{{
                    ""id"": { parent.Id },
                    ""durationInMs"": { parent.Duration },
                    { failedOutcome },
                    ""subResults"": { subResults }
                }}";
            })) + " ]";
            await _apiClient.SendAsync(new HttpMethod("PATCH"), TestRunEndpoint, "5.0-preview.5", request, cancellationToken);
        }
        
        private string GetTestResultJson(ITestResult testResult)
        {
            Dictionary<string, object> properties = new Dictionary<string, object>
            {
                { "displayName", testResult.DisplayName },
                { "outcome", testResult.Outcome.ToString() }
            };

            if (testResult.Outcome == TestOutcome.Passed || testResult.Outcome == TestOutcome.Failed)
            {
                long duration = Convert.ToInt64(testResult.Duration.TotalMilliseconds);
                properties.Add("durationInMs", duration.ToString(CultureInfo.InvariantCulture));

                string errorStackTrace = testResult.ErrorStackTrace;
                if (!string.IsNullOrEmpty(errorStackTrace))
                {
                    properties.Add("stackTrace", errorStackTrace);
                }

                string errorMessage = testResult.ErrorMessage;
                StringBuilder stdErr = new StringBuilder();
                StringBuilder stdOut = new StringBuilder();
                foreach (TestResultMessage m in testResult.Messages)
                {
                    if (TestResultMessage.StandardOutCategory.Equals(m.Category, StringComparison.OrdinalIgnoreCase))
                    {
                        stdOut.AppendLine(m.Text);
                    }
                    else if (TestResultMessage.StandardErrorCategory.Equals(m.Category, StringComparison.OrdinalIgnoreCase))
                    {
                        stdErr.AppendLine(m.Text);
                    }
                }

                if (!string.IsNullOrEmpty(errorMessage) || stdErr.Length > 0 || stdOut.Length > 0)
                {
                    properties.Add("errorMessage", $"{errorMessage}\n\n---\n\nSTDERR:\n\n{stdErr}\n\n---\n\nSTDOUT:\n\n{stdOut}");
                }
            }
            else
            {
                // Handle output type skip, NotFound and None
            }

            return properties.ToJson();
        }

        private async Task SendTestsCompleted(CancellationToken cancellationToken)
        {
            // Mark all parents as completed
            string parentRequest = "[ " + string.Join(", ", Parents.Values.Select(x =>
                $@"{{
                    ""id"": { x.Id },
                    ""state"": ""Completed""
                }}")) + " ]";
            await _apiClient.SendAsync(new HttpMethod("PATCH"), TestRunEndpoint, "5.0-preview.5", parentRequest, cancellationToken);

            // Mark the overall test run as completed
            string testRunRequest = $@"{{
                    ""state"": ""Completed""
                }}";
            await _apiClient.SendAsync(new HttpMethod("PATCH"), $"/{RunId}", "5.0-preview.5", testRunRequest, cancellationToken);
        }
    }
}