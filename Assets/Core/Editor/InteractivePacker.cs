using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using EnglishTek.Core;

public class InteractivePacker : EditorWindow
{
    private InteractiveManifest manifest;
    private string buildPath = "ServerData";
    private string scriptFolderPath = "Assets";

    [MenuItem("Tools/Interactive Game Packer")]
    public static void ShowWindow()
    {
        GetWindow<InteractivePacker>("Game Packer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Setup Interactive Bundle", EditorStyles.boldLabel);

        manifest = (InteractiveManifest)EditorGUILayout.ObjectField("Manifest File", manifest, typeof(InteractiveManifest), false);

        if (manifest == null)
        {
            EditorGUILayout.HelpBox("Please create and select an InteractiveManifest ScriptableObject.", MessageType.Info);
            return;
        }

        // ── Container Import ─────────────────────────────────────────────────
        EditorGUILayout.Space();
        GUILayout.Label("Container Setup", EditorStyles.boldLabel);

        if (manifest.gameId <= 0)
        {
            EditorGUILayout.HelpBox("Set the 'Game Id' field on the manifest before importing.", MessageType.Warning);
        }
        else
        {
            EditorGUILayout.LabelField("XML destination", $"Assets/Resources/XML/{manifest.gameId}/");
            EditorGUILayout.HelpBox(
                "Expected keys: Instruction, Itembank_Practice, Itembank_Workout, Itembank_Quiz, Feedback\n" +
                "Keys must match what the GameManager passes to Resources.Load.",
                MessageType.Info);
        }

        EditorGUI.BeginDisabledGroup(manifest.gameId <= 0);
        if (GUILayout.Button("Import XML to Container (Resources)"))
        {
            ImportXmlToContainer();
        }
        EditorGUI.EndDisabledGroup();

        // ── Asset Bundle ─────────────────────────────────────────────────────
        EditorGUILayout.Space();
        GUILayout.Label("Asset Bundling", EditorStyles.boldLabel);

        if (GUILayout.Button("1. Tag Assets for Bundle"))
        {
            TagAssets();
        }

        if (GUILayout.Button("2. Build Asset Bundle"))
        {
            BuildBundles();
        }

        // ── Namespace Sync ───────────────────────────────────────────────────
        EditorGUILayout.Space();
        GUILayout.Label("Namespace Sync", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        scriptFolderPath = EditorGUILayout.TextField("Script Folder", scriptFolderPath);
        if (GUILayout.Button("Select Folder", GUILayout.Width(120)))
        {
            SelectScriptFolder();
        }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("3. Apply Namespace to C# Files"))
        {
            ApplyNamespaceToScripts();
        }
    }

    // ── Container Import ──────────────────────────────────────────────────────

    /// <summary>
    /// Copies every XML in the manifest's xmlConfigs into
    /// Assets/Resources/XML/{gameId}/{key}.xml
    /// so Resources.Load("XML/{gameId}/{key}") works in all GameManagers.
    /// </summary>
    private void ImportXmlToContainer()
    {
        if (manifest.xmlConfigs == null || manifest.xmlConfigs.Count == 0)
        {
            EditorUtility.DisplayDialog("No XML Files", "The manifest has no XML configs to import.", "OK");
            return;
        }

        string destFolder = Path.Combine("Assets", "Resources", "XML", manifest.gameId.ToString());
        Directory.CreateDirectory(destFolder);

        int imported = 0;
        int skipped  = 0;

        foreach (NamedXML entry in manifest.xmlConfigs)
        {
            if (entry.xmlFile == null)
            {
                Debug.LogWarning($"[InteractivePacker] xmlConfig key='{entry.key}' has no file assigned — skipped.");
                skipped++;
                continue;
            }

            string sourcePath = AssetDatabase.GetAssetPath(entry.xmlFile);
            string destPath   = Path.Combine(destFolder, entry.key + ".xml").Replace("\\", "/");

            if (File.Exists(destPath))
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "File Already Exists",
                    $"'{destPath}' already exists.\nOverwrite?",
                    "Overwrite", "Skip");

                if (!overwrite) { skipped++; continue; }
            }

            AssetDatabase.CopyAsset(sourcePath, destPath);
            imported++;
        }

        AssetDatabase.Refresh();
        Debug.Log($"[InteractivePacker] Import complete — {imported} imported, {skipped} skipped → {destFolder}");
        EditorUtility.DisplayDialog("Import Complete",
            $"{imported} XML file(s) copied to Resources/XML/{manifest.gameId}/\n{skipped} skipped.",
            "OK");
    }

    // ── Asset Bundle ──────────────────────────────────────────────────────────

    private void TagAssets()
    {
        if (string.IsNullOrEmpty(manifest.bundleName)) return;

        string baseName        = manifest.bundleName.ToLower();
        string sceneBundleName = baseName + ".scenes";
        string assetBundleName = baseName + ".assets";

        // XMLs are NOT bundled — they are copied to Resources/XML/{gameId}/ in the container
        SetAssetBundleName(manifest, assetBundleName);
        foreach (var prefab in manifest.prefabsToInclude)
            SetAssetBundleName(prefab, assetBundleName);
        foreach (var sceneObj in manifest.allScenes)
            SetAssetBundleName(sceneObj, sceneBundleName);

        AssetDatabase.RemoveUnusedAssetBundleNames();
        Debug.Log($"[InteractivePacker] Tagged: {assetBundleName} AND {sceneBundleName}");
    }

    private void SetAssetBundleName(Object asset, string name)
    {
        if (asset == null) return;
        string path = AssetDatabase.GetAssetPath(asset);
        AssetImporter.GetAtPath(path).SetAssetBundleNameAndVariant(name, "");
    }

    private void BuildBundles()
    {
        if (!Directory.Exists(buildPath)) Directory.CreateDirectory(buildPath);
        BuildPipeline.BuildAssetBundles(buildPath, BuildAssetBundleOptions.None, BuildTarget.Android);
        Debug.Log("[InteractivePacker] Build complete → " + buildPath);
    }

    // ── Namespace Sync ────────────────────────────────────────────────────────

    private void SelectScriptFolder()
    {
        string selected = EditorUtility.OpenFolderPanel("Select Script Folder", Application.dataPath, "");
        if (!string.IsNullOrEmpty(selected))
            scriptFolderPath = selected;
    }

    private void ApplyNamespaceToScripts()
    {
        if (string.IsNullOrWhiteSpace(manifest.bundleName))
        {
            Debug.LogWarning("[InteractivePacker] Manifest bundleName is empty.");
            return;
        }

        if (string.IsNullOrWhiteSpace(scriptFolderPath) || !Directory.Exists(scriptFolderPath))
        {
            Debug.LogWarning("[InteractivePacker] Please select a valid script folder.");
            return;
        }

        string namespaceName = ConvertBundleNameToNamespace(manifest.bundleName.ToLower());
        string[] scriptFiles = Directory.GetFiles(scriptFolderPath, "*.cs", SearchOption.AllDirectories);
        int updated = 0;

        foreach (string file in scriptFiles)
            if (UpdateScriptNamespace(file, namespaceName)) updated++;

        AssetDatabase.Refresh();
        Debug.Log($"[InteractivePacker] Namespace sync complete — {updated}/{scriptFiles.Length} files → '{namespaceName}'.");
    }

    private string ConvertBundleNameToNamespace(string bundleName)
    {
        string cleaned = Regex.Replace(bundleName ?? string.Empty, @"[^A-Za-z0-9_\.]+", "_");
        string[] parts = cleaned.Split('.');
        for (int i = 0; i < parts.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(parts[i])) { parts[i] = "_"; continue; }
            if (!char.IsLetter(parts[i][0]) && parts[i][0] != '_')
                parts[i] = "_" + parts[i];
        }
        return string.Join(".", parts);
    }

    private bool UpdateScriptNamespace(string filePath, string namespaceName)
    {
        string content = File.ReadAllText(filePath);
        Regex nsRegex  = new Regex(@"\bnamespace\s+([A-Za-z_][A-Za-z0-9_\.]*)");
        string updated;

        if (nsRegex.IsMatch(content))
        {
            updated = nsRegex.Replace(content, "namespace " + namespaceName, 1);
        }
        else
        {
            if (content.Contains("[assembly:"))
            {
                Debug.LogWarning("[InteractivePacker] Skipping assembly-level script: " + filePath);
                return false;
            }

            // Extract leading using directives so they appear outside the namespace block.
            Regex usingRegex = new Regex(@"^(\s*using\s+[^;]+;\s*\n)+", RegexOptions.Multiline);
            Match usingMatch = usingRegex.Match(content);
            string usingBlock = usingMatch.Success ? usingMatch.Value : string.Empty;
            string remainder  = content.Substring(usingBlock.Length);
            updated = usingBlock + "namespace " + namespaceName + "\n{\n" + IndentContent(remainder) + "\n}";
        }

        if (updated == content) return false;
        File.WriteAllText(filePath, updated);
        return true;
    }

    private string IndentContent(string text)
    {
        string[] lines = text.Replace("\r\n", "\n").Split('\n');
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Length > 0) sb.Append("    ");
            sb.Append(lines[i]);
            if (i < lines.Length - 1) sb.Append("\n");
        }
        return sb.ToString();
    }
}
