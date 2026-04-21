using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace EnglishTek.Core
{
    [DisallowMultipleComponent]
    internal class CatalogThumbnailLoader : MonoBehaviour
    {
        private readonly List<Coroutine> imageLoadRoutines = new List<Coroutine>();
        private readonly List<Texture2D> loadedTextures = new List<Texture2D>();

        internal void TryLoadThumbnail(InteractiveCatalogEntry entry, RawImage target, InteractiveController controller)
        {
            if (entry == null || target == null || controller == null || string.IsNullOrWhiteSpace(entry.image))
            {
                return;
            }

            string imageUrl = controller.ResolveCatalogAssetUrl(entry, entry.image);
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return;
            }

            Coroutine routine = StartCoroutine(LoadThumbnailRoutine(imageUrl, target));
            imageLoadRoutines.Add(routine);
        }

        internal void TryLoadThumbnail(InteractiveCatalogEntry entry, Image target, InteractiveController controller)
        {
            if (entry == null || target == null || controller == null || string.IsNullOrWhiteSpace(entry.image))
            {
                return;
            }

            string imageUrl = controller.ResolveCatalogAssetUrl(entry, entry.image);
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return;
            }

            Coroutine routine = StartCoroutine(LoadThumbnailImageRoutine(imageUrl, target));
            imageLoadRoutines.Add(routine);
        }

        internal void StopAll()
        {
            for (int index = 0; index < imageLoadRoutines.Count; index++)
            {
                if (imageLoadRoutines[index] != null)
                {
                    StopCoroutine(imageLoadRoutines[index]);
                }
            }

            imageLoadRoutines.Clear();
        }

        internal void ClearTextures()
        {
            for (int index = 0; index < loadedTextures.Count; index++)
            {
                if (loadedTextures[index] != null)
                {
                    Destroy(loadedTextures[index]);
                }
            }

            loadedTextures.Clear();
        }

        private IEnumerator LoadThumbnailRoutine(string imageUrl, RawImage target)
        {
            yield return StartCoroutine(FetchTexture(imageUrl, texture =>
            {
                if (target != null)
                {
                    target.texture = texture;
                    target.color = Color.white;
                }
            }));
        }

        private IEnumerator LoadThumbnailImageRoutine(string imageUrl, Image target)
        {
            yield return StartCoroutine(FetchTexture(imageUrl, texture =>
            {
                if (target != null)
                {
                    Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                    target.sprite = sprite;
                    target.color = Color.white;
                }
            }));
        }

        private IEnumerator FetchTexture(string imageUrl, System.Action<Texture2D> onLoaded)
        {
            UnityWebRequest request = UnityWebRequestTexture.GetTexture(imageUrl);
            yield return request.SendWebRequest();

            if (request.isNetworkError || request.isHttpError)
            {
                Debug.LogWarning("Catalog thumbnail download failed: " + request.error + " | URL: " + imageUrl);
                yield break;
            }

            Texture2D texture = DownloadHandlerTexture.GetContent(request);
            if (texture == null) yield break;

            loadedTextures.Add(texture);
            onLoaded(texture);
        }
    }
}
