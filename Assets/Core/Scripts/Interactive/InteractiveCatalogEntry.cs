using System.Collections.Generic;
using UnityEngine;

namespace Tek.Core
{
    [System.Serializable]
    public class InteractiveCatalogEntry
    {
        public string id;
        public string title;
        public string image;
        public string home;
        public string category;
        public string unit;
        public string folder;
        public string grade;
        public string bundleBaseName;
        public string bundleVersion;
        public bool enabled = true;

        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrEmpty(title))
                {
                    return title;
                }

                return id;
            }
        }
    }

    [System.Serializable]
    public class InteractiveCatalogDocument
    {
        public List<InteractiveCatalogEntry> interactives = new List<InteractiveCatalogEntry>();
    }
}
