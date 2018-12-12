using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using PipelinesTestLogger.Json;

namespace PipelinesTestLogger
{
    [FriendlyName(PipelinesTestLogger.FriendlyName)]
    [ExtensionUri(PipelinesTestLogger.ExtensionUri)]
    public class PipelinesTestLogger : ITestLogger
    {
        /// <summary>
        /// Uri used to uniquely identify the logger.
        /// </summary>
        public const string ExtensionUri = "logger://Microsoft/TestPlatform/PiplinesTestLogger/v1";

        /// <summary>
        /// Alternate user friendly string to uniquely identify the logger.
        /// </summary>
        public const string FriendlyName = "PipelinesTestLogger";

        private LoggerQueue _queue;

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

            ApiClient apiClient = new ApiClient(accessToken, collectionUri, teamProject);
            _queue = new LoggerQueue(apiClient, buildId, agentName, jobName);

            // Register for the events.
            events.TestRunMessage += TestMessageHandler;
            events.TestResult += TestResultHandler;
            events.TestRunComplete += TestRunCompleteHandler;
        }

        private bool GetRequiredVariable(string name, out string value)
        {
            value = Environment.GetEnvironmentVariable(name);
            if(string.IsNullOrEmpty(value))
            {
                Console.WriteLine($"PipelinesTestLogger: Not an Azure Pipelines test run, environment variable { name } not set.");
                return false;
            }
            return true;
        }

        private void TestMessageHandler(object sender, TestRunMessageEventArgs e)
        {
            // Add code to handle message
        }

        private void TestResultHandler(object sender, TestResultEventArgs e)
        {
            string filename = string.IsNullOrEmpty(e.Result.TestCase.Source) ? string.Empty : Path.GetFileName(e.Result.TestCase.Source);

            Dictionary<string, object> testResult = new Dictionary<string, object>()
            {
                { "testCaseTitle", e.Result.TestCase.DisplayName },
                { "automatedTestName", e.Result.TestCase.FullyQualifiedName },
                { "outcome", e.Result.Outcome.ToString() },
                { "state", "Completed" },
                { "automatedTestType", "UnitTest" },
                { "automatedTestTypeId", "13cdc9d9-ddb5-4fa4-a97d-d965ccfc6d4b" }, // This is used in the sample response and also appears in web searches
            };

            if (!string.IsNullOrEmpty(filename))
            {
                testResult.Add("automatedTestStorage", filename);
            }

            if (e.Result.Outcome == TestOutcome.Passed || e.Result.Outcome == TestOutcome.Failed)
            {
                int duration = Convert.ToInt32(e.Result.Duration.TotalMilliseconds);
                testResult.Add("durationInMs", duration.ToString(CultureInfo.InvariantCulture));

                string errorStackTrace = e.Result.ErrorStackTrace;
                if (!string.IsNullOrEmpty(errorStackTrace))
                {
                    testResult.Add("stackTrace", errorStackTrace);
                }

                string errorMessage = e.Result.ErrorMessage;
                StringBuilder stdErr = new StringBuilder();
                StringBuilder stdOut = new StringBuilder();
                foreach (TestResultMessage m in e.Result.Messages)
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
                    testResult.Add("errorMessage", $"{errorMessage}\n{stdErr}\n{stdOut}");
                }
            }
            else
            {
                // Handle output type skip, NotFound and None
            }

            _queue.Enqueue(testResult.ToJson());
        }

        private void TestRunCompleteHandler(object sender, TestRunCompleteEventArgs e)
        {
            _queue.Flush();
        }
    }
}
