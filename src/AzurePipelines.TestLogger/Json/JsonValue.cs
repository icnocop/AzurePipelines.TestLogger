namespace AzurePipelines.TestLogger.Json
{
    internal class JsonValue
    {
        public JsonValue(int line, int column)
        {
            Line = line;
            Column = column;
        }

        public int Line { get; }

        public int Column { get; }
    }
}

