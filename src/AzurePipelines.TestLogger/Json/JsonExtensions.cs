using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace AzurePipelines.TestLogger.Json
{
    // From https://github.com/xunit/xunit/blob/master/src/xunit.runner.reporters/JsonExtentions.cs
    public static class JsonExtentions
    {
        public static string ToJson(this IDictionary<string, object> data)
        {
            StringBuilder sb = new StringBuilder();

            foreach (KeyValuePair<string, object> kvp in data)
            {
                AddValue(sb, kvp.Key, kvp.Value);
            }

            return "{" + sb.ToString() + "}";
        }

        static void AddValue(StringBuilder sb, string name, object value)
        {
            if (value == null)
            {
                return;
            }

            if (sb.Length != 0)
            {
                sb.Append(',');
            }

            if (value is int || value is long || value is float || value is double || value is decimal)
            {
                sb.AppendFormat(@"""{0}"":{1}", name, Convert.ToString(value, CultureInfo.InvariantCulture));
            }
            else if (value is bool)
            {
                sb.AppendFormat(@"""{0}"":{1}", name, value.ToString().ToLower());
            }
            else if (value is DateTime dt)
            {
                sb.AppendFormat(@"""{0}"":""{1}""", name, dt.ToString("o", CultureInfo.InvariantCulture));
            }
            else if (value is IDictionary<string, object> dict)
            {
                sb.AppendFormat(@"""{0}"":{1}", name, dict.ToJson()); // sub-object
            }
            else
            {
                sb.AppendFormat(@"""{0}"":""{1}""", name, JsonEscape(value.ToString()));
            }
        }

        static string JsonEscape(string value)
        {
            StringBuilder sb = new StringBuilder();

            foreach (char c in value)
            {
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append(@"\"""); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\n': sb.Append("\\n"); break;
                    default:
                        if (c < 32)
                        {
                            sb.AppendFormat("\\u{0:X4}", (int)c);
                        }
                        else
                        {
                            sb.Append(c);
                        }

                        break;
                }
            }

            return sb.ToString();
        }
    }
}
