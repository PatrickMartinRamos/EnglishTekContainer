using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tek.Core
{
	public sealed class UpdatePackageName : IPreprocessBuildWithReport, IPostprocessBuildWithReport
	{
		private const string PackagePrefix = "com.TechfactorsInc.";
		private const string AndroidIdentifierSessionKey = "Tek.Core.UpdatePackageName.AndroidIdentifier";
		private const string IosIdentifierSessionKey = "Tek.Core.UpdatePackageName.IosIdentifier";
		private const string ProductNameSessionKey = "Tek.Core.UpdatePackageName.ProductName";
		private const string SessionKeySavedSuffix = ".Saved";

		public int callbackOrder => 0;

		public void OnPreprocessBuild(BuildReport report)
		{
			if (!TryGetCurrentTek(out CurrentTek currentTek))
			{
				Debug.LogWarning("[UpdatePackageName] No InteractiveController with a serialized TEK was found. Package name was not changed.");
				return;
			}

			BuildTargetGroup targetGroup = BuildPipeline.GetBuildTargetGroup(report.summary.platform);
			if (!TryGetIdentifierSessionKey(targetGroup, out string sessionKey))
			{
				return;
			}

			SaveCurrentIdentifierIfNeeded(targetGroup, sessionKey);
			SaveCurrentProductNameIfNeeded();

			string packageName = BuildPackageName(currentTek);
			PlayerSettings.SetApplicationIdentifier(targetGroup, packageName);
			PlayerSettings.productName = BuildProductName(currentTek);
			Debug.Log("[UpdatePackageName] Set package name to " + packageName + " for " + targetGroup + ".");
		}

		public void OnPostprocessBuild(BuildReport report)
		{
			BuildTargetGroup targetGroup = BuildPipeline.GetBuildTargetGroup(report.summary.platform);
			if (!TryGetIdentifierSessionKey(targetGroup, out string sessionKey))
			{
				return;
			}

			RestoreOriginalIdentifier(targetGroup, sessionKey);
			RestoreOriginalProductName();
		}

		private static string BuildPackageName(CurrentTek currentTek)
		{
			return PackagePrefix + currentTek;
		}

		private static string BuildProductName(CurrentTek currentTek)
		{
			return InteractivePathResolver.GetTekPathName(currentTek);   
        }

		private static bool TryGetIdentifierSessionKey(BuildTargetGroup targetGroup, out string sessionKey)
		{
			sessionKey = null;

			switch (targetGroup)
			{
				case BuildTargetGroup.Android:
					sessionKey = AndroidIdentifierSessionKey;
					return true;
				case BuildTargetGroup.iOS:
					sessionKey = IosIdentifierSessionKey;
					return true;
				default:
					return false;
			}
		}

		private static void SaveCurrentIdentifierIfNeeded(BuildTargetGroup targetGroup, string sessionKey)
		{
			string savedKey = sessionKey + SessionKeySavedSuffix;
			if (SessionState.GetBool(savedKey, false))
			{
				return;
			}

			SessionState.SetString(sessionKey, PlayerSettings.GetApplicationIdentifier(targetGroup));
			SessionState.SetBool(savedKey, true);
		}

		private static void SaveCurrentProductNameIfNeeded()
		{
			string savedKey = ProductNameSessionKey + SessionKeySavedSuffix;
			if (SessionState.GetBool(savedKey, false))
			{
				return;
			}

			SessionState.SetString(ProductNameSessionKey, PlayerSettings.productName);
			SessionState.SetBool(savedKey, true);
		}

		private static void RestoreOriginalIdentifier(BuildTargetGroup targetGroup, string sessionKey)
		{
			string savedKey = sessionKey + SessionKeySavedSuffix;
			if (!SessionState.GetBool(savedKey, false))
			{
				return;
			}

			string originalIdentifier = SessionState.GetString(sessionKey, string.Empty);
			if (!string.IsNullOrWhiteSpace(originalIdentifier))
			{
				PlayerSettings.SetApplicationIdentifier(targetGroup, originalIdentifier);
			}

			SessionState.EraseString(sessionKey);
			SessionState.EraseBool(savedKey);
			Debug.Log("[UpdatePackageName] Restored original package name for " + targetGroup + ".");
		}

		private static void RestoreOriginalProductName()
		{
			string savedKey = ProductNameSessionKey + SessionKeySavedSuffix;
			if (!SessionState.GetBool(savedKey, false))
			{
				return;
			}

			string originalProductName = SessionState.GetString(ProductNameSessionKey, string.Empty);
			if (!string.IsNullOrWhiteSpace(originalProductName))
			{
				PlayerSettings.productName = originalProductName;
			}

			SessionState.EraseString(ProductNameSessionKey);
			SessionState.EraseBool(savedKey);
			Debug.Log("[UpdatePackageName] Restored original app name.");
		}

		private static bool TryGetCurrentTek(out CurrentTek currentTek)
		{
			currentTek = default;

			Scene activeScene = EditorSceneManager.GetActiveScene();
			if (TryGetCurrentTekFromScene(activeScene, out currentTek))
			{
				return true;
			}

			foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
			{
				if (!buildScene.enabled || string.IsNullOrWhiteSpace(buildScene.path))
				{
					continue;
				}

				Scene scene = EditorSceneManager.GetSceneByPath(buildScene.path);
				bool openedScene = false;

				if (!scene.isLoaded)
				{
					scene = EditorSceneManager.OpenScene(buildScene.path, OpenSceneMode.Additive);
					openedScene = true;
				}

				try
				{
					if (TryGetCurrentTekFromScene(scene, out currentTek))
					{
						return true;
					}
				}
				finally
				{
					if (openedScene && scene.isLoaded)
					{
						EditorSceneManager.CloseScene(scene, true);
					}
				}
			}

			return false;
		}

		private static bool TryGetCurrentTekFromScene(Scene scene, out CurrentTek currentTek)
		{
			currentTek = default;

			if (!scene.isLoaded)
			{
				return false;
			}

			GameObject[] rootObjects = scene.GetRootGameObjects();
			foreach (GameObject rootObject in rootObjects)
			{
				InteractiveController[] controllers = rootObject.GetComponentsInChildren<InteractiveController>(true);
				foreach (InteractiveController controller in controllers)
				{
					if (TryReadCurrentTek(controller, out currentTek))
					{
						return true;
					}
				}
			}

			return false;
		}

		private static bool TryReadCurrentTek(InteractiveController controller, out CurrentTek currentTek)
		{
			currentTek = default;

			if (controller == null)
			{
				return false;
			}

			SerializedObject serializedObject = new SerializedObject(controller);
			SerializedProperty currentTekProperty = serializedObject.FindProperty("currentTek");
			if (currentTekProperty == null || currentTekProperty.propertyType != SerializedPropertyType.Enum)
			{
				return false;
			}

			currentTek = (CurrentTek)currentTekProperty.enumValueIndex;
			return true;
		}
	}
}
