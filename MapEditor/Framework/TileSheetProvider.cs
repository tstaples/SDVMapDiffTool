using System.Linq;

namespace MapEditorFramework
{
    public class TileSheetProvider : ITileSheetProvider
    {
        public ITileSheetGroup[] TileSheetGroups { get; set; }

        public TileSheetProvider()
        {
        }

        public TileSheetProvider(ITileSheetGroup tileSheetGroup)
        {
            this.TileSheetGroups = new ITileSheetGroup[] { tileSheetGroup };
        }

        public TileSheetProvider(ITileSheetGroup[] tileSheetGroups)
        {
            this.TileSheetGroups = tileSheetGroups;
        }

        public ITileSheetGroup GetTileSheetGroupById(string uniqueId)
        {
            return TileSheetGroups.FirstOrDefault(g => g.UniqueId == uniqueId);
        }

        public string GetTileSheetPath(string uniqueId)
        {
            return GetTileSheetGroupById(uniqueId)?.GetTileSheetPath();
        }
    }
}
