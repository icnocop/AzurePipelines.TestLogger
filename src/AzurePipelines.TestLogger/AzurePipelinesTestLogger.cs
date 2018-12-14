using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using AzurePipelines.TestLogger.Json;

namespace AzurePipelines.TestLogger
{
    [FriendlyName(AzurePipelinesTestLogger.FriendlyName)]
    [ExtensionUri(AzurePipelinesTestLogger.ExtensionUri)]
    public class AzurePipelinesTestLogger : ITestLogger
    {
        /// <summary>
        /// Uri used to uniquely identify the logger.
        /// </summary>
        public const string ExtensionUri = "logger://Microsoft/TestPlatform/AzurePiplinesTestLogger/v1";

        /// <summary>
        /// Alternate user friendly string to uniquely identify the logger.
        /// </summary>
        public const string FriendlyName = "AzurePipelines";

        private IApiClient _apiClient;
        private LoggerQueue _queue;

        public AzurePipelinesTestLogger()
        {
        }

        // Used for testing
        internal AzurePipelinesTestLogger(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public void Initialize(TestLoggerEvents events, string testRunDirectory)
        {
            if(!GetRequiredVariable("SYSTEM_ACCESSTOKEN", out string accessToken)
                || !GetRequiredVariable("SYSTEM_TEAMFOUNDATIONCOLLECTIONURI", out string collectionUri)
                || !GetRequiredVariable("SYSTEM_TEAMPROJECT", out string teamProject)
                || !GetRequiredVariable("BUILD_BUILDID", out string buildId)
                || !GetRequiredVariable("AGENT_NAME", out string agentName)
                || !GetRequiredVariable("AGENT_JOBNAME", out string jobName))
            {
                return;
            }

            if (_apiClient == null)
            {
                _apiClient = new ApiClient(accessToken, collectionUri, teamProject);
            }
            _queue = new LoggerQueue(_apiClient, buildId, agentName, jobName);

            // Register for the events
            events.TestRunMessage += TestMessageHandler;
            events.TestResult += TestResultHandler;
            events.TestRunComplete += TestRunCompleteHandler;
        }

        private bool GetRequiredVariable(string name, out string value)
        {
            value = Environment.GetEnvironmentVariable(name);
            if(string.IsNullOrEmpty(value))
            {
                Console.WriteLine($"AzurePipelines.TestLogger: Not an Azure Pipelines test run, environment variable { name } not set.");
                return false;
            }
            return true;
        }

        private void TestMessageHandler(object sender, TestRunMessageEventArgs e)
        {
            // Add code to handle message
        }

        private void TestResultHandler(object sender, TestResultEventArgs e) =>
            _queue.Enqueue(new VstpTestResult(e.Result));

        private void TestRunCompleteHandler(object sender, TestRunCompleteEventArgs e) => _queue.Flush();
    }
}
