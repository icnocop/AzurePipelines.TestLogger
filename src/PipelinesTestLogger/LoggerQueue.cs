using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PipelinesTestLogger
{
    internal class LoggerQueue
    {
        private static readonly HttpClient _client = new HttpClient();

        private readonly string _apiUrl;

        private readonly AsyncProducerConsumerCollection<string> _queue = new AsyncProducerConsumerCollection<string>();
        private readonly Task _consumeTask;
        private readonly CancellationTokenSource _consumeTaskCancellationSource = new CancellationTokenSource();

        private int totalEnqueued = 0;
        private int totalSent = 0;

        public LoggerQueue(string accessToken, string apiUrl)
        {
            // The : character delimits username (which should be empty here) and password in basic auth headers
            _client.DefaultRequestHeaders.Authorization
                = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(
                        ASCIIEncoding.ASCII.GetBytes($":{ accessToken }")));
            _apiUrl = apiUrl;
            Console.WriteLine($"API URL: {_apiUrl}");
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
            HttpContent content = new StringContent(jsonArray, Encoding.UTF8, "application/json");
            try
            {
                Console.WriteLine("POST" + Environment.NewLine + jsonArray);
                HttpResponseMessage response = await _client.PostAsync(_apiUrl, content, cancellationToken);
                response.EnsureSuccessStatusCode();
                totalSent += jsonEntities.Count;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}