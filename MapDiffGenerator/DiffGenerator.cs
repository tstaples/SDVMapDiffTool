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
            
            //Console.WriteLine(data);
            // TODO: write to dest path
            File.WriteAllText("diff.json", data);
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

                    Console.WriteLine($"Adding tilesheet: {tileSheet.Id}");
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
                Diff.TileData[] tileData = GetTileData(layer, referenceLayer);

                if (referenceLayer == null || DoLayersDiffer(layer, referenceLayer) || tileData != null)
                {
                    Console.WriteLine($"Adding layer: {layer.Id}");

                    layerData.Add(new Diff.LayerData()
                    {
                        EditType = referenceLayer == null ? EditType.Add : EditType.Merge,
                        Properties = GetDifference(layer.Properties, referenceLayer?.Properties),
                        Id = layer.Id,
                        Visible = layer.Visible,
                        LayerSize = layer.LayerSize,
                        TileSize = layer.TileSize,
                        Tiles = tileData
                    });
                }
            }
            return layerData.Count > 0 ? layerData.ToArray() : null;
        }

        private Diff.TileData[] GetTileData(Layer modifiedLayer, Layer referenceLayer)
        {
            List<Diff.TileData> tiles = new List<Diff.TileData>();

            Tile GetReferenceTile(int tileX, int tileY)
            {
                if (referenceLayer != null && tileX < referenceLayer.TileWidth && tileY < referenceLayer.TileHeight)
                {
                    return referenceLayer.Tiles[tileX, tileY];
                }
                return null;
            }

            for (int y = 0; y < modifiedLayer.TileHeight; ++y)
            {
                for (int x = 0; x < modifiedLayer.TileWidth; ++x)
                {
                    Tile modifiedTile = modifiedLayer.Tiles[x, y];
                    Tile referenceTile = GetReferenceTile(x, y);

                    // Check if the tile was deleted
                    if (modifiedTile == null)
                    {
                        if (referenceTile != null)
                        {
                            Console.WriteLine($"Deleted tile: {referenceTile.TileIndex} from layer: {modifiedLayer.Id}");

                            tiles.Add(new Diff.TileData()
                            {
                                EditType = EditType.Delete,
                                TileIndex = referenceTile.TileIndex
                            });
                        }
                        continue;
                    }

                    if (referenceTile == null || DoTilesDiffer(modifiedTile, referenceTile))
                    {
                        Console.WriteLine($"{(referenceTile == null ? EditType.Add : EditType.Replace)} tile: {modifiedTile.TileIndex} from layer: {modifiedLayer.Id}");

                        // TODO: check for animated tiles
                        tiles.Add(new Diff.TileData()
                        {
                            EditType = referenceTile == null ? EditType.Add : EditType.Replace,
                            BlendMode = modifiedTile.BlendMode,
                            OwningTileSheetId = modifiedTile.TileSheet.Id,
                            TileIndex = modifiedTile.TileIndex,
                            Properties = GetDifference(modifiedTile.Properties, referenceTile?.Properties)
                        });
                    }
                }
            }
            return tiles.Count > 0 ? tiles.ToArray() : null;
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
                // Need to do ToString() otherwise comparison always fails.
                if (!b.ContainsKey(property.Key) ||
                    (overwriteConflicting && b[property.Key].ToString() != property.Value.ToString()))
                {
                    properties.Add(property.Key, property.Value);
                }
            }
            return (properties.Count > 0 || !nullIfEmpty) ? properties : null;
        }

        private bool DoPropertiesDiffer(IPropertyCollection a, IPropertyCollection b)
        {
            if (a == null && b == null)
                return false;
            if ((a == null && b != null) || a != null && b == null)
                return true;
            if (a.Count != b.Count)
                return true;
            return GetDifference(a, b) != null;
        }

        private bool DoTileSheetsDiffer(TileSheet a, TileSheet b)
        {
            return !(a.Id == b.Id && a.ImageSource == b.ImageSource && a.SheetSize == b.SheetSize && a.TileSize == b.TileSize);
        }

        // Doesn't check tiles
        private bool DoLayersDiffer(Layer a, Layer b)
        {
            return !(a.Id == b.Id && a.LayerSize == b.LayerSize && a.TileSize == b.TileSize);
        }

        private bool DoTilesDiffer(Tile a, Tile b)
        {
            return a.Layer.Id != b.Layer.Id ||
                   a.TileIndex != b.TileIndex ||
                   a.BlendMode != b.BlendMode || 
                   DoPropertiesDiffer(a.TileIndexProperties, b.TileIndexProperties) ||
                   DoPropertiesDiffer(a.Properties, b.Properties);
        }
    }
}
