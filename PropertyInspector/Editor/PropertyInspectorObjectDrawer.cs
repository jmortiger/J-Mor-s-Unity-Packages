using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace JMor.Utility.Inspector.EditorScripts
{
	[CustomPropertyDrawer(typeof(PropertyInspectorObject))]
	public class PropertyInspectorObjectDrawer : PropertyDrawer
	{
		private SerializedObject Get_MyContainerSerialized(SerializedProperty property) => property.serializedObject;
		private object Get_MyContainer(SerializedProperty property) => property.serializedObject.targetObject;
		private PropertyInspectorObject Get_PIO(SerializedProperty property) => property.managedReferenceValue.As<PropertyInspectorObject>();
		public bool noDebug = true;
		public bool isDebug = true;
		public bool IsDebug => !noDebug && isDebug;
		public bool isSuperDebug = false;
		public bool IsSuperDebug => IsDebug && isSuperDebug;
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => EditorGUIUtility.singleLineHeight *
				(Get_PIO(property).PropertyNames.Count * (IsDebug ? 2f : 1f) + (!noDebug ? (isDebug ? 2f : 1f) : 0f)) *
				(EditorGUIUtility.wideMode ? 1f : 2f);
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			#region Setup objects
			if (IsSuperDebug)
			{
				Debug.Log(property.propertyType);
				Debug.Log(property.type);
			}
			var obj = Get_PIO(property);
			var containerObj = Get_MyContainer(property);
			if (obj.container == null)
			{
				Debug.LogWarning("PropertyInspectorObject not properly initialized, hooking up through SerializedProperty.serializedObject.");
				obj.container = Get_MyContainer(property);
			}
			else if (obj.container != containerObj)
			{
				Debug.LogError("PropertyInspectorObject.container != SerializedProperty.container; Desynced. Major problem. Syncing to SerializedProperty.container. Requires investigation.");
				obj.container = Get_MyContainer(property);
			}
			var containerSerialized = Get_MyContainerSerialized(property);
			#endregion

			var currPos = position;
			currPos.height = EditorGUIUtility.singleLineHeight;

			#region Debug buttons
			var new_isDebug = isDebug;
			var new_isSuperDebug = isSuperDebug;
			if (!noDebug)
			{
				new_isDebug = EditorGUI.Toggle(currPos, "Debug?", isDebug);
				currPos.y += currPos.height;
				if (isDebug)
				{
					new_isSuperDebug = EditorGUI.Toggle(currPos, "Super Debug?", isSuperDebug);
					currPos.y += currPos.height;
					if (isSuperDebug)
					{
						Debug.Log($"Property Lengths");
						Debug.Log($"\tNames: {obj.PropertyNames.Count}");
						Debug.Log($"\tTypes: {obj.PropertyTypes.Count}");
						Debug.Log($"\tValues: {obj.PropertyValues.Count}");
					}
				}
			}
			#endregion
			
			for (int i = 0; i < obj.PropertyNames.Count; i++)
			{
				var currVal = PropertyInspectorObject.Parse(obj.PropertyValues[i], obj.PropertyTypes[i]);//JsonUtility.FromJson(obj.PropertyValues[i], obj.PropertyTypes[i]);
				EditorGUI.BeginChangeCheck();
				string debug = obj.PropertyValues[i];
				if (IsDebug)
				{
					debug = EditorGUI.TextField(currPos, $"{obj.PropertyNames[i]} Serialized", obj.PropertyValues[i]);
					currPos.y += currPos.height;
				}
				object newVal = currVal switch
				{
					int myV => EditorGUI.IntField(currPos, obj.PropertyNames[i], myV),
					float myV => EditorGUI.FloatField(currPos, obj.PropertyNames[i], myV),
					bool myV => EditorGUI.Toggle(currPos, obj.PropertyNames[i], myV),
					long myV => EditorGUI.LongField(currPos, obj.PropertyNames[i], myV),
					Vector2 myV => EditorGUI.Vector2Field(currPos, obj.PropertyNames[i], myV),
					Vector2Int myV => EditorGUI.Vector2IntField(currPos, obj.PropertyNames[i], myV),
					Vector3 myV => EditorGUI.Vector3Field(currPos, obj.PropertyNames[i], myV),
					Vector3Int myV => EditorGUI.Vector3IntField(currPos, obj.PropertyNames[i], myV),
					Bounds myV => EditorGUI.BoundsField(currPos, obj.PropertyNames[i], myV),
					Color myV => EditorGUI.ColorField(currPos, obj.PropertyNames[i], myV),
					Rect myV => EditorGUI.RectField(currPos, obj.PropertyNames[i], myV),
					RectInt myV => EditorGUI.RectIntField(currPos, obj.PropertyNames[i], myV),
					Vector4 myV => EditorGUI.Vector4Field(currPos, obj.PropertyNames[i], myV),
					AnimationCurve myV => EditorGUI.CurveField(currPos, obj.PropertyNames[i], myV),
					Gradient myV => EditorGUI.GradientField(currPos, obj.PropertyNames[i], myV),
					Object myV => EditorGUI.ObjectField(currPos, obj.PropertyNames[i], myV, obj.PropertyTypes[i], true),
					_ => throw new System.NotImplementedException()
				};
				currPos.y += currPos.height;
				if (EditorGUI.EndChangeCheck()/*newVal != currVal*/)
				{
					if (IsSuperDebug) Debug.Log($"Changed {obj.PropertyNames[i]} Property Value from {currVal} to {newVal}");
					Undo.RecordObject((Object)containerObj, $"Changed {obj.PropertyNames[i]} Property Value from {currVal} to {newVal}");
					if (debug != obj.PropertyValues[i])
					{
						/*obj.PropertyValues[i]*/
						containerSerialized.FindProperty($"{property.name}.PropertyValues").GetArrayElementAtIndex(i).stringValue = debug;
						return;
					}
					// obj.
					// var trueProp = property.serializedObject.FindProperty("PropertyValues").GetArrayElementAtIndex(i);
					/*switch (currVal)
					{
						case int myO:
							// trueProp.intValue = (int)newVal;
							obj.PropertyValues[i] = JsonUtility.ToJson((int)newVal);
							break;
						case float myO:
							// trueProp.floatValue = (float)newVal;
							obj.PropertyValues[i] = JsonUtility.ToJson((float)newVal);
							break;
						case bool myO:
							// trueProp.boolValue = (bool)newVal;
							obj.PropertyValues[i] = JsonUtility.ToJson((bool)newVal);
							break;
						case long myO:
							// trueProp.longValue = (long)newVal;
							obj.PropertyValues[i] = JsonUtility.ToJson((long)newVal);
							break;
						case Vector2 myO:
							// trueProp.vector2Value = (Vector2)newVal;
							obj.PropertyValues[i] = JsonUtility.ToJson((Vector2)newVal);
							break;
						case Vector2Int myO:
							// trueProp.vector2IntValue = (Vector2Int)newVal;
							obj.PropertyValues[i] = JsonUtility.ToJson((Vector2Int)newVal);
							break;
						case Vector3 myO:
							// trueProp.vector3Value = (Vector3)newVal;
							obj.PropertyValues[i] = JsonUtility.ToJson((Vector3)newVal);
							break;
						case Vector3Int myO:
							// trueProp.vector3IntValue = (Vector3Int)newVal;
							obj.PropertyValues[i] = JsonUtility.ToJson((Vector3Int)newVal);
							break;
						case Bounds myO:
							// trueProp.boundsValue = (Bounds)newVal;
							obj.PropertyValues[i] = JsonUtility.ToJson((Bounds)newVal);
							break;
						case Color myO:
							// trueProp.colorValue = (Color)newVal;
							obj.PropertyValues[i] = JsonUtility.ToJson((Color)newVal);
							break;
						case Rect myO:
							// trueProp.rectValue = (Rect)newVal;
							obj.PropertyValues[i] = JsonUtility.ToJson((Rect)newVal);
							break;
						case RectInt myO:
							// trueProp.rectIntValue = (RectInt)newVal;
							obj.PropertyValues[i] = JsonUtility.ToJson((RectInt)newVal);
							break;
						case Vector4 myO:
							// trueProp.vector4Value = (Vector4)newVal;
							obj.PropertyValues[i] = JsonUtility.ToJson((Vector4)newVal);
							break;
						case AnimationCurve myO:
							// trueProp.animationCurveValue = (AnimationCurve)newVal;
							obj.PropertyValues[i] = JsonUtility.ToJson((AnimationCurve)newVal);
							break;
						// case Gradient myO:
						// // 	trueProp.gradientValue = (Gradient)newVal;
						//	obj.PropertyValues[i] = JsonUtility.ToJson((Gradient)newVal);
						// 	break;
						case Object myO:
							// trueProp.objectReferenceValue = (Object)newVal;
							obj.PropertyValues[i] = JsonUtility.ToJson((Object)newVal);
							break;
						default:
							throw new System.NotImplementedException();
					};*/
					containerSerialized.FindProperty($"{property.name}.PropertyValues").GetArrayElementAtIndex(i).stringValue = PropertyInspectorObject.Stringify(newVal, obj.PropertyTypes[i]);
					property.serializedObject.ApplyModifiedProperties();
				}
			}
			
			isDebug = new_isDebug;
			isSuperDebug = new_isSuperDebug;

			#region Debug Toggle
			Event e = Event.current;
        	if (e.type == EventType.MouseDown && e.button == 1 && position.Contains(e.mousePosition)) {
				// GenericMenu.MenuFunction2 f = (object d) => {((PropertyInspectorObjectDrawer)d).noDebug = !((PropertyInspectorObjectDrawer)d).noDebug;};
				GenericMenu.MenuFunction f = () => noDebug = !noDebug;
        	    GenericMenu context = new GenericMenu ();
        	    // context.AddItem(new GUIContent ("Toggle Debug"), true, f, this);
        	    context.AddItem(new GUIContent ("Toggle Debug"), !noDebug, f);
        	    context.ShowAsContext();
        	}
			#endregion

		}
	}
}
