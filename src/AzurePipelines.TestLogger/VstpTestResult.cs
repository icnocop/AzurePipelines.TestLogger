using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace AzurePipelines.TestLogger
{
    internal class VstpTestResult : ITestResult
    {
        private readonly TestResult _testResult;

        public VstpTestResult(TestResult testResult)
        {
            _testResult = testResult;
        }

        public string Source => _testResult.TestCase.Source;

        public string FullyQualifiedName => _testResult.TestCase.FullyQualifiedName;

        public string DisplayName => _testResult.TestCase.DisplayName;

        public TestOutcome Outcome => _testResult.Outcome;

        public TimeSpan Duration => _testResult.Duration;

        public string ErrorStackTrace => _testResult.ErrorStackTrace;

        public string ErrorMessage => _testResult.ErrorMessage;

        public IList<TestResultMessage> Messages => _testResult.Messages;
    }
}
