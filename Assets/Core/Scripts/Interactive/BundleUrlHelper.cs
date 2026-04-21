using System;
using System.Collections.Generic;

namespace Tek.Core
{
    /// <summary>
    /// Static helpers for building, normalizing, and encoding URLs and cache keys
    /// used by InteractiveController when resolving bundle paths.
    /// </summary>
    internal static class BundleUrlHelper
    {
        /// <summary>
        /// Normalizes a URL path segment: trims whitespace and leading/trailing slashes.
        /// </summary>
        internal static string NormalizePathPart(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return value.Trim().Replace("\\", "/").Trim('/');
        }

        /// <summary>
        /// Normalizes a game ID for lookup: trims, uppercases, and ensures "ID" prefix.
        /// </summary>
        internal static string NormalizeLookupId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            string normalized = value.Trim().ToUpperInvariant();
            if (!normalized.StartsWith("ID"))
            {
                normalized = "ID" + normalized;
            }

            return normalized;
        }

        /// <summary>
        /// Normalizes a value to a safe file-system key (slashes → underscores).
        /// </summary>
        internal static string NormalizeCacheKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "unknown";
            }

            return value.Trim().Replace("/", "_").Replace("\\", "_").Replace(" ", "_");
        }

        /// <summary>
        /// Percent-encodes each slash-separated segment of a path for use in a URL.
        /// e.g. "Grade 1/grammar/unit1" → "Grade%201/grammar/unit1"
        /// </summary>
        internal static string EncodePathSegments(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            string[] segments = path.Split('/');
            for (int i = 0; i < segments.Length; i++)
            {
                segments[i] = Uri.EscapeDataString(segments[i]);
            }

            return string.Join("/", segments);
        }

        /// <summary>
        /// Builds a relative folder path from grade/category/unit/id, omitting empty parts.
        /// </summary>
        internal static string BuildDefaultFolderPath(string grade, string category, string unit, string gameId)
        {
            string normalizedGrade    = NormalizePathPart(grade);
            string normalizedCategory = NormalizePathPart(category);
            string normalizedUnit     = NormalizePathPart(unit);
            string normalizedGameId   = NormalizePathPart(gameId);

            List<string> parts = new List<string>();
            if (!string.IsNullOrEmpty(normalizedGrade))    parts.Add(normalizedGrade);
            if (!string.IsNullOrEmpty(normalizedCategory)) parts.Add(normalizedCategory);
            if (!string.IsNullOrEmpty(normalizedUnit))     parts.Add(normalizedUnit);
            if (!string.IsNullOrEmpty(normalizedGameId))   parts.Add(normalizedGameId);

            return parts.Count == 0 ? string.Empty : string.Join("/", parts.ToArray());
        }

        /// <summary>
        /// Builds the default bundle base name: [prefix.]grade.id
        /// </summary>
        internal static string BuildDefaultBundleBaseName(string bundlePrefix, string grade, string gameId)
        {
            string safeGrade = grade.ToLowerInvariant().Replace(" ", string.Empty);
            string safeId    = gameId.ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(bundlePrefix))
            {
                return safeGrade + "." + safeId;
            }

            return bundlePrefix.Trim().ToLowerInvariant() + "." + safeGrade + "." + safeId;
        }

        /// <summary>
        /// Builds a cache directory key from the game ID, bundle base name, and optional version.
        /// </summary>
        internal static string BuildCacheKey(string gameId, string bundleBaseName, string version)
        {
            string normalizedId   = NormalizeCacheKey(gameId);
            string normalizedBase = NormalizeCacheKey(bundleBaseName);

            if (string.IsNullOrWhiteSpace(version))
            {
                return normalizedId + "_" + normalizedBase;
            }

            return normalizedId + "_" + normalizedBase + "_" + NormalizeCacheKey(version);
        }
    }
}
