using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using xTile.ObjectModel;

namespace MapDiffGenerator
{
    internal class PropertyCollectionConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IPropertyCollection);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            IPropertyCollection properties = new PropertyCollection();
            try
            {
                serializer.Populate(reader, properties);
            }
            catch (JsonSerializationException ex)
            {
                // The collection was null.
                return null;
            }
            return properties;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            IPropertyCollection propertyCollection = (IPropertyCollection)value;
            Dictionary<string, object> properties = propertyCollection
                .ToDictionary(k => k.Key, v => GetValue(v.Value));
            serializer.Serialize(writer, properties);
        }

        private object GetValue(PropertyValue property)
        {
            if (property.Type == typeof(int))
                return (int)property;
            if (property.Type == typeof(float))
                return (float)property;
            if (property.Type == typeof(bool))
                return (bool)property;
            return (string)property;
        }
    }
}
