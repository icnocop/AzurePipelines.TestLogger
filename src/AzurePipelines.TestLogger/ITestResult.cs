using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace AzurePipelines.TestLogger
{
    internal interface ITestResult
    {        
        string Source { get; }
        string FullyQualifiedName { get; }
        string DisplayName { get; }
        TestOutcome Outcome { get; }
        TimeSpan Duration { get; }
        string ErrorStackTrace { get; }
        string ErrorMessage { get; }
        IList<TestResultMessage> Messages { get; }
    }
}
