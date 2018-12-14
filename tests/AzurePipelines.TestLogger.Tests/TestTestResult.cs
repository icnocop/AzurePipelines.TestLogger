using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace AzurePipelines.TestLogger.Tests
{
    public class TestTestResult : ITestResult
    {
        public string Source { get; set; }

        public string FullyQualifiedName { get; set; }

        public string DisplayName { get; set; }

        public TestOutcome Outcome { get; set; }

        public TimeSpan Duration { get; set; }

        public string ErrorStackTrace { get; set; }

        public string ErrorMessage { get; set; }

        public IList<TestResultMessage> Messages { get; } = new List<TestResultMessage>();
    }
}
