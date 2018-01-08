using System;
using System.Linq;
using xTile;
using xTile.Display;
using xTile.Layers;
using xTile.ObjectModel;
using xTile.Tiles;

namespace MapEditorFramework
{
    public class MapEditor : IMapEditor
    {
        public void Patch(Map targetMap, Diff mapDiff, IDisplayDevice displayDevice)
        {
            Utilities.MergeProperties(targetMap.Properties, mapDiff.Map.Properties);

            Diff.MapData mapData = mapDiff.Map;

            // Add tilesheets
            MergeTileSheets(targetMap, mapData.TileSheets, displayDevice);

            MergeLayers(targetMap, mapData.Layers);
        }

        private void MergeTileSheets(Map targetMap, Diff.TileSheetData[] tileSheets, IDisplayDevice displayDevice)
        {
            if (tileSheets == null)
                return;

            void AddTileSheet(Diff.TileSheetData tileSheetData)
            {
                TileSheet tileSheet = new TileSheet(
                            id: tileSheetData.Id,
                            map: targetMap,
                            imageSource: tileSheetData.ImageSource,
                            sheetSize: tileSheetData.SheetSize,
                            tileSize: tileSheetData.TileSize
                        );
                targetMap.AddTileSheet(tileSheet);
            }

            foreach (Diff.TileSheetData tileSheetData in tileSheets)
            {
                TileSheet tileSheet = targetMap.GetTileSheet(tileSheetData.Id);
                switch (tileSheetData.EditType)
                {
                    case EditType.Add:
                        AddTileSheet(tileSheetData);
                        break;

                    case EditType.Delete:
                        targetMap.RemoveTileSheet(tileSheet);
                        break;

                    case EditType.Merge:
                    case EditType.Replace:
                        targetMap.RemoveTileSheet(tileSheet);
                        AddTileSheet(tileSheetData);
                        break;
                }
            }

            targetMap.LoadTileSheets(displayDevice);
        }

        private void MergeLayers(Map targetMap, Diff.LayerData[] layers)
        {
            void AddLayer(Diff.LayerData layerData, int index)
            {
                Layer layer = new Layer(layerData.Id, targetMap, layerData.LayerSize, layerData.TileSize)
                {
                    Visible = layerData.Visible,
                };
                layer.Properties.CopyFrom(layerData.Properties);
                MergeTiles(layer, layer.Tiles, layerData.Tiles);
                targetMap.InsertLayer(layer, index);
            }

            void MergeLayer(Layer layer, Diff.LayerData layerData)
            {
                layer.Visible = layerData.Visible;
                layer.LayerSize = layerData.LayerSize;
                layer.TileSize = layerData.TileSize;
                Utilities.MergeProperties(layer.Properties, layerData.Properties);
                MergeTiles(layer, layer.Tiles, layerData.Tiles);
            }

            for (int i = 0; i < layers.Length; ++i)
            {
                Diff.LayerData layerData = layers[i];
                Layer layer = targetMap.GetLayer(layerData.Id);
                switch (layerData.EditType)
                {
                    case EditType.Delete:
                        targetMap.RemoveLayer(layer);
                        break;

                    case EditType.Add:
                        AddLayer(layerData, i);
                        break;

                    case EditType.Replace:
                    case EditType.Merge:
                        MergeLayer(layer, layerData);
                        break;
                }
            }
        }

        private void MergeTiles(Layer layer, TileArray tiles, Diff.TileData[] tilesData)
        {
            TileSheet GetTileSheetForTile(Diff.TileData tiledata)
            {
                return layer.Map.TileSheets.First(ts => ts.Id == tiledata.OwningTileSheetId);
            }

            foreach (Diff.TileData tileData in tilesData)
            {
                Tile newTile = null;
                if (tileData is Diff.AnimatedTileData animatedTileData)
                {
                    StaticTile[] frames = animatedTileData.Frames
                        .Select(fd => new StaticTile(layer, GetTileSheetForTile(fd), fd.BlendMode, fd.TileIndex))
                        .ToArray();
                    newTile = new AnimatedTile(layer, frames, animatedTileData.FrameInterval);
                }

                newTile = newTile ?? new StaticTile(layer, GetTileSheetForTile(tileData), tileData.BlendMode, tileData.TileIndex);
                // TODO: Should we merge these in case another mod has edited them?
                newTile.Properties.CopyFrom(tileData.Properties);
                newTile.TileIndexProperties.CopyFrom(tileData.TileIndexProperties);

                tiles[tileData.X, tileData.Y] = newTile;
            }
        }
    }
}
