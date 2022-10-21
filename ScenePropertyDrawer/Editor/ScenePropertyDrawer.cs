using JMor.Utility;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace JMor.EditorScripts.Utility
{
	[CustomPropertyDrawer(typeof(SceneWrapper))]
	public class ScenePropertyDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			// TODO: Clean up rect names, stores, and changes.
			var reloadCheckboxWidth = position.height;
			var reloadCheckboxXPos = position.x + position.width - reloadCheckboxWidth;
			var origX = position.x;
			var newWidth = position.width - reloadCheckboxWidth;

			position.width = newWidth;

			var scenePathProperty = property.FindPropertyRelative("newScenePath");
			var lastValidProp = property.FindPropertyRelative("scenePath");

			#region Validation
			if (lastValidProp.stringValue == "" || lastValidProp.stringValue == null)
			{
				//if (EditorBuildSettings.scenes.Length > 0)
				//	lastValidProp.stringValue = EditorBuildSettings.scenes[0].path;
				//else
				//{
				//	var scenes = AssetDatabase.FindAssets("t:scene");
				//	if (scenes.Length > 0)
				//		lastValidProp.stringValue = AssetDatabase.GUIDToAssetPath(scenes[0]);
				//	else
				//	{
				//		GUI.Label(position, "There must be at least 1 scene (a .unity file).");
				//		return;
				//	}
				//}
				SetFallbackScenePath(lastValidProp, position);
				property.serializedObject.ApplyModifiedProperties();
			}
			if (scenePathProperty.stringValue == "" || scenePathProperty.stringValue == null)
			{
				//scenePathProperty.stringValue = EditorBuildSettings.scenes[0].path;
				scenePathProperty.stringValue = lastValidProp.stringValue;
				property.serializedObject.ApplyModifiedProperties();
			}
			#endregion


			var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePathProperty.stringValue);
			property.serializedObject.Update();

			#region Show the variable name if not in a list
			var fullPosition = position;
			//Debug.Log(property.displayName);
			// Don't show labels in collections (arrays, lists, etc.)
			if (!property.displayName.StartsWith("Element ") && !property.displayName.Contains('.') && !property.displayName.Contains('/'))
			{
				// TODO: Figure out field to label ratio.
				var labelPosition = fullPosition;
				labelPosition.width = fullPosition.width * 2f / 5f;
				GUI.Label(labelPosition, property.displayName);

				position.x += labelPosition.width;
				position.width = fullPosition.width - labelPosition.width;
				newWidth = position.width;
				origX = position.x;
			}
			#endregion

			void DrawAddToBuildButtons()
			{
				if (!IsSceneInBuild(GetPropStrOrDefault(scenePathProperty)))
				{
					position.width /= 2;
					var newIndex = EditorBuildSettings.scenes.Length;
					var currSceneName = AssetDatabase.LoadAssetAtPath<SceneAsset>(GetPropStrOrDefault(scenePathProperty)).name;
					var lastValidSceneName = AssetDatabase.LoadAssetAtPath<SceneAsset>(GetPropStrOrDefault(lastValidProp)).name;
					var yButtonTooltip = $"Add \"{currSceneName}\" to the build at index {newIndex}?";
					var nButtonTooltip = $"Revert to prior valid scene \"{lastValidSceneName}\"?";
					if (GUI.Button(position, new GUIContent("Add To Build", yButtonTooltip)))
					{
						property.FindPropertyRelative("sceneName").stringValue = sceneAsset.name;
						property.serializedObject.ApplyModifiedProperties();
						AddToBuild(scenePathProperty.stringValue);
					}
					position.x += position.width;
					if (GUI.Button(position, new GUIContent("No", nButtonTooltip)))
					{
						scenePathProperty.stringValue = GetPropStrOrDefault(lastValidProp);//lastValidProp.stringValue;
						property.serializedObject.ApplyModifiedProperties();
					}
					position.x -= position.width;
					position.width *= 2;
				}
			}
			// Hacky solution: Drawing the buttons before the ObjectField allows the buttons
			// interaction to take precedence, but draws the object field over them. Drawing the buttons
			// after the ObjectField draws the the buttons over the object field. but then the object
			// field interaction takes precedence. Removing the object field during attempts to draw the buttons
			// won't erase the editor window, but will stop the change check from setting the values. Drawing the
			// buttons twice, first to prioritize their interactions, then to draw them over the object field visually,
			// gives the intended behaviour.
			DrawAddToBuildButtons();
			#region ForceReload Checkbox Setup and Reset.
			EditorGUI.BeginChangeCheck();
			position.width = reloadCheckboxWidth;
			position.x = reloadCheckboxXPos;
			var newForceReload = GUI.Toggle(position, property.FindPropertyRelative("forceReload").boolValue, new GUIContent("", "Reload scene if scene is already loaded?"));
			position.width = newWidth;
			position.x = origX;
			if (EditorGUI.EndChangeCheck())
			{
				property.FindPropertyRelative("forceReload").boolValue = newForceReload;
				property.serializedObject.ApplyModifiedProperties();
			}
			#endregion
			EditorGUI.BeginChangeCheck();
			#region Remove Scene From Build Button Setup and Reset.
			var removeButtonXWidth = position.height;
			var posOrigWidth = position.width;
			var posOrigXPos = position.x;
			position.width = removeButtonXWidth;
			var buildIndex = FindBuildIndexManually(GetPropStrOrDefault(scenePathProperty)/*scenePathProperty.stringValue*/);
			if (GUI.Button(position, new GUIContent("X", $"Remove scene from build; index = {buildIndex}")))
			{
				RemoveFromBuild(buildIndex);
				SetFallbackScenePath(lastValidProp, position);//lastValidProp.stringValue = EditorBuildSettings.scenes[0].path;
			}
			position.width = posOrigWidth - removeButtonXWidth;
			position.x += removeButtonXWidth;
			#endregion
			sceneAsset = EditorGUI.ObjectField(position/*, sceneAsset.name*/, sceneAsset, typeof(SceneAsset), false) as SceneAsset;
			position.width = posOrigWidth;
			position.x = posOrigXPos;
			if (EditorGUI.EndChangeCheck())
			{
				var newPath = AssetDatabase.GetAssetPath(sceneAsset);
				scenePathProperty.stringValue = newPath;
				for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
				{
					if (EditorBuildSettings.scenes[i].path == newPath)
					{
						// Sync check
						if (!IsSceneInBuild(newPath))
							InitializeBuildSceneDictionary();
						//scenePathProperty.stringValue = newPath;
						lastValidProp.stringValue = newPath;
						property.FindPropertyRelative("sceneName").stringValue = sceneAsset.name;
						property.serializedObject.ApplyModifiedProperties();
						break;
					}
				}
			}
			DrawAddToBuildButtons();
		}

		void SetFallbackScenePath(SerializedProperty property, Rect position)
		{
			if (EditorBuildSettings.scenes.Length > 0)
				property.stringValue = EditorBuildSettings.scenes[0].path;
			else
			{
				var scenes = AssetDatabase.FindAssets("t:scene");
				if (scenes.Length > 0)
					property.stringValue = AssetDatabase.GUIDToAssetPath(scenes[0]);
				else
				{
					GUI.Label(position, "There must be at least 1 scene (a .unity file).");
					return;
				}
			}
		}

		string GetPropStrOrDefault(SerializedProperty property)
		{
			// TODO: Account for no scenes in build path (see SetFallbackScenePath)
			return (property.stringValue == "" || property.stringValue == null) ?
				property.stringValue = EditorBuildSettings.scenes[0].path :
				property.stringValue;
		}

		int FindBuildIndexManually(string scenePath)
		{
			for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
				if (EditorBuildSettings.scenes[i].path == scenePath)
					return i;
			return -1;
		}

		#region Build Scene Stuff
		public static Dictionary<string, int> buildScenesAndIndexes;

		void InitializeBuildSceneDictionary()
		{
			buildScenesAndIndexes = new Dictionary<string, int>(EditorBuildSettings.scenes.Length + 1);
			for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
				buildScenesAndIndexes[EditorBuildSettings.scenes[i].path] = i;
		}
		int FindBuildIndex(string scenePath)
		{
			if (buildScenesAndIndexes == null)
				InitializeBuildSceneDictionary();
			if (buildScenesAndIndexes.ContainsKey(scenePath))
			{
				if (buildScenesAndIndexes[scenePath] < 0)
					return buildScenesAndIndexes[scenePath];
				// Check if in sync
				else if (buildScenesAndIndexes[scenePath] < EditorBuildSettings.scenes.Length && EditorBuildSettings.scenes[buildScenesAndIndexes[scenePath]].path == scenePath)
					return buildScenesAndIndexes[scenePath];
				// Out of sync, redo
				else
				{
					InitializeBuildSceneDictionary();
					return FindBuildIndexManually(scenePath);
				}
			}
			// If it doesn't contain the key, add it.
			else
			{
				return buildScenesAndIndexes[scenePath] = FindBuildIndexManually(scenePath);
			}
		}
		void RemoveFromBuild(int index)
		{
			if (index < 0 || index >= EditorBuildSettings.scenes.Length)
			{
				Debug.LogWarning($"Index {index} out of bounds; no scenes will be removed from the build.");
				return;
			}
			var newBuildScenes = new EditorBuildSettingsScene[EditorBuildSettings.scenes.Length - 1];
			for (int i = 0; i < newBuildScenes.Length; i++)
			{
				if (i >= index)
					buildScenesAndIndexes[EditorBuildSettings.scenes[i + 1].path] = i + 1;
				newBuildScenes[i] = (i >= index) ?
					EditorBuildSettings.scenes[i + 1] :
					EditorBuildSettings.scenes[i];
			}
			if (buildScenesAndIndexes == null)
				InitializeBuildSceneDictionary();
			buildScenesAndIndexes[EditorBuildSettings.scenes[index].path] = -1;
			EditorBuildSettings.scenes = newBuildScenes;
		}
		void AddToBuild(string scenePath)
		{
			var newBuildScenes = new EditorBuildSettingsScene[EditorBuildSettings.scenes.Length + 1];
			EditorBuildSettings.scenes.CopyTo(newBuildScenes, 0);
			newBuildScenes[^1] = new EditorBuildSettingsScene(scenePath, true);
			if (buildScenesAndIndexes == null)
				InitializeBuildSceneDictionary();
			buildScenesAndIndexes[scenePath] = newBuildScenes.Length - 1;
			EditorBuildSettings.scenes = newBuildScenes;
		}

		bool IsSceneInBuild(string scenePath)
		{
			if (buildScenesAndIndexes == null)
				InitializeBuildSceneDictionary();
			if (buildScenesAndIndexes.ContainsKey(scenePath) && buildScenesAndIndexes[scenePath] >= 0)
			{
				// Check if in sync
				if (buildScenesAndIndexes[scenePath] < EditorBuildSettings.scenes.Length && EditorBuildSettings.scenes[buildScenesAndIndexes[scenePath]].path == scenePath)
					return true;
				// If the index is too large or points to a different scene, then the dict is out of sync.
				else
				{
					InitializeBuildSceneDictionary();
					return FindBuildIndexManually(scenePath) >= 0;
				}
			}
			else
				return FindBuildIndex(scenePath) >= 0;
		}
		#endregion
	}
}