namespace MapEditorFramework
{
    public interface ITileSheetProvider
    {
        ITileSheetGroup[] TileSheetGroups { get; }

        ITileSheetGroup GetTileSheetGroupById(string uniqueId);
        string GetTileSheetPath(string uniqueId);
    }
}
