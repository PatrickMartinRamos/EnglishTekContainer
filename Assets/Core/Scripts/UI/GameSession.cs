using UnityEngine;

namespace Tek.Core
{
    public static class GameSession
    {
        public static InteractiveManifest CurrentManifest;
        public static AssetBundle CurrentAssetBundle;
        public static AssetBundle CurrentSceneBundle;
        public static string ContainerSceneName;

        public static void CleanUp()
        {
            if (CurrentAssetBundle != null) CurrentAssetBundle.Unload(true);
            if (CurrentSceneBundle != null) CurrentSceneBundle.Unload(true);

            CurrentManifest = null;
            CurrentAssetBundle = null;
            CurrentSceneBundle = null;
            ContainerSceneName = null;

            Resources.UnloadUnusedAssets();
        }
    }
}
