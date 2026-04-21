using UnityEngine;
using System.Collections.Generic;

namespace EnglishTek.Core
{
    [System.Serializable]
    public class NamedXML
    {
        public string key;
        public TextAsset xmlFile;
    }

    [CreateAssetMenu(fileName = "NewManifest", menuName = "Interactive/Manifest")]
    public class InteractiveManifest : ScriptableObject
    {
        public string bundleName;
        // Numeric ID of the interactive (e.g. 106). Used to place XMLs under Resources/XML/{gameId}/
        public int gameId;
        public string firstSceneName;
        public List<Object> allScenes;
        // Keys must match the filename the GameManager expects: "Instruction", "Itembank_Practice", "Itembank_Workout", "Itembank_Quiz", "Feedback"
        public List<NamedXML> xmlConfigs;
        public GameObject[] prefabsToInclude;

        public string GetXMLText(string key)
        {
            if (xmlConfigs == null) return null;
            NamedXML config = xmlConfigs.Find(x => x.key == key);
            return (config != null && config.xmlFile != null) ? config.xmlFile.text : null;
        }
    }
}