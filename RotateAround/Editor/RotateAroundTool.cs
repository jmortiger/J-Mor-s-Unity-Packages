using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine.InputSystem;

namespace JMor.EditorScripts.Utility
{
    // TODO: Finish
    [EditorTool("Rotate Around")]
    public class RotateAroundTool : EditorTool
    {
        private Transform centerTransform;
        public override void OnActivated()
        {
            SceneView.lastActiveSceneView.ShowNotification(new GUIContent("Entering Rotate Around Tool"), .1f);
        }

        public override void OnToolGUI(EditorWindow window)
        {
            if (Selection.activeTransform == null || window is not SceneView sceneView)
                return;

            if ((centerTransform == null || Keyboard.current[Key.Q].isPressed))
			{
                centerTransform = Selection.activeTransform;
                Debug.Log($"Transform {centerTransform.name} is the center.");
			}
			else
			{
                EditorGUI.BeginChangeCheck();
                var displacement = centerTransform.position - Selection.activeTransform.position;
                var zProjection = Vector3.ProjectOnPlane(displacement, Vector3.forward);
                var zAngle = Vector3.Angle(Vector3.right, zProjection);
                var yProjection = Vector3.ProjectOnPlane(displacement, Vector3.up);
                var yAngle = Vector3.Angle(Vector3.right, yProjection);
                Quaternion orientation = Quaternion.Euler(0, yAngle, zAngle);
                var newOrientation = Handles.RotationHandle(orientation, Selection.activeTransform.position);
                //var end = Handles.PositionHandle(platform.end, Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    //Selection.activeTransform.RotateAround()
                    //Undo.RecordObject(platform, "Set Platform Destinations");
                    //platform.start = start;
                    //platform.end = end;
                }
            }
		}
	}
}
