namespace MapEditorFramework
{
    public interface ITileSheetGroup
    {
        // Unique identifier for this tilesheet. This is used as the tilesheet name.
        string UniqueId { get; }

        // Get the path to the tilesheet to use.
        // If this depends on something like season then derived classes must handle that.
        string GetTileSheetPath();
    }
}
