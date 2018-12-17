using System;
using System.Globalization;

namespace AzurePipelines.TestLogger.Json
{
    internal class JsonNumber : JsonValue
    {
        private readonly string _raw;
        private readonly double _double;

        public JsonNumber(JsonToken token)
            : base(token.Line, token.Column)
        {
            try
            {
                _raw = token.Value;
                _double = double.Parse(_raw, NumberStyles.Float);
            }
            catch (FormatException ex)
            {
                throw new JsonDeserializerException(
                    JsonDeserializerResource.Format_InvalidFloatNumberFormat(_raw),
                    ex,
                    token.Line,
                    token.Column);
            }
            catch (OverflowException ex)
            {
                throw new JsonDeserializerException(
                    JsonDeserializerResource.Format_FloatNumberOverflow(_raw),
                    ex,
                    token.Line,
                    token.Column);
            }
        }

        public double Double
        {
            get { return _double; }
        }

        public string Raw
        {
            get { return _raw; }
        }
    }
}
