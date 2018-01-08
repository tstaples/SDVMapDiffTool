using System;
using System.Collections.Generic;
using System.Linq;

namespace MapEditorFramework
{
    public enum Season
    {
        Spring,
        Summer,
        Fall,
        Winter
    }

    public abstract class TileSheetGroup : ITileSheetGroup
    {
        public string UniqueId { get; set; }

        public abstract string GetTileSheetPath();
    }

    public class SingleTileSheet : TileSheetGroup
    {
        public string TileSheetPath;

        public override string GetTileSheetPath()
        {
            return this.TileSheetPath;
        }
    }

    public class SeasonalTileSheetGroup : TileSheetGroup
    {
        // If a season doesn't contain a sheet then use sheet for this season instead.
        public Season DefaultSeason { get; set; } = Season.Spring;
        public Func<Season> GetCurrentSeasonFunc;
        public Dictionary<Season, string> TileSheetPaths = new Dictionary<Season, string>();

        public string DefaultTileSheetPath => this.TileSheetPaths.ContainsKey(this.DefaultSeason) ? this.TileSheetPaths[this.DefaultSeason] : null;

        // Helper function for converting season name to the enum.
        public static Season ParseSeason(string seasonName)
        {
            return (Season)Enum.Parse(typeof(Season), seasonName, true);
        }

        public override string GetTileSheetPath()
        {
            Season key = GetCurrentSeasonFunc?.Invoke() ?? this.DefaultSeason;
            return this.TileSheetPaths.ContainsKey(key) ? this.TileSheetPaths[key] : this.DefaultTileSheetPath;
        }
    }
}
