using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tek.Core
{
    /// <summary>
    /// Persistent singleton. Every time a new scene loads it finds every TMP_Text
    /// component in that scene and forces their font material's shader back to
    /// "TextMeshPro/Distance Field". This repairs the broken shader reference that
    /// occurs when TMP fonts are loaded from an AssetBundle.
    /// 
    /// Drop on any GameObject in the container scene — it will survive scene transitions.
    /// </summary>
    [DisallowMultipleComponent]
    public class BundleTMPShaderFixer : MonoBehaviour
    {
        private const string TmpDistanceFieldShader = "TextMeshPro/Distance Field";

        private static BundleTMPShaderFixer instance;

        private Shader distanceFieldShader;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            distanceFieldShader = Shader.Find(TmpDistanceFieldShader);
            if (distanceFieldShader == null)
            {
                Debug.LogWarning("[BundleTMPShaderFixer] Shader not found in build: " + TmpDistanceFieldShader
                    + ". Add it to Graphics Settings > Always Included Shaders.");
            }
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
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
            // Run at end of frame so all Awake/Start calls have had a chance to
            // instantiate dynamic TMP objects (e.g. pooled UI) before we fix them.
            StartCoroutine(FixNextFrame(scene));
        }

        private IEnumerator FixNextFrame(Scene scene)
        {
            yield return null;
            FixScene(scene);
        }

        /// <summary>
        /// Public so InteractiveController can call this immediately after a scene load
        /// without waiting for the next frame if needed.
        /// </summary>
        public static void FixSceneNow(Scene scene)
        {
            if (instance != null)
            {
                instance.FixScene(scene);
            }
        }

        private void FixScene(Scene scene)
        {
            if (!scene.isLoaded) return;

            if (distanceFieldShader == null)
            {
                distanceFieldShader = Shader.Find(TmpDistanceFieldShader);
            }

            if (distanceFieldShader == null)
            {
                Debug.LogWarning("[BundleTMPShaderFixer] Cannot fix TMP shaders — Distance Field shader not found.");
                return;
            }

            int fixed_count = 0;
            GameObject[] roots = scene.GetRootGameObjects();

            foreach (GameObject root in roots)
            {
                TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
                foreach (TMP_Text text in texts)
                {
                    if (text == null) continue;

                    // Fix the font asset's own material (shared across all text using that font).
                    TMP_FontAsset font = text.font;
                    if (font != null)
                    {
                        fixed_count += FixFontAsset(font);
                    }

                    // Also fix the per-instance material in case it was overridden.
                    Material mat = text.fontMaterial;
                    if (mat != null && mat.shader != distanceFieldShader)
                    {
                        mat.shader = distanceFieldShader;
                        fixed_count++;
                    }

                    // Force the renderer to re-apply the material.
                    text.SetMaterialDirty();
                }
            }

            if (fixed_count > 0)
            {
                Debug.Log("[BundleTMPShaderFixer] Fixed " + fixed_count + " TMP material(s) in scene: " + scene.name);
            }
        }

        private int FixFontAsset(TMP_FontAsset font)
        {
            int count = 0;

            if (font.material != null && font.material.shader != distanceFieldShader)
            {
                font.material.shader = distanceFieldShader;
                count++;
            }

            // Re-link atlas texture if it was lost during bundle loading.
            if (font.material != null && font.atlasTexture != null
                && font.material.mainTexture == null)
            {
                font.material.mainTexture = font.atlasTexture;
                count++;
            }

            // Fix every material in the font's material preset table.
            if (font.fontMaterialReferences != null)
            {
                foreach (Material preset in font.fontMaterialReferences)
                {
                    if (preset != null && preset.shader != distanceFieldShader)
                    {
                        preset.shader = distanceFieldShader;
                        count++;
                    }
                }
            }

            return count;
        }
    }
}
