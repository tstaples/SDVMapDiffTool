using xTile.Dimensions;

namespace MapEditorFramework
{
    public interface ITileSheetGroup
    {
        // Unique identifier for this tilesheet. This is used as the tilesheet name.
        string UniqueId { get; }

        // The size of your tilesheet image (number of columns, number of rows).
        Size SheetSize { get; }
        // should always be 16x16 for maps
        Size TileSize { get; }

        string GetTileSheetForSeason(string season);
        string GetTileSheetPathForSeason(string season);
    }

    public interface ITileSheetProvider
    {
        ITileSheetGroup[] TileSheetGroups { get; }

        ITileSheetGroup GetTileSheetGroupById(string uniqueId);
    }
}
