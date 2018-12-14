using System;

namespace AzurePipelines.TestLogger
{
    internal class TestResultParent
    {
        public int Id { get; }

        public long Duration { get; set; }

        public DateTime StartedDate { get; }

        public TestResultParent(int id)
            : this(id, DateTime.UtcNow)
        {
        }

        public TestResultParent(int id, DateTime startedDate)
        {
            Id = id;
            StartedDate = startedDate;
        }
    }
}