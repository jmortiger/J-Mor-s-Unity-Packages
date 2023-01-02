using UnityEngine;
using UnityEditor;
using JMor.Utility.Inspector;
namespace JMor.Utility.Inspector.EditorScripts
{
	// [CustomPropertyDrawer(typeof(PropertyInspectorAttribute))]
	// public class PropertyInspectorDrawer : PropertyDrawer
	// {
	// 	public PropertyInspectorAttribute MyAttrib => attribute.As<PropertyInspectorAttribute>();
	// 	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	// 	{
	// 		// base.OnGUI(position, property, label);
	// 		if (MyAttrib.onlyGetter) {
	// 			EditorGUILayout.LabelField(MyAttrib.propertyName, "" + MyAttrib.enclosingType.GetProperty(MyAttrib.propertyName).GetValue(property.serializedObject.targetObject));//.GetValue());
	// 			return;
	// 		}

	// 	}
	// }
	[CustomPropertyDrawer(typeof(IHasPropertyInspectors))]
	public class PropertyInspectorDrawer : PropertyDrawer
	{
		public PropertyInspectorAttribute MyAttrib => attribute.As<PropertyInspectorAttribute>();
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			base.OnGUI(position, property, label);
			var obj = property.serializedObject.targetObject.As<IHasPropertyInspectors>();
			for (int i = 0; i < obj.PropertyNames.Count; i++)
			{
				var currVal = JsonUtility.FromJson(obj.PropertyValues[i], obj.PropertyTypes[i]);
				// var f = currVal switch
				// {
				// 	Object myO => EditorGUILayout.ObjectField(obj.PropertyNames[i], myO, obj.PropertyTypes[i], true),
				// 	int myO => EditorGUILayout.IntField(obj.PropertyNames[i], myO),
				// 	_ => ""
				// };
				// switch (currVal) {
				// 	case Object myO:
				// 		EditorGUILayout.ObjectField(obj.PropertyNames[i], myO, obj.PropertyTypes[i], true);
				// 		break;
				// 	case int myO: 
				// 		EditorGUILayout.IntField(obj.PropertyNames[i], myO);
				// 		break;
				// 	default:
				// 		break; 
				// }
				// if (currVal is Object o){
				// 	// var so = new SerializedObject(o);
				// 	EditorGUILayout.ObjectField(o);
				// }
				// else {
				// 	EditorGUILayout.PropertyField();
				// }
				object newVal = currVal switch
				{
					int myO => EditorGUILayout.IntField(obj.PropertyNames[i], myO),
					float myO => EditorGUILayout.FloatField(obj.PropertyNames[i], myO),
					bool myO => EditorGUILayout.Toggle(obj.PropertyNames[i], myO),
					long myO => EditorGUILayout.LongField(obj.PropertyNames[i], myO),
					Vector2 myO => EditorGUILayout.Vector2Field(obj.PropertyNames[i], myO),
					Vector2Int myO => EditorGUILayout.Vector2IntField(obj.PropertyNames[i], myO),
					Vector3 myO => EditorGUILayout.Vector3Field(obj.PropertyNames[i], myO),
					Vector3Int myO => EditorGUILayout.Vector3IntField(obj.PropertyNames[i], myO),
					Bounds myO => EditorGUILayout.BoundsField(obj.PropertyNames[i], myO),
					Color myO => EditorGUILayout.ColorField(obj.PropertyNames[i], myO),
					Rect myO => EditorGUILayout.RectField(obj.PropertyNames[i], myO),
					RectInt myO => EditorGUILayout.RectIntField(obj.PropertyNames[i], myO),
					Vector4 myO => EditorGUILayout.Vector4Field(obj.PropertyNames[i], myO),
					AnimationCurve myO => EditorGUILayout.CurveField(obj.PropertyNames[i], myO),
					Gradient myO => EditorGUILayout.GradientField(obj.PropertyNames[i], myO),
					Object myO => EditorGUILayout.ObjectField(obj.PropertyNames[i], myO, obj.PropertyTypes[i], true),
					_ => throw new System.NotImplementedException()
				};
				if (newVal != currVal) {
					// Undo.RecordObject((Object)obj, "Changed Property Value");
					// obj.
					var trueProp = property.serializedObject.FindProperty("PropertyValues").GetArrayElementAtIndex(i);
					switch (currVal)
					{
						case int myO:
							trueProp.intValue = (int)newVal;
							break;
						case float myO:
							trueProp.floatValue = (float)newVal;
							break;
						case bool myO:
							trueProp.boolValue = (bool)newVal;
							break;
						case long myO:
							trueProp.longValue = (long)newVal;
							break;
						case Vector2 myO:
							trueProp.vector2Value = (Vector2)newVal;
							break;
						case Vector2Int myO:
							trueProp.vector2IntValue = (Vector2Int)newVal;
							break;
						case Vector3 myO:
							trueProp.vector3Value = (Vector3)newVal;
							break;
						case Vector3Int myO:
							trueProp.vector3IntValue = (Vector3Int)newVal;
							break;
						case Bounds myO:
							trueProp.boundsValue = (Bounds)newVal;
							break;
						case Color myO:
							trueProp.colorValue = (Color)newVal;
							break;
						case Rect myO:
							trueProp.rectValue = (Rect)newVal;
							break;
						case RectInt myO:
							trueProp.rectIntValue = (RectInt)newVal;
							break;
						case Vector4 myO:
							trueProp.vector4Value = (Vector4)newVal;
							break;
						case AnimationCurve myO:
							trueProp.animationCurveValue = (AnimationCurve)newVal;
							break;
						// case Gradient myO:
						// 	trueProp.gradientValue = (Gradient)newVal;
						// 	break;
						case Object myO:
							trueProp.objectReferenceValue = (Object)newVal;
							break;
						default:
							throw new System.NotImplementedException();
					};
					property.serializedObject.ApplyModifiedProperties();
				}
			}
		}
	}
}