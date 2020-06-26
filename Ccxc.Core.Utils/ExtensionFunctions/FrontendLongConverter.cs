using Newtonsoft.Json;
using System;

namespace Ccxc.Core.Utils.ExtensionFunctions
{
    public class FrontendLongConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            var nullType = Nullable.GetUnderlyingType(objectType);
            var unboxType = nullType ?? objectType;

            switch (unboxType.Name)
            {
                case "Int64":
                case "UInt64":
                    return true;
                default:
                    return false;
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var number = reader.Value as string;
            if (objectType == typeof(ulong))
            {
                ulong.TryParse(number, out var res);
                return res;
            }
            else
            {
                long.TryParse(number, out var res);
                return res;
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var v = value is ulong uv ? (long)uv : (long)value;
            writer.WriteValue(v.ToString());
        }
    }
}
