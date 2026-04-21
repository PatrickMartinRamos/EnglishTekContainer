using System;
using System.Collections.Generic;

namespace Tek.Core
{
    internal static class CatalogFilter
    {
        // Natural sort: compares embedded numbers numerically so "Unit 2" < "Unit 10".
        private static int NaturalCompare(string a, string b)
        {
            int ia = 0, ib = 0;
            while (ia < a.Length && ib < b.Length)
            {
                bool aIsDigit = char.IsDigit(a[ia]);
                bool bIsDigit = char.IsDigit(b[ib]);

                if (aIsDigit && bIsDigit)
                {
                    int numA = 0, numB = 0;
                    while (ia < a.Length && char.IsDigit(a[ia])) numA = numA * 10 + (a[ia++] - '0');
                    while (ib < b.Length && char.IsDigit(b[ib])) numB = numB * 10 + (b[ib++] - '0');
                    int cmp = numA.CompareTo(numB);
                    if (cmp != 0) return cmp;
                }
                else
                {
                    int cmp = char.ToUpperInvariant(a[ia]).CompareTo(char.ToUpperInvariant(b[ib]));
                    if (cmp != 0) return cmp;
                    ia++; ib++;
                }
            }
            return a.Length.CompareTo(b.Length);
        }

        // Returns the normalized label for a raw category or unit string, defaulting to "general".
        private static string EffectiveLabel(string raw)
        {
            string normalized = CatalogStringHelper.NormalizeCategory(raw);
            return string.IsNullOrEmpty(normalized) ? "general" : normalized;
        }

        internal static bool HasCategory(IReadOnlyList<InteractiveCatalogEntry> interactives, string category)
        {
            if (string.IsNullOrEmpty(category)) return false;

            for (int index = 0; index < interactives.Count; index++)
            {
                InteractiveCatalogEntry entry = interactives[index];
                if (entry == null) continue;

                if (string.Equals(EffectiveLabel(entry.category), category, StringComparison.OrdinalIgnoreCase))
                    return true;
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
                string category = EffectiveLabel(entry != null ? entry.category : null);
                if (seen.Add(category)) categories.Add(category);
            }

            categories.Sort(NaturalCompare);
            return categories;
        }

        internal static List<string> BuildUnitsForCategory(IReadOnlyList<InteractiveCatalogEntry> interactives, string category)
        {
            List<string> units = new List<string>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrEmpty(category)) return units;

            for (int index = 0; index < interactives.Count; index++)
            {
                InteractiveCatalogEntry entry = interactives[index];
                if (entry == null) continue;

                if (!string.Equals(EffectiveLabel(entry.category), category, StringComparison.OrdinalIgnoreCase))
                    continue;

                string unit = EffectiveLabel(entry.unit);
                if (seen.Add(unit)) units.Add(unit);
            }

            units.Sort(NaturalCompare);
            return units;
        }
    }
}
