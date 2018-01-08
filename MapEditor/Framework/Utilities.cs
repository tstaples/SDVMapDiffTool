using System.Collections.Generic;
using System.Linq;
using xTile.ObjectModel;

namespace MapEditorFramework
{
    public class Utilities
    {
        // TODO: put all these functions into some property utilites class
        public static void MergeProperties(IPropertyCollection a, IPropertyCollection b)
        {
            // TODO
        }

        public static IPropertyCollection GetDifference(IPropertyCollection a, IPropertyCollection b, bool overwriteConflicting = true, bool nullIfEmpty = true)
        {
            if (b == null)
            {
                return a;
            }

            IPropertyCollection properties = new PropertyCollection();
            foreach (var property in a)
            {
                // Only add it if it's not in the vanilla map properties or we changed the value.
                // This way we only set things we actually changed.
                // Need to do ToString() otherwise comparison always fails.
                if (!b.ContainsKey(property.Key) ||
                    (overwriteConflicting && b[property.Key].ToString() != property.Value.ToString()))
                {
                    properties.Add(property.Key, property.Value);
                }
            }
            return (properties.Count > 0 || !nullIfEmpty) ? properties : null;
        }

        public static bool DoPropertiesDiffer(IPropertyCollection a, IPropertyCollection b)
        {
            if (a == null && b == null)
                return false;
            if ((a == null && b != null) || a != null && b == null)
                return true;
            if (a.Count != b.Count)
                return true;
            return GetDifference(a, b) != null;
        }
    }
}
