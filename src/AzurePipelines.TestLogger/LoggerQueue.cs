using AzurePipelines.TestLogger.Json;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AzurePipelines.TestLogger
{
    internal class LoggerQueue
    {
        private readonly AsyncProducerConsumerCollection<TestResult> _queue = new AsyncProducerConsumerCollection<TestResult>();
        private readonly Task _consumeTask;
        private readonly CancellationTokenSource _consumeTaskCancellationSource = new CancellationTokenSource();

        private readonly Dictionary<string, int> _parentIds = new Dictionary<string, int>();

        private readonly ApiClient _apiClient;
        private readonly string _buildId;
        private readonly string _agentName;
        private readonly string _jobName;

        private string _filename = null;
        private string _testRunEndpoint = null;

        public LoggerQueue(ApiClient apiClient, string buildId, string agentName, string jobName)
        {
            _apiClient = apiClient;
            _buildId = buildId;
            _agentName = agentName;
            _jobName = jobName;
            
            _consumeTask = ConsumeItemsAsync(_consumeTaskCancellationSource.Token);
        }

        public void Enqueue(TestResult testResult) => _queue.Add(testResult);

        public void Flush()
        {
            // Cancel any idle consumers and let them return
            _queue.Cancel();

            try
            {
                // Any active consumer will circle back around and batch post the remaining queue
                _consumeTask.Wait(TimeSpan.FromSeconds(60));

                // Cancel any active HTTP requests if still hasn't finished flushing
                _consumeTaskCancellationSource.Cancel();
                if (!_consumeTask.Wait(TimeSpan.FromSeconds(10)))
                {
                    throw new TimeoutException("cancellation didn't happen quickly");
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
                TestResult[] nextItems = await _queue.TakeAsync();

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

        private async Task SendResultsAsync(TestResult[] testResults, CancellationToken cancellationToken)
        {
            //string jsonArray = "[" + string.Join(",", testResults) + "]";
            try
            {
                // Create a test run if we need it
                if (_testRunEndpoint == null)
                {
                    _filename = Array.Find(testResults, x => !string.IsNullOrEmpty(x.TestCase.Source))?.TestCase.Source;
                    _filename = _filename == null ? null : Path.GetFileNameWithoutExtension(_filename);
                    int runId = await CreateTestRun(cancellationToken);
                    _testRunEndpoint = $"/{runId}/results";
                }

                // Group results by their parent and create any required parent nodes
                IEnumerable<IGrouping<string, TestResult>> testResultsByParent = await ProcessTestResults(testResults, cancellationToken);

                // Update parents with the test results
                await SendTestResults(testResultsByParent, cancellationToken);
            }
            catch (Exception)
            {
                // Eat any communications exceptions
            }
        }

        private async Task<int> CreateTestRun(CancellationToken cancellationToken)
        {
            string runName = $"{( string.IsNullOrEmpty(_filename) ? "Unknown Test File" : _filename)} (OS: { System.Runtime.InteropServices.RuntimeInformation.OSDescription }, Job: { _jobName }, Agent: { _agentName }) at {DateTime.UtcNow.ToString("o")}";
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

        private async Task<IEnumerable<IGrouping<string, TestResult>>> ProcessTestResults(TestResult[] testResults, CancellationToken cancellationToken)
        {
            // Group test runs with their parents
            IEnumerable<IGrouping<string, TestResult>> testResultsByParent = testResults.GroupBy(x =>
            {
                string name = x.TestCase.FullyQualifiedName;
                if (_filename != null && name.StartsWith(_filename + "."))
                {
                    name = name.Substring(_filename.Length + 1);
                }

                // At this point, name should always have at least one '.' to represent the Class.Method
                return name.Substring(0, name.LastIndexOfAny(new[] { '.', '(' }));
            });

            // Find the parents that don't exist
            string[] parentsToAdd = testResultsByParent
                .Select(x => x.Key)
                .Where(x => !_parentIds.ContainsKey(x))
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
                        { "automatedTestType", "UnitTest" },
                        { "automatedTestTypeId", "13cdc9d9-ddb5-4fa4-a97d-d965ccfc6d4b" } // This is used in the sample response and also appears in web searches
                    };
                    if (!string.IsNullOrEmpty(_filename))
                    {
                        properties.Add("automatedTestStorage", _filename);
                    }
                    return properties.ToJson();
                })) + " ]";
                string responseString = await _apiClient.SendAsync(HttpMethod.Post, _testRunEndpoint, "5.0-preview.5", request, cancellationToken);
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
                        _parentIds.Add(parentsToAdd[c], ((JsonObject)parents[c]).ValueAsInt("id"));
                    }
                }
            }

            // At this point all the parents should have been created
            return testResultsByParent;
        }

        private async Task SendTestResults(IEnumerable<IGrouping<string, TestResult>> testResultsByParent, CancellationToken cancellationToken)
        {
            string request = "[ " + string.Join(", ", testResultsByParent.Select(x =>
            {
                int parentId = _parentIds[x.Key];
                string subResults = "[ " + string.Join(", ", x.Select(t =>
                {
                    Dictionary<string, object> properties = new Dictionary<string, object>
                    {
                        // TODO: copy data from TestResultItem then delete it
                    };
                    return properties.ToJson();
                })) + " ]";
                return $"{{ \"id\": { parentId }, \"subResults\": { subResults } }}";
            })) + " ]";
            await _apiClient.SendAsync(new HttpMethod("PATCH"), _testRunEndpoint, "5.0-preview.5", request, cancellationToken);
        }
    }
}