using System;
using System.Net.NetworkInformation;
using Newtonsoft.Json;

namespace Shibari.Sub.Core.Shared.IPC.Converter
{
    public class PhysicalAddressConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(PhysicalAddress) == objectType;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            serializer.TypeNameHandling = TypeNameHandling.None;
            return new PhysicalAddress(serializer.Deserialize<byte[]>(reader));
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, ((PhysicalAddress)value).GetAddressBytes());
        }
    }
}