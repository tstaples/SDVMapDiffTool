using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using MapEditorFramework;
using System.IO;
using xTile;
using StardewValley;
using StardewModdingAPI.Events;
using Newtonsoft.Json;
using xTile.Display;
using Microsoft.Xna.Framework.Graphics;
using xTile.Dimensions;
using xTile.Tiles;

namespace MapEditorMod
{
    internal class DisplayDevice : IDisplayDevice
    {
        private ITileSheetProvider TileSheetProvider;
        private IModHelper Helper;

        public DisplayDevice(ITileSheetProvider tsProvider, IModHelper helper)
        {
            this.TileSheetProvider = tsProvider;
            this.Helper = helper;
        }

        public void BeginScene(SpriteBatch b)
        {
            Game1.mapDisplayDevice.BeginScene(b);
        }

        public void DisposeTileSheet(TileSheet tileSheet)
        {
            Game1.mapDisplayDevice.DisposeTileSheet(tileSheet);
        }

        public void DrawTile(Tile tile, Location location, float layerDepth)
        {
            Game1.mapDisplayDevice.DrawTile(tile, location, layerDepth);
        }

        public void EndScene()
        {
            Game1.mapDisplayDevice.EndScene();
        }

        public void LoadTileSheet(TileSheet tileSheet)
        {
            string path = this.TileSheetProvider.GetTileSheetPath(tileSheet.Id);
            if (path == null)
            {
                Game1.mapDisplayDevice.LoadTileSheet(tileSheet);
                return;
            }

            Texture2D texture = this.Helper.Content.Load<Texture2D>(path);
            if (Game1.mapDisplayDevice is XnaDisplayDevice xnaDisplayDevice)
            {
                var tileSheetTextures = this.Helper.Reflection.GetField<Dictionary<TileSheet, Texture2D>>(xnaDisplayDevice, "m_tileSheetTextures");
                var value = tileSheetTextures.GetValue();
                value.Add(tileSheet, texture);
                tileSheetTextures.SetValue(value);
            }
        }

        public void SetClippingRegion(Rectangle clippingRegion)
        {
            Game1.mapDisplayDevice.SetClippingRegion(clippingRegion);
        }
    }

    public class MapEditorMod : Mod
    {
        public override void Entry(IModHelper helper)
        {
            SaveEvents.AfterLoad += SaveEvents_AfterLoad;

            InputEvents.ButtonPressed += (sender, e) =>
            {
                if (e.Button == SButton.F7)
                    Game1.warpFarmer("RailRoad", 14, 58, false);
            };
        }

        private void SaveEvents_AfterLoad(object sender, EventArgs e)
        {
            const string TileSheetId = "zpathtotalbathhouseoverhaulexterior";

            string diffPath = Path.Combine(Helper.DirectoryPath, "TestData/diff.json");
            string data = File.ReadAllText(diffPath);
            Diff mapDiff = JsonConvert.DeserializeObject<Diff>(data);
            //Diff mapDiff = this.Helper.ReadJsonFile<Diff>("TestData/diff.json");

            Map targetMap = Game1.getLocationFromName("Railroad").map;

            SeasonalTileSheetGroup tileSheetGroup = new SeasonalTileSheetGroup()
            {
                UniqueId = TileSheetId,
                TileSheetPaths = Enum.GetValues(typeof(Season))
                .Cast<Season>()
                .Select(s => new KeyValuePair<Season, string>(s, Path.Combine("TestData", $"{TileSheetId}_{s.ToString().ToLower()}.png")))
                .ToDictionary(k => k.Key, v => v.Value)
            };

            IMapEditor mapEditor = new MapEditor();
            var tileSheetProvider = new TileSheetProvider(tileSheetGroup);
            var displayDevice = new DisplayDevice(tileSheetProvider, this.Helper);
            mapEditor.Patch(targetMap, mapDiff, tileSheetProvider, displayDevice);
        }
    }
}
