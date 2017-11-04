using System;
using Newtonsoft.Json;

namespace Shibari.Sub.Core.Shared.Types.Common
{
    public class DualShockDeviceConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var device = new DualShockDevice();

            serializer.TypeNameHandling = TypeNameHandling.None;
            serializer.Populate(reader, device);

            return device;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override bool CanWrite => false;
    }
}