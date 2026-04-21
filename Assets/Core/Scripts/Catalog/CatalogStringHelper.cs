using System;

namespace EnglishTek.Core
{
    internal static class CatalogStringHelper
    {
        private static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return value.Trim().ToLowerInvariant();
        }

        internal static string NormalizeCategory(string value) => Normalize(value);

        internal static string NormalizeUnit(string value) => Normalize(value);

        internal static string FormatCategoryLabel(string category)
        {
            return TitleCase(Normalize(category));
        }

        internal static string FormatUnitLabel(string unit)
        {
            return TitleCase(Normalize(unit));
        }

        private static string TitleCase(string normalized)
        {
            if (string.IsNullOrEmpty(normalized))
            {
                return "General";
            }

            string[] words = normalized.Split(' ');
            for (int index = 0; index < words.Length; index++)
            {
                if (string.IsNullOrEmpty(words[index]))
                {
                    continue;
                }

                words[index] = char.ToUpperInvariant(words[index][0]) + words[index].Substring(1);
            }

            return string.Join(" ", words);
        }
    }
}
