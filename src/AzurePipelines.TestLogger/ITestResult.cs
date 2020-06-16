using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace AzurePipelines.TestLogger
{
    internal interface ITestResult
    {
        Guid Id { get; }
        string Source { get; }
        string FullyQualifiedName { get; }
        string DisplayName { get; }
        TestOutcome Outcome { get; }
        TimeSpan Duration { get; }
        string ErrorStackTrace { get; }
        string ErrorMessage { get; }
        IList<TestResultMessage> Messages { get; }
        string ComputerName { get; }
        Collection<AttachmentSet> Attachments { get; }
    }
}
