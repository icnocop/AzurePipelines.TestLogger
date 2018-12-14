namespace AzurePipelines.TestLogger
{
    internal class TestResultParent
    {
        public int Id { get; }

        public long Duration { get; set; }

        public TestResultParent(int id)
        {
            Id = id;
        }
    }
}