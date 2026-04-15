using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class NamedXML {
    public string key;
    public TextAsset xmlFile;
}

[CreateAssetMenu(fileName = "NewManifest", menuName = "Interactive/Manifest")]
public class InteractiveManifest : ScriptableObject {
    public string bundleName;
    public string firstSceneName;
    public List<Object> allScenes;
    public List<NamedXML> xmlConfigs;
    public GameObject[] prefabsToInclude;

    public string GetXMLText(string key) {
        var config = xmlConfigs.Find(x => x.key == key);
        return config != null ? config.xmlFile.text : null;
    }
}