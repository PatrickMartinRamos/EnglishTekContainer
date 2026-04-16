using System;
using System.Collections.Generic;

namespace EnglishTek.Core
{
    internal static class CatalogFilter
    {
        internal static bool HasCategory(IReadOnlyList<InteractiveCatalogEntry> interactives, string category)
        {
            if (string.IsNullOrEmpty(category))
            {
                return false;
            }

            for (int index = 0; index < interactives.Count; index++)
            {
                InteractiveCatalogEntry entry = interactives[index];
                if (entry == null)
                {
                    continue;
                }

                string entryCategory = CatalogStringHelper.NormalizeCategory(entry.category);
                if (string.IsNullOrEmpty(entryCategory))
                {
                    entryCategory = "general";
                }

                if (string.Equals(entryCategory, category, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        internal static List<string> BuildUniqueCategories(IReadOnlyList<InteractiveCatalogEntry> interactives)
        {
            List<string> categories = new List<string>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int index = 0; index < interactives.Count; index++)
            {
                InteractiveCatalogEntry entry = interactives[index];
                string category = CatalogStringHelper.NormalizeCategory(entry != null ? entry.category : null);
                if (string.IsNullOrEmpty(category))
                {
                    category = "general";
                }

                if (seen.Add(category))
                {
                    categories.Add(category);
                }
            }

            return categories;
        }

        internal static List<string> BuildUnitsForCategory(IReadOnlyList<InteractiveCatalogEntry> interactives, string category)
        {
            List<string> units = new List<string>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrEmpty(category))
            {
                return units;
            }

            for (int index = 0; index < interactives.Count; index++)
            {
                InteractiveCatalogEntry entry = interactives[index];
                if (entry == null)
                {
                    continue;
                }

                string entryCategory = CatalogStringHelper.NormalizeCategory(entry.category);
                if (string.IsNullOrEmpty(entryCategory))
                {
                    entryCategory = "general";
                }

                if (!string.Equals(entryCategory, category, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string unit = CatalogStringHelper.NormalizeUnit(entry.unit);
                if (string.IsNullOrEmpty(unit))
                {
                    unit = "general";
                }

                if (seen.Add(unit))
                {
                    units.Add(unit);
                }
            }

            return units;
        }
    }
}
