using System;
using System.Collections.Generic;

namespace EnglishTek.Core
{
    internal static class CatalogFilter
    {
        // Returns the normalized category for an entry, defaulting to "general".
        private static string EffectiveCategory(string raw)
        {
            string normalized = CatalogStringHelper.NormalizeCategory(raw);
            return string.IsNullOrEmpty(normalized) ? "general" : normalized;
        }

        // Returns the normalized unit for an entry, defaulting to "general".
        private static string EffectiveUnit(string raw)
        {
            string normalized = CatalogStringHelper.NormalizeUnit(raw);
            return string.IsNullOrEmpty(normalized) ? "general" : normalized;
        }

        internal static bool HasCategory(IReadOnlyList<InteractiveCatalogEntry> interactives, string category)
        {
            if (string.IsNullOrEmpty(category))
            {
                return false;
            }

            for (int index = 0; index < interactives.Count; index++)
            {
                InteractiveCatalogEntry entry = interactives[index];
                if (entry == null) continue;

                if (string.Equals(EffectiveCategory(entry.category), category, StringComparison.OrdinalIgnoreCase))
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
                string category = EffectiveCategory(entry != null ? entry.category : null);
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
                if (entry == null) continue;

                if (!string.Equals(EffectiveCategory(entry.category), category, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string unit = EffectiveUnit(entry.unit);
                if (seen.Add(unit))
                {
                    units.Add(unit);
                }
            }

            return units;
        }
    }
}
