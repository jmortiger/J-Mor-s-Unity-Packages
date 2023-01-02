using UnityEngine;
using UnityEditor;

namespace JMor.AimAssist.Editor
{
    public class CustomAimAssistEditor : EditorWindow
    {
		#region Creation
		[MenuItem("Tools/Edit Selected Aim Assist Curve", true)]
		static bool InitializeMyEditorWindow_Validate() => Selection.activeGameObject?.GetComponent<Targeter>() != null;
		[MenuItem("Tools/Edit Selected Aim Assist Curve")]
		static void InitializeMyEditorWindow()
		{
			CustomAimAssistEditor window = (CustomAimAssistEditor)EditorWindow.GetWindow(typeof(CustomAimAssistEditor));
	    	window.Show();
		}
		#endregion
		// TODO: Create an editor that shows a view like this video: https://www.youtube.com/watch?v=yGci-Lb87zs
		void OnGUI()
		{

		}
    }
}
