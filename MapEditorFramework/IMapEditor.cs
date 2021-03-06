﻿using xTile;
using xTile.Display;

namespace MapEditorFramework
{
    public interface IMapEditor
    {
        void Patch(Map targetMap, Diff mapDiff, ITileSheetProvider tileSheetProvider, IDisplayDevice displayDevice);
    }
}
