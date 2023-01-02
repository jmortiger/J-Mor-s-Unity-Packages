using UnityEngine;
using UnityEditor;

namespace JMor.AimAssist.Editor
{
	public class InterpolatorEditorWindow : EditorWindow
	{
		#region Creation
		// [MenuItem("Tools/Test Interpolator", true)]
		// static bool InitializeMyEditorWindow_Validate() => Selection.activeGameObject?.GetComponent<Targeter>() != null;
		[MenuItem("Tools/Test Interpolator")]
		static void InitializeMyEditorWindow()
		{
			InterpolatorEditorWindow window = (InterpolatorEditorWindow)EditorWindow.GetWindow(typeof(InterpolatorEditorWindow));
			window.Show();
		}
		#endregion
		public Vector2[] inputXYs;
		// TODO: Create an editor that shows a view like this video: https://www.youtube.com/watch?v=yGci-Lb87zs
		void OnGUI()
		{
			SerializedObject obj = new SerializedObject(this);

			EditorGUILayout.PropertyField(obj.FindProperty("inputXYs"));
			if (inputXYs == null)
			{
				inputXYs = new Vector2[0];
				EditorGUILayout.HelpBox("I need some inputs.", MessageType.Warning);
			}
			else if (inputXYs.Length == 0)
			{
				EditorGUILayout.HelpBox("I need some inputs.", MessageType.Warning);
			}

			DrawChartControls();


			obj.ApplyModifiedProperties();
		}

		void DrawChartControls()
		{
			
		}
	}
}
