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
using Newtonsoft.Json.Converters;

namespace MapEditorMod
{
    public class TestTileSheetGroup : SeasonalTileSheetGroup
    {
        public IModHelper Helper;

        public override string GetTileSheetPath()
        {
            return this.Helper.Content.GetActualAssetKey(base.GetTileSheetPath());
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

            /////////////////////////////////////////////////////////////////////////////////
            // BEGIN CONFUSION
            /////////////////////////////////////////////////////////////////////////////////
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                ObjectCreationHandling = ObjectCreationHandling.Replace,
                Converters = new List<JsonConverter> { new StringEnumConverter() }
            };

            // Works
            Diff mapDiff = JsonConvert.DeserializeObject<Diff>(data, settings);

            // Doesn't work
            //string jsonPath = Path.Combine(/*this.Helper.DirectoryPath,*/ "TestData", "diff.json");
            //Diff mapDiff = this.Helper.ReadJsonFile<Diff>(jsonPath);

            /////////////////////////////////////////////////////////////////////////////////
            // END CONFUSION
            /////////////////////////////////////////////////////////////////////////////////

            Map targetMap = Game1.getLocationFromName("Railroad").map;

            TestTileSheetGroup tileSheetGroup = new TestTileSheetGroup()
            {
                Helper = this.Helper,
                UniqueId = TileSheetId,
                TileSheetPaths = Enum.GetValues(typeof(Season))
                .Cast<Season>()
                .Select(s => new KeyValuePair<Season, string>(s, Path.Combine("TestData", $"{TileSheetId}_{s.ToString().ToLower()}.png")))
                .ToDictionary(k => k.Key, v => v.Value)
            };

            IMapEditor mapEditor = new MapEditor();
            var tileSheetProvider = new TileSheetProvider(tileSheetGroup);
            mapEditor.Patch(targetMap, mapDiff, tileSheetProvider, Game1.mapDisplayDevice);
        }
    }
}
