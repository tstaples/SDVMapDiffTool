using xTile.ObjectModel;
using xTile.Tiles;
using xTile.Dimensions;
using Newtonsoft.Json;

namespace MapEditorFramework
{
    /*
     * mod config will have:
     *  - path to seasonal tilesheets relative to mod folder
     *  
     * Edit Types
     *  - Add - Not contained in reference and isn't replacing something.
     *  - Replace - replace existing
     *  - Delete - Removes
     *  
     * For minimal data only write out values that change. This means the variable name is stored as a string and only added to a dict if it's different.
     *  
     * mapname.json
     * - map properties
     * - custom tilesheets (only need data Tilesheet ctor requires)
     *  - id
     *  - image source path
     *  - sheet size
     *  - tile size
     * - layers
     *  - *edit type
     *  - layer id
     *  - layer properties
     *  - tiles
     *      - *edit type
     *      - blend mode
     *      - tile sheet name
     *      - tile index
     *      - tile index properties
     *      - tile properties
     */
    public enum EditType
    {
        Add,
        Merge,
        Replace,
        Delete
    }

    public class Diff
    {
        public class DiffData
        {
            public EditType EditType;

            [JsonConverter(typeof(PropertyCollectionConverter))]
            public IPropertyCollection Properties;
        }

        public class MapData : DiffData
        {
            public TileSheetData[] TileSheets;
            public LayerData[] Layers;
        }

        public class TileSheetData : DiffData
        {
            public string Id;
            public string ImageSource;
            public Size SheetSize;
            public Size TileSize;
        }

        public class LayerData : DiffData
        {
            public string Id;
            public bool Visible;
            public Size LayerSize;
            public Size TileSize;
            public TileData[] Tiles;
        }

        public class TileData : DiffData
        {
            public BlendMode BlendMode;
            public string OwningTileSheetId;
            public int TileIndex;

            [JsonConverter(typeof(PropertyCollectionConverter))]
            public IPropertyCollection TileIndexProperties;
        }

        public class StaticTileData : TileData
        {
        }

        public class AnimatedTileData : TileData
        {
            public long FrameInterval;
            public StaticTileData[] Frames;
        }

        // Could add a version here or something if needed.
        public MapData Map;
    }
}
