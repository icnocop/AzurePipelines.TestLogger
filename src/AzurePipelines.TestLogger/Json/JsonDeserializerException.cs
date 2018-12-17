using System;

namespace AzurePipelines.TestLogger.Json
{
    internal class JsonDeserializerException : Exception
    {
        public JsonDeserializerException(string message, Exception innerException, int line, int column)
            : base(message, innerException)
        {
            Line = line;
            Column = column;
        }

        public JsonDeserializerException(string message, int line, int column)
            : base(message)
        {
            Line = line;
            Column = column;
        }

        public JsonDeserializerException(string message, JsonToken nextToken)
            : base(message)
        {
            Line = nextToken.Line;
            Column = nextToken.Column;
        }

        public int Line { get; }

        public int Column { get; }
    }
}
