using System.Xml;
using UnityEngine;

namespace EnglishTek.Core
{
    /// <summary>
    /// Centralized XML loading utility for all interactives.
    /// All XML files live under Resources/XML/{id}/{filename}.
    /// New game managers should call these methods instead of building paths manually.
    /// Existing game managers are NOT required to change.
    /// </summary>
    public static class XmlLoader
    {
        private const string Root = "XML/";

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Loads the itembank XML for the given game ID and difficulty.
        ///
        /// Standard path  : Resources/XML/{id}/Itembank_{difficulty}
        /// Legacy path    : Resources/{id}/Itembanks  (difficulty embedded in node path)
        /// </summary>
        public static XmlDocument LoadItembank(int id, string difficulty)
        {
            // Try difficulty-specific file first, then the shared Itembanks file
            string path = Root + id + "/Itembank_" + difficulty.Replace(" ", "");
            TextAsset asset = Resources.Load<TextAsset>(path)
                           ?? Resources.Load<TextAsset>(Root + id + "/Itembanks");
            return ParseXml(asset, "itembank", id);
        }

        /// <summary>
        /// Loads the feedback XML for the given game ID.
        /// Path: Resources/XML/{id}/Feedback
        /// </summary>
        public static XmlDocument LoadFeedback(int id)
        {
            TextAsset asset = Resources.Load<TextAsset>(Root + id + "/Feedback");
            return ParseXml(asset, "feedback", id);
        }

        /// <summary>
        /// Loads the instruction XML for the given game ID.
        ///
        /// Standard path  : Resources/XML/{id}/Instruction
        /// Legacy path    : Resources/{id}/Instructions_{difficulty}
        /// </summary>
        public static XmlDocument LoadInstruction(int id, string difficulty = "")
        {
            // Try the single Instruction file first (used by most games).
            // Fall back to a per-difficulty file (e.g. Instructions_Level1) if needed.
            TextAsset asset = Resources.Load<TextAsset>(Root + id + "/Instruction");
            if (asset == null && !string.IsNullOrEmpty(difficulty))
                asset = Resources.Load<TextAsset>(Root + id + "/Instructions_" + difficulty.Replace(" ", ""));
            return ParseXml(asset, "instruction", id);
        }

        /// <summary>
        /// Loads the dialogue bank XML for the given game ID and difficulty.
        /// Legacy path: Resources/{id}/Dialougebanks
        /// </summary>
        public static XmlDocument LoadDialoguebank(int id, string difficulty = "")
        {
            TextAsset asset = Resources.Load<TextAsset>(Root + id + "/Dialougebanks");
            if (asset == null && !string.IsNullOrEmpty(difficulty))
                asset = Resources.Load<TextAsset>(Root + id + "/Dialoguebank_" + difficulty.Replace(" ", ""));
            return ParseXml(asset, "dialoguebank", id);
        }

        /// <summary>
        /// General-purpose loader. Loads any XML file by its full Resources-relative path.
        /// </summary>
        public static XmlDocument LoadRaw(string resourcePath)
        {
            return ParseXml(Resources.Load<TextAsset>(resourcePath), resourcePath, -1);
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Derives the game ID from a GameManager's namespace.
        /// Namespace must end with "ID###" (e.g. EnglishTek.Grade1.ID106).
        /// Call this from a new game manager: XmlLoader.IdFromNamespace(typeof(GameManager).Namespace)
        /// </summary>
        public static int IdFromNamespace(string namespaceName)
        {
            string[] parts = namespaceName.Split('.');
            string last = parts[parts.Length - 1];
            string digits = last.Replace("ID", "").Replace("id", "");
            if (int.TryParse(digits, out int id))
                return id;

            Debug.LogError($"[XmlLoader] Cannot parse game ID from namespace '{namespaceName}'.");
            return -1;
        }

        /// <summary>
        /// Returns the feedback node name ("Perfect" / "Average" / "Fail")
        /// based on score and total items — shared logic used by all game managers.
        /// </summary>
        public static string FeedbackNodeName(int score, int totalItems)
        {
            float pct = totalItems > 0 ? (float)score / totalItems * 100f : 0f;
            if (pct >= 100f) return "Perfect";
            if (pct >  70f)  return "Average";
            return "Fail";
        }

        // ── Private ──────────────────────────────────────────────────────────

        private static XmlDocument ParseXml(TextAsset asset, string label, int id)
        {
            if (asset == null)
            {
                Debug.LogError($"[XmlLoader] XML asset not found — label='{label}' id={id}");
                return null;
            }

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(asset.text);
            return doc;
        }
    }
}
