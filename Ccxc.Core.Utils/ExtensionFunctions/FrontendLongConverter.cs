using Newtonsoft.Json;
using System;

namespace Ccxc.Core.Utils.ExtensionFunctions
{
    public class FrontendLongConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            var nullType = Nullable.GetUnderlyingType(objectType);
            var unboxType = nullType != null ? nullType : objectType;

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
            string number = reader.Value as string;
            if (objectType == typeof(ulong))
            {
                ulong.TryParse(number, out ulong res);
                return res;
            }
            else
            {
                long.TryParse(number, out long res);
                return res;
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            long v = value is ulong ? (long)(ulong)value : (long)value;
            writer.WriteValue(v.ToString());
        }
    }
}
