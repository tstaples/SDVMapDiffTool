using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapEditorFramework
{
    public class SerializationSettings : JsonSerializerSettings
    {
        public SerializationSettings()
        {
#if DEBUG
            Formatting = Formatting.Indented;
#else
            Formatting = Formatting.None;
#endif
            // We need this enabled to distinguish between animated and static tiles.
            TypeNameHandling = TypeNameHandling.Auto;
        }
    }
}
