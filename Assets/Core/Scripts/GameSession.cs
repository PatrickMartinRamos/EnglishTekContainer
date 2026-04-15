using UnityEngine;

namespace EnglishTek.Core
{
    public static class GameSession
    {
        public static InteractiveManifest CurrentManifest;
        public static AssetBundle CurrentAssetBundle;
        public static AssetBundle CurrentSceneBundle;

        public static void CleanUp()
        {
            if (CurrentAssetBundle != null) CurrentAssetBundle.Unload(true);
            if (CurrentSceneBundle != null) CurrentSceneBundle.Unload(true);

            CurrentManifest = null;
            CurrentAssetBundle = null;
            CurrentSceneBundle = null;
            
            Resources.UnloadUnusedAssets();
        }
    }
}
