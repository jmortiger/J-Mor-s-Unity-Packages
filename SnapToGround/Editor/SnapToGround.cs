using UnityEditor;
using UnityEngine;

namespace JMor.EditorScripts
{
	public class SnapToGround : ScriptableObject
	{
		// TODO: Account for offsets, colliders, etc.
		[MenuItem("Tools/Snap selected to ground")]
		static void SnapSelectedToGround()
		{
			foreach (var go in Selection.gameObjects)
			{
				if (SceneView.lastActiveSceneView is not null && SceneView.lastActiveSceneView.in2DMode)
				{
					RaycastHit2D hit = Physics2D.Raycast(go.transform.position, Vector3.down);
					if (go.activeInHierarchy && default(RaycastHit2D) != hit)
						go.transform.position = hit.point;
				}
				else
					if (go.activeInHierarchy && Physics.Raycast(new Ray(go.transform.position, Vector3.down), out RaycastHit hit))
						go.transform.position = hit.point;
			}
		}
	}
}