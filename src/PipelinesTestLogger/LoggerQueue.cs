using PipelinesTestLogger.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PipelinesTestLogger
{
    internal class LoggerQueue
    {
        private readonly ApiClient _apiClient;
        private readonly string _runName;
        private readonly string _buildId;

        private readonly AsyncProducerConsumerCollection<string> _queue = new AsyncProducerConsumerCollection<string>();
        private readonly Task _consumeTask;
        private readonly CancellationTokenSource _consumeTaskCancellationSource = new CancellationTokenSource();
        
        private string _runEndpoint = null;
        private int totalEnqueued = 0;
        private int totalSent = 0;

        public LoggerQueue(ApiClient apiClient, string buildId, string agentName, string jobName)
        {
            _apiClient = apiClient;
            _buildId = buildId;
            _runName = $"{ jobName } on { agentName } at {DateTime.UtcNow.ToString("o")}";
            _consumeTask = ConsumeItemsAsync(_consumeTaskCancellationSource.Token);
        }

        public void Enqueue(string json)
        {
            _queue.Add(json);
            totalEnqueued++;
        }

        public void Flush()
        {
            // Cancel any idle consumers and let them return
            _queue.Cancel();

            try
            {
                // Any active consumer will circle back around and batch post the remaining queue.
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
                string[] nextItems = await _queue.TakeAsync();

                if (nextItems == null || nextItems.Length == 0)
                {
                    // Queue is canceling and is empty
                    return;      
                }

                await PostResultsAsync(nextItems, cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
            }
        }

        private async Task PostResultsAsync(ICollection<string> jsonEntities, CancellationToken cancellationToken)
        {
            string jsonArray = "[" + string.Join(",", jsonEntities) + "]";
            try
            {
                // Make sure we have a test run
                if(_runEndpoint == null)
                {
                    int runId = await CreateTestRun(cancellationToken);
                    _runEndpoint = $"/{runId}/results";
                }

                // Post the result(s)
                await _apiClient.PostAsync(jsonArray, cancellationToken, _runEndpoint);
                totalSent += jsonEntities.Count;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private async Task<int> CreateTestRun(CancellationToken cancellationToken)
        {
            Dictionary<string, object> request = new Dictionary<string, object>
            {
                { "name", _runName },
                { "build", new Dictionary<string, object> { { "id", _buildId } } },
                { "isAutomated", true }
            };
            JsonObject result = await _apiClient.PostAsync(request.ToJson(), cancellationToken);
            return result.ValueAsInt("id");
        }
    }
}