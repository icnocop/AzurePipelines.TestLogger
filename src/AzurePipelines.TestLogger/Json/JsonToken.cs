namespace AzurePipelines.TestLogger.Json
{
    internal struct JsonToken
    {
        public JsonTokenType Type;
        public string Value;
        public int Line;
        public int Column;
    }
}

