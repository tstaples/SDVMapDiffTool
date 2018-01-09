using MapEditorFramework;
using StardewModdingAPI;

namespace MapEditor
{
    internal class PathResolvingTileSheetGroup : SeasonalTileSheetGroup
    {
        public IModHelper Helper;

        public PathResolvingTileSheetGroup(IModHelper helper)
        {
            this.Helper = helper;
        }

        public override string GetTileSheetPath()
        {
            // Resolve the proper path.
            return this.Helper.Content.GetActualAssetKey(base.GetTileSheetPath());
        }
    }
}
