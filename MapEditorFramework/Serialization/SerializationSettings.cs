using Newtonsoft.Json;
using System.Collections.Generic;

namespace MapEditorFramework
{
    public class Serialization
    {
        public static JsonSerializerSettings Settings => new JsonSerializerSettings()
        {
#if DEBUG
            Formatting = Formatting.Indented,
#else
            Formatting = Formatting.None,
#endif
            // We need this enabled to distinguish between animated and static tiles.
            TypeNameHandling = TypeNameHandling.Auto
        };
    }
}
