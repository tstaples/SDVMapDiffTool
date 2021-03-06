﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using xTile;
using xTile.Format;
using xTile.ObjectModel;
using xTile.Tiles;
using Newtonsoft.Json;
using xTile.Layers;
using MapEditorFramework;

namespace MapDiffGenerator
{
    /*
     * - xnb mods -> use my map editor mod
     * - code mods -> add dependency since they have their own code
     *  - MapEditor.dll
     *      - use it like TBO does now:
     *          - provide a tilesheet provider etc.
     * 
     * MapEditorMod/
     *  Mods/
     *      SomeFarmMod/
     *          Config.json
     *              {
     *                  "map" : "Farm", // use this name to find the folder and diff data
     *                  "spring_tilesheet" : "zspring_Farm_Tilesheet.png",
     *                  ...
     *              }
     *              {
     *                  "map" : "FarmHouse",
     *                  "spring_tilesheet" : "zspring_FarmHouse_Tilesheet.png",
     *                  ...
     *              }
     *          Farm/
     *              Farm.json
     *              zspring_Farm_Tilesheet.png
     *              zspring_Fall_Tilesheet.png
     *          FarmHouse/
     *              FarmHouse.json
     *              zFarmHouse_Tilesheet.png
     *      SomeTownMod/
     *          ...
     *
     * Modules:
     *  - DiffGenerator.exe
     *      - creates the diff
     *  - MapEditor.dll
     *      - common data structures
     *          - diff
     *          - tilesheet provider
     *      - deserializing diff
     *      - applying changes to a game location
     *  - MapEditorMod.dll
     *      - creates a tilesheet provider from each mod's config and applies the changes
     */
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

            string data = JsonConvert.SerializeObject(diff, Serialization.Settings);
            File.WriteAllText("../../../TestData/diff.json", data);
        }

        private Diff.MapData GetMapData()
        {
            return new Diff.MapData()
            {
                EditType = EditType.Merge,
                Properties = Utilities.GetDifference(this.ModifiedMap.Properties, this.ReferenceMap.Properties),
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
                        Properties = Utilities.GetDifference(tileSheet.Properties, referenceTileSheet?.Properties),
                        Id = tileSheet.Id,
                        ImageSource = tileSheet.ImageSource, // This is just the filename.extension
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
                        Properties = Utilities.GetDifference(layer.Properties, referenceLayer?.Properties),
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
                                TileIndex = referenceTile.TileIndex,
                                X = x,
                                Y = y
                            });
                        }
                        continue;
                    }

                    if (referenceTile == null || DoTilesDiffer(modifiedTile, referenceTile))
                    {
                        Console.WriteLine($"{(referenceTile == null ? EditType.Add : EditType.Replace)} tile: {modifiedTile.TileIndex} from layer: {modifiedLayer.Id}");

                        Diff.TileData newTile = null;
                        if (modifiedTile is AnimatedTile animatedTile)
                        {
                            Console.WriteLine($"Tile {modifiedTile.TileIndex} is animated");

                            Diff.StaticTileData[] frames = new Diff.StaticTileData[animatedTile.TileFrames.Length];
                            for (int i = 0; i < animatedTile.TileFrames.Length; ++i)
                            {
                                StaticTile copyFrame = animatedTile.TileFrames[i];
                                frames[i] = new Diff.StaticTileData()
                                {
                                    BlendMode = copyFrame.BlendMode,
                                    OwningTileSheetId = copyFrame.TileSheet.Id,
                                    TileIndex = copyFrame.TileIndex,
                                    Properties = copyFrame.Properties,
                                    TileIndexProperties = copyFrame.TileIndexProperties
                                };
                            }

                            newTile = new Diff.AnimatedTileData()
                            {
                                FrameInterval = animatedTile.FrameInterval,
                                Frames = frames
                            };
                        }

                        newTile = newTile ?? new Diff.TileData();
                        newTile.EditType = referenceTile == null ? EditType.Add : EditType.Replace;
                        newTile.BlendMode = modifiedTile.BlendMode;
                        newTile.OwningTileSheetId = modifiedTile.TileSheet.Id;
                        newTile.TileIndex = modifiedTile.TileIndex;
                        newTile.X = x;
                        newTile.Y = y;
                        newTile.Properties = Utilities.GetDifference(modifiedTile.Properties, referenceTile?.Properties);
                        newTile.TileIndexProperties = Utilities.GetDifference(modifiedTile.TileIndexProperties, referenceTile?.TileIndexProperties);

                        tiles.Add(newTile);
                    }
                }
            }
            return tiles.Count > 0 ? tiles.ToArray() : null;
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
                   Utilities.DoPropertiesDiffer(a.TileIndexProperties, b.TileIndexProperties) ||
                   Utilities.DoPropertiesDiffer(a.Properties, b.Properties);
        }
    }
}
