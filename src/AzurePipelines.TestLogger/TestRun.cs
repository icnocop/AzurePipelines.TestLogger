using System;

namespace AzurePipelines.TestLogger
{
    internal class TestRun
    {
        public string Name { get; set; }

        public string BuildId { get; set; }

        public DateTime StartedDate { get; set; }

        public bool IsAutomated { get; set; }
    }
}