using System;

namespace Tek.Core
{
    internal static class InteractivePathResolver
    {
        internal static string GetTekPathName(CurrentTek tek)
        {
            switch (tek)
            {
                case CurrentTek.englishtek:
                    return "EnglishTek";
                case CurrentTek.sciencetek:
                    return "ScienceTek";
                case CurrentTek.filipinotek:
                    return "FilipinoTek";
                case CurrentTek.mathtek:
                    return "MathTek";
                case CurrentTek.aptek:
                    return "APTek";
                default:
                    return tek.ToString();
            }
        }

        internal static string GetGradePathName(GradeLevel grade)
        {
            switch (grade)
            {
                case GradeLevel.Grade1:
                    return "Grade 1";
                case GradeLevel.Grade2:
                    return "Grade 2";
                case GradeLevel.Grade3:
                    return "Grade 3";
                case GradeLevel.Grade4:
                    return "Grade 4";
                case GradeLevel.Grade5:
                    return "Grade 5";
                case GradeLevel.Grade6:
                    return "Grade 6";
                case GradeLevel.Grade7:
                    return "Grade 7";
                case GradeLevel.Grade8:
                    return "Grade 8";
                case GradeLevel.Grade9:
                    return "Grade 9";
                case GradeLevel.Grade10:
                    return "Grade 10";
                default:
                    return grade.ToString();
            }
        }

        internal static string BuildCatalogUrl(string serverRoot, bool useGoogleSheetCatalogs, string webAppUrl, CurrentTek currentTek, GradeLevel grade, string catalogFileName)
        {
            if (useGoogleSheetCatalogs)
            {
                return AppendQueryParameter(webAppUrl, "tek", GetTekPathName(currentTek));
            }

            string tekAndGradePath = BuildTekRelativePath(GetTekPathName(currentTek), GetGradePathName(grade));
            return BuildRootUrl(serverRoot) + BundleUrlHelper.EncodePathSegments(tekAndGradePath) + "/" + catalogFileName;
        }

        internal static string BuildFolderUrl(string serverRoot, CurrentTek currentTek, string folderName)
        {
            string tekAwareFolder = BuildTekRelativePath(GetTekPathName(currentTek), folderName);
            return BuildRootUrl(serverRoot) + BundleUrlHelper.EncodePathSegments(tekAwareFolder) + "/";
        }

        internal static string ResolveCatalogAssetUrl(string serverRoot, CurrentTek currentTek, GradeLevel grade, InteractiveCatalogEntry entry, string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return null;
            }

            string trimmedPath = assetPath.Trim();
            if (Uri.IsWellFormedUriString(trimmedPath, UriKind.Absolute))
            {
                return trimmedPath;
            }

            if (trimmedPath.StartsWith("/"))
            {
                return BuildRootUrl(serverRoot).TrimEnd('/') + trimmedPath;
            }

            if (entry != null && !string.IsNullOrWhiteSpace(entry.folder))
            {
                return CombineUrl(BuildFolderUrl(serverRoot, currentTek, entry.folder), trimmedPath);
            }

            if (entry != null)
            {
                string entryGrade = !string.IsNullOrWhiteSpace(entry.grade) ? entry.grade : GetGradePathName(grade);
                string defaultEntryFolder = BundleUrlHelper.BuildDefaultFolderPath(entryGrade, entry.category, entry.unit, entry.id);
                if (!string.IsNullOrWhiteSpace(defaultEntryFolder))
                {
                    return CombineUrl(BuildFolderUrl(serverRoot, currentTek, defaultEntryFolder), trimmedPath);
                }
            }

            return CombineUrl(BuildRootUrl(serverRoot), trimmedPath);
        }

        private static string AppendQueryParameter(string baseUrl, string key, string value)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                return string.Empty;
            }

            string separator = baseUrl.Contains("?") ? "&" : "?";
            return baseUrl.TrimEnd('/') + separator + Uri.EscapeDataString(key) + "=" + Uri.EscapeDataString(value ?? string.Empty);
        }

        private static string BuildTekRelativePath(string tekName, string relativePath)
        {
            string normalizedTek = BundleUrlHelper.NormalizePathPart(tekName);
            string normalizedRelative = BundleUrlHelper.NormalizePathPart(relativePath);

            if (string.IsNullOrEmpty(normalizedTek))
            {
                return normalizedRelative;
            }

            if (string.IsNullOrEmpty(normalizedRelative))
            {
                return normalizedTek;
            }

            if (string.Equals(normalizedRelative, normalizedTek, StringComparison.OrdinalIgnoreCase)
                || normalizedRelative.StartsWith(normalizedTek + "/", StringComparison.OrdinalIgnoreCase))
            {
                return normalizedRelative;
            }

            return normalizedTek + "/" + normalizedRelative;
        }

        private static string BuildRootUrl(string serverRoot)
        {
            if (string.IsNullOrEmpty(serverRoot))
            {
                return string.Empty;
            }

            return serverRoot.EndsWith("/") ? serverRoot : serverRoot + "/";
        }

        private static string CombineUrl(string baseUrl, string relativePath)
        {
            if (string.IsNullOrEmpty(baseUrl))
            {
                return relativePath;
            }

            return baseUrl.TrimEnd('/') + "/" + relativePath.TrimStart('/');
        }
    }
}
