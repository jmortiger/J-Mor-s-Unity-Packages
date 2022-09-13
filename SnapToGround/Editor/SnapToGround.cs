using UnityEditor;
using UnityEngine;

namespace JMor.EditorScripts
{
	public class SnapToGround : ScriptableObject
	{
		// TODO: Account for offsets, colliders, etc.
		// TODO: Add undo for prefabs https://docs.unity3d.com/ScriptReference/Undo.RecordObject.html , https://docs.unity3d.com/ScriptReference/PrefabUtility.RecordPrefabInstancePropertyModifications.html
		[MenuItem("Tools/Snap selected to ground")]
		static void SnapSelectedToGround()
		{
			var qsic2D = Physics2D.queriesStartInColliders;
			Physics2D.queriesStartInColliders = false;
			//var qsic3D = Physics.queriesHitBackfaces;
			//Physics2D.queriesStartInColliders = false;
			Undo.RecordObjects(Selection.gameObjects, "Snapped Objects Down");
			foreach (var go in Selection.gameObjects)
			{
				Debug.Log($"Attempting to snap {go.name} from {go.transform.position}");
				if (SceneView.lastActiveSceneView is not null && SceneView.lastActiveSceneView.in2DMode)
				{
					Debug.Log($"2D Snapping");
					var qsic = Physics2D.queriesStartInColliders;
					Physics2D.queriesStartInColliders = false;
					RaycastHit2D hit = Physics2D.Raycast(go.transform.position, Vector3.down);
					if (go.activeInHierarchy && default(RaycastHit2D) != hit)
					{
						Debug.Log($"Snapping {go.name} from {go.transform.position} to {hit.point}");
						go.transform.position = new(hit.point.x, hit.point.y, go.transform.position.z);
					}
				}
				else
				{
					Debug.Log($"3D Snapping");
					if (go.activeInHierarchy && Physics.Raycast(new Ray(go.transform.position, Vector3.down), out RaycastHit hit))
					{
						Debug.Log($"Snapping {go.name} from {go.transform.position} to {hit.point}");
						go.transform.position = hit.point;
					}

				}
			}
			Physics2D.queriesStartInColliders = qsic2D;
		}
		
		// Disable the menu if there is nothing selected
		[MenuItem("Tools/Snap selected to ground", true)]
		static bool ValidateSelection() => Selection.activeGameObject != null;
	}
}