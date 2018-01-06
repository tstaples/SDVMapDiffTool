using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using xTile;
using xTile.Format;
using xTile.ObjectModel;
using xTile.Tiles;
using xTile.Dimensions;
using Newtonsoft.Json;
using xTile.Layers;

namespace MapDiffGenerator
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
    internal enum EditType
    {
        Add,
        Merge,
        Replace,
        Delete
    }

    internal class Diff
    {
        internal class DiffData
        {
            public EditType EditType;
            public IPropertyCollection Properties;
        }

        internal class MapData : DiffData
        {
            public TileSheetData[] TileSheets;
            public LayerData[] Layers;
        }

        internal class TileSheetData : DiffData
        {
            public string Id;
            public string ImageSource;
            public Size SheetSize;
            public Size TileSize;
        }

        internal class LayerData : DiffData
        {
            public string Id;
            public bool Visible;
            public Size LayerSize;
            public Size TileSize;
            public TileData[] Tiles;
        }

        internal class TileData : DiffData
        {
            public BlendMode BlendMode;
            public string OwningTileSheetId;
            public int TileIndex;
        }

        internal class StaticTileData : TileData
        {
        }

        internal class AnimatedTileData : TileData
        {
            public long FrameInterval;
            public List<StaticTileData> Frames;
        }

        // Could add a version here or something if needed.
        public MapData Map;
    }

    internal class DiffGenerator
    {
        private Map ModifiedMap;
        private Map ReferenceMap;

        public void GenerateDiff(string customMapPath, string referenceMapPath)
        {
            FormatManager formatManager = FormatManager.Instance;
            this.ModifiedMap = formatManager.LoadMap(customMapPath);
            this.ReferenceMap = formatManager.LoadMap(referenceMapPath);

            Diff diff = new Diff();
            diff.Map = GetMapData();
            string data = JsonConvert.SerializeObject(diff);
            
            Console.WriteLine(data);
            // TODO: write to dest path
        }

        private Diff.MapData GetMapData()
        {
            return new Diff.MapData()
            {
                EditType = EditType.Merge,
                Properties = GetDifference(this.ModifiedMap.Properties, this.ReferenceMap.Properties),
                TileSheets = GetTilesheetData(),
                Layers = GetLayerData()
            };
        }

        private Diff.TileSheetData[] GetTilesheetData()
        {
            List<Diff.TileSheetData> tileSheets = new List<Diff.TileSheetData>();

            foreach (TileSheet tileSheet in this.ModifiedMap.TileSheets)
            {
                TileSheet referenceTileSheet = this.ReferenceMap.TileSheets.FirstOrDefault(t => t.Id == tileSheet.Id);
                if (referenceTileSheet == null || DoTileSheetsDiffer(tileSheet, referenceTileSheet))
                {
                    Diff.TileSheetData tileSheetData = new Diff.TileSheetData()
                    {
                        EditType = referenceTileSheet == null ? EditType.Add : EditType.Merge,
                        Properties = GetDifference(tileSheet.Properties, referenceTileSheet?.Properties),
                        Id = tileSheet.Id,
                        ImageSource = tileSheet.ImageSource, // May need to just get filename and ignore full path
                        SheetSize = tileSheet.SheetSize,
                        TileSize = tileSheet.SheetSize
                    };

                    tileSheets.Add(tileSheetData);
                }
            }
            return tileSheets.Count > 0 ? tileSheets.ToArray() : null;
        }

        private Diff.LayerData[] GetLayerData()
        {
            var layerData = new List<Diff.LayerData>();
            foreach (Layer layer in this.ModifiedMap.Layers)
            {
                Layer referenceLayer = this.ReferenceMap.Layers.FirstOrDefault(l => l.Id == layer.Id);
                if (referenceLayer == null || DoLayersDiffer(layer, referenceLayer))
                {
                    layerData.Add(new Diff.LayerData()
                    {
                        EditType = referenceLayer == null ? EditType.Add : EditType.Merge,
                        Properties = GetDifference(layer.Properties, referenceLayer?.Properties),
                        Id = layer.Id,
                        Visible = layer.Visible,
                        LayerSize = layer.LayerSize,
                        TileSize = layer.TileSize,
                        //Tiles = GetTileData(layer, referenceLayer) // if the ref layer is null, all are add. otherwise only return the tiles ref layer doesn't have.
                    });
                }
            }
            return layerData.Count > 0 ? layerData.ToArray() : null;
        }

        private IPropertyCollection GetDifference(IPropertyCollection a, IPropertyCollection b, bool overwriteConflicting = true, bool nullIfEmpty = true)
        {
            if (b == null)
            {
                return a;
            }

            IPropertyCollection properties = new PropertyCollection();
            foreach (var property in a)
            {
                // Only add it if it's not in the vanilla map properties or we changed the value.
                // This way we only set things we actually changed.
                if (!b.ContainsKey(property.Key) ||
                    (overwriteConflicting && b[property.Key] != property.Value))
                {
                    properties.Add(property.Key, property.Value);
                }
            }
            return (properties.Count > 0 || !nullIfEmpty) ? properties : null;
        }

        private bool DoTileSheetsDiffer(TileSheet a, TileSheet b)
        {
            return !(a.Id == b.Id && a.ImageSource == b.ImageSource && a.SheetSize == b.SheetSize && a.TileSize == b.TileSize);
        }

        private bool DoLayersDiffer(Layer a, Layer b)
        {
            // TODO: probably have to iter over each tile to see if they differ.
            // We may as well do this while getting the tile diff data though since we have to iter over them all and check anyway.
            if (a.Id == b.Id && a.LayerSize == b.LayerSize && a.TileSize == b.TileSize /*&& a.Tiles. == b.Tiles*/)
            {

                //for (int i = 0; i < a.Tiles.l)
                return true;
            }
            return false;
        }

        private bool DoTilesDiffer(Tile a, Tile b)
        {
            return !(a.Layer.Id == b.Layer.Id && a.TileIndex == b.TileIndex && a.BlendMode == b.BlendMode && a.TileIndexProperties == b.TileIndexProperties);
        }
    }
}
