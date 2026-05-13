using UnityEngine;

namespace ScienceTek.Grade3.U2L1
{
    public class TagRestorer : MonoBehaviour
    {
        public string originalTag;

        void Awake()
        {
            if (!string.IsNullOrEmpty(originalTag))
            {
                try {
                    this.gameObject.tag = originalTag;
                }
                catch {
                    Debug.LogError($"The tag '{originalTag}' does not exist in Project Settings!");
                }
            }
        }
    }
}