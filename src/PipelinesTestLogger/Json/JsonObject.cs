using System;
using System.Collections.Generic;

namespace PipelinesTestLogger.Json
{
    internal class JsonObject : JsonValue
    {
        private readonly IDictionary<string, JsonValue> _data;

        public JsonObject(IDictionary<string, JsonValue> data, int line, int column)
            : base(line, column)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            _data = data;
        }

        public ICollection<string> Keys
        {
            get { return _data.Keys; }
        }

        public JsonValue Value(string key)
        {
            JsonValue result;
            if (!_data.TryGetValue(key, out result))
            {
                result = null;
            }

            return result;
        }

        public JsonObject ValueAsJsonObject(string key)
        {
            return Value(key) as JsonObject;
        }

        public JsonString ValueAsString(string key)
        {
            return Value(key) as JsonString;
        }

        public int ValueAsInt(string key)
        {
            JsonNumber number = Value(key) as JsonNumber;
            if (number == null)
            {
                throw new FormatException();
            }
            return Convert.ToInt32(number.Raw);
        }

        public bool ValueAsBoolean(string key, bool defaultValue = false)
        {
            JsonBoolean boolVal = Value(key) as JsonBoolean;
            if (boolVal != null)
            {
                return boolVal.Value;
            }

            return defaultValue;
        }

        public bool? ValueAsNullableBoolean(string key)
        {
            JsonBoolean boolVal = Value(key) as JsonBoolean;
            if (boolVal != null)
            {
                return boolVal.Value;
            }

            return null;
        }

        public string[] ValueAsStringArray(string key)
        {
            JsonArray list = Value(key) as JsonArray;
            if (list == null)
            {
                return null;
            }

            string[] result = new string[list.Length];

            for (int i = 0; i < list.Length; ++i)
            {
                JsonString jsonString = list[i] as JsonString;
                result[i] = jsonString?.ToString();
            }

            return result;
        }
    }
}

