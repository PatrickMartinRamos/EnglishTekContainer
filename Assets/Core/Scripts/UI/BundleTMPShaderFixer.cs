using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tek.Core
{
    /// <summary>
    /// Persistent singleton that repairs TMP shaders after scene transitions.
    ///
    /// This is mainly for TMP fonts/materials coming from AssetBundles where shader
    /// references can be lost or resolve to the wrong shader at runtime.
    /// </summary>
    [DisallowMultipleComponent]
    public class BundleTMPShaderFixer : MonoBehaviour
    {
        private const string TmpDistanceFieldShader = "TextMeshPro/Distance Field";
        private const string TmpMobileDistanceFieldShader = "TextMeshPro/Mobile/Distance Field";

        private static BundleTMPShaderFixer instance;

        private Shader preferredShader;

        public static void EnsureExists()
        {
            if (instance != null)
            {
                return;
            }

            GameObject go = new GameObject("BundleTMPShaderFixer");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<BundleTMPShaderFixer>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            ResolveShader();
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            StartCoroutine(FixAfterFrame(scene));
        }

        private IEnumerator FixAfterFrame(Scene scene)
        {
            // Wait one frame so objects spawned in Awake/Start are included.
            yield return null;
            FixScene(scene);
        }

        public static void FixSceneNow(Scene scene)
        {
            if (instance != null)
            {
                instance.FixScene(scene);
            }
        }

        private void FixScene(Scene scene)
        {
            if (!scene.isLoaded)
            {
                return;
            }

            if (!ResolveShader())
            {
                Debug.LogWarning("[BundleTMPShaderFixer] TMP Distance Field shader not found in build.");
                return;
            }

            int fixedCount = 0;
            GameObject[] roots = scene.GetRootGameObjects();

            foreach (GameObject root in roots)
            {
                TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
                foreach (TMP_Text text in texts)
                {
                    if (text == null)
                    {
                        continue;
                    }

                    TMP_FontAsset font = text.font;
                    if (font != null)
                    {
                        fixedCount += FixFontAsset(font);
                    }

                    Material instanceMaterial = text.fontMaterial;
                    if (instanceMaterial != null && instanceMaterial.shader != preferredShader)
                    {
                        instanceMaterial.shader = preferredShader;
                        fixedCount++;
                    }

                    text.SetMaterialDirty();
                }
            }

            if (fixedCount > 0)
            {
                Debug.Log("[BundleTMPShaderFixer] Reassigned TMP shader/material on " + fixedCount + " item(s) in scene: " + scene.name);
            }
        }

        private int FixFontAsset(TMP_FontAsset font)
        {
            int count = 0;

            if (font.material != null && font.material.shader != preferredShader)
            {
                font.material.shader = preferredShader;
                count++;
            }

            if (font.material != null && font.atlasTexture != null && font.material.mainTexture == null)
            {
                font.material.mainTexture = font.atlasTexture;
                count++;
            }

            return count;
        }

        private bool ResolveShader()
        {
            if (preferredShader != null)
            {
                return true;
            }

            preferredShader = Shader.Find(TmpDistanceFieldShader);
            if (preferredShader == null)
            {
                preferredShader = Shader.Find(TmpMobileDistanceFieldShader);
            }

            return preferredShader != null;
        }
    }
}
