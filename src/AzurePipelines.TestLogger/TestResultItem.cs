using AzurePipelines.TestLogger.Json;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace AzurePipelines.TestLogger
{
    // TODO: Delete
    internal class TestResultItem
    {
        public string[] Parents { get; }

        public string Filename { get; }

        public string Json { get; }

        public TestResultItem(TestResult testResult)
        {
            Json = GetJson(testResult, out string filename);
            Filename = filename;
        }

        private static string GetJson(TestResult testResult, out string filename)
        {
            Dictionary<string, object> result = new Dictionary<string, object>()
            {
                { "testCaseTitle", testResult.TestCase.DisplayName },
                { "automatedTestName", testResult.TestCase.FullyQualifiedName },
                { "outcome", testResult.Outcome.ToString() },
                { "state", "Completed" },
                { "automatedTestType", "UnitTest" },
                { "automatedTestTypeId", "13cdc9d9-ddb5-4fa4-a97d-d965ccfc6d4b" }, // This is used in the sample response and also appears in web searches
            };

            filename = string.IsNullOrEmpty(testResult.TestCase.Source) ? string.Empty : Path.GetFileName(testResult.TestCase.Source);
            if (!string.IsNullOrEmpty(filename))
            {
                result.Add("automatedTestStorage", filename);
            }

            if (testResult.Outcome == TestOutcome.Passed || testResult.Outcome == TestOutcome.Failed)
            {
                int duration = Convert.ToInt32(testResult.Duration.TotalMilliseconds);
                result.Add("durationInMs", duration.ToString(CultureInfo.InvariantCulture));

                string errorStackTrace = testResult.ErrorStackTrace;
                if (!string.IsNullOrEmpty(errorStackTrace))
                {
                    result.Add("stackTrace", errorStackTrace);
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
                    result.Add("errorMessage", $"{errorMessage}\n\n---\n\nSTDERR:\n\n{stdErr}\n\n---\n\nSTDOUT:\n\n{stdOut}");
                }
            }
            else
            {
                // Handle output type skip, NotFound and None
            }

            return result.ToJson();
        }
    }
}