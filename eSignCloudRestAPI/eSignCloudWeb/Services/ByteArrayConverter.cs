using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace eSignCloudWeb.Services
{
    public class ByteArrayConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(byte[]) || objectType == typeof(IEnumerable<byte>);
        }

        public override bool CanWrite => true;

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value is byte[] bytes)
            {
                writer.WriteStartArray();
                for (int i = 0; i < bytes.Length; i++)
                {
                    writer.WriteValue((int)bytes[i]);
                }
                writer.WriteEndArray();
            }
            else
            {
                writer.WriteNull();
            }
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            var token = JToken.Load(reader);
            if (token == null || token.Type == JTokenType.Null)
                return null;

            switch (token.Type)
            {
                case JTokenType.String:
                    var str = (string?)token;
                    return string.IsNullOrEmpty(str) ? null : Convert.FromBase64String(str);

                case JTokenType.Array:
                    return token.ToObject<byte[]>();

                case JTokenType.Object:
                    {
                        var value = (string?)token["$value"];
                        return string.IsNullOrEmpty(value) ? null : Convert.FromBase64String(value);
                    }

                default:
                    throw new JsonSerializationException("Unknown byte array format: " + token.Type);
            }
        }
    }
}
