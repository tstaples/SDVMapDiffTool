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

namespace MapEditor
{
    public class MapEditorMod : Mod
    {
        public override void Entry(IModHelper helper)
        {
            SaveEvents.AfterLoad += SaveEvents_AfterLoad;

            // Temp for testing
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
            //Diff mapDiff = this.Helper.ReadJsonFile<Diff>(diffPath);
            string data = File.ReadAllText(diffPath);
            Diff mapDiff = JsonConvert.DeserializeObject<Diff>(data, new SerializationSettings());

            Map targetMap = Game1.getLocationFromName("Railroad").map;

            PathResolvingTileSheetGroup tileSheetGroup = new PathResolvingTileSheetGroup(this.Helper)
            {
                UniqueId = TileSheetId,
                TileSheetPaths = Enum.GetValues(typeof(Season))
                .Cast<Season>()
                .Select(s => new KeyValuePair<Season, string>(s, Path.Combine("TestData", $"{TileSheetId}_{s.ToString().ToLower()}.png")))
                .ToDictionary(k => k.Key, v => v.Value)
            };

            IMapEditor mapEditor = new MapEditorFramework.MapEditor();
            var tileSheetProvider = new TileSheetProvider(tileSheetGroup);
            mapEditor.Patch(targetMap, mapDiff, tileSheetProvider, Game1.mapDisplayDevice);
        }
    }
}
