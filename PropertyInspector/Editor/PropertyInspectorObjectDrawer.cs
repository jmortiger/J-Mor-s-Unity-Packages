using UnityEditor;
using UnityEngine;
using PIO = JMor.Utility.Inspector.PropertyInspectorObject;

namespace JMor.Utility.Inspector.EditorScripts
{
	[CustomPropertyDrawer(typeof(PIO))]
	public class PropertyInspectorObjectDrawer : PropertyDrawer
	{
		private SerializedObject Get_MyContainerSerialized(SerializedProperty property) => property.serializedObject;
		private object Get_MyContainer(SerializedProperty property) => property.serializedObject.targetObject;
		private PIO Get_PIO(SerializedProperty property) => property.managedReferenceValue.As<PIO>();
		public bool noDebug = true;
		public bool isDebug = true;
		public bool IsDebug => !noDebug && isDebug;
		public bool isSuperDebug = false;
		public bool IsSuperDebug => IsDebug && isSuperDebug;
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => (
			(EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) *
			(Get_PIO(property).PropertyNames.Count * (IsDebug ? 2f : 1f) + (!noDebug ? (isDebug ? 2f : 1f) : 0f)) *
			(EditorGUIUtility.wideMode ? 1f : 2f)
		) + Get_PIO(property).GetTotalAddedSpaceFromSpaceAttributes();
		/// <summary>
        /// Distance from the top of 1 element to the top of the next.
        /// </summary>
		public float YOffset => EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
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
			if (obj.container == null/* || obj.containerType == null*/)
				Debug.LogWarning($"{typeof(PIO).Name} not properly initialized, hooking up through SerializedProperty.");
			else if (obj.container != containerObj)
				Debug.LogError($"{typeof(PIO).Name}.{Util.GetMemberName((PIO p) => p.container)} != SerializedProperty.{Util.GetMemberName((PIO p) => p.container)}; Desynced. Major problem. Syncing to SerializedProperty.{Util.GetMemberName((PIO p) => p.container)}. Requires investigation.");
			obj.container = (obj.container != containerObj) ? containerObj : obj.container;
			// obj.containerType = containerObj.GetType();
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
				currPos.y += YOffset;
				if (isDebug)
				{
					new_isSuperDebug = EditorGUI.Toggle(currPos, "Super Debug?", isSuperDebug);
					currPos.y += YOffset;
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
				// Handle Space attrib
				currPos.y += obj.GetSpace(i)/* + EditorGUIUtility.standardVerticalSpacing*/;
				if (IsSuperDebug) Debug.Log($"Space attrib: {obj.GetSpace(i)/*} + {EditorGUIUtility.standardVerticalSpacing*/}");
				var currVal = PIO.Parse(obj.PropertyValues[i], obj.PropertyTypes[i]);
				EditorGUI.BeginChangeCheck();
				string debug = obj.PropertyValues[i];
				if (IsDebug)
				{
					debug = EditorGUI.TextField(
						currPos,
						new GUIContent($"{obj.PropertyNames[i]} Serialized", obj.GetTooltip(i) ?? ""),
						obj.PropertyValues[i]);
					currPos.y += YOffset;
				}
				object newVal = currVal;
				if (obj.GetUnityEngineAttribute<HideInInspector>(i) == null)
				{
					var content = new GUIContent(obj.PropertyNames[i], obj.GetTooltip(i) ?? "");
					newVal = currVal switch
					{
						int				myV => EditorGUI.IntField		(currPos, content, myV),
						float			myV => EditorGUI.FloatField		(currPos, content, myV),
						bool			myV => EditorGUI.Toggle			(currPos, content, myV),
						long			myV => EditorGUI.LongField		(currPos, content, myV),
						Vector2			myV => EditorGUI.Vector2Field	(currPos, content, myV),
						Vector2Int		myV => EditorGUI.Vector2IntField(currPos, content, myV),
						Vector3			myV => EditorGUI.Vector3Field	(currPos, content, myV),
						Vector3Int		myV => EditorGUI.Vector3IntField(currPos, content, myV),
						Bounds			myV => EditorGUI.BoundsField	(currPos, content, myV),
						Color			myV => EditorGUI.ColorField		(currPos, content, myV),
						Rect			myV => EditorGUI.RectField		(currPos, content, myV),
						RectInt			myV => EditorGUI.RectIntField	(currPos, content, myV),
						Vector4			myV => EditorGUI.Vector4Field	(currPos, content, myV),
						AnimationCurve	myV => EditorGUI.CurveField		(currPos, content, myV),
						Gradient		myV => EditorGUI.GradientField	(currPos, content, myV),
						Object			myV => EditorGUI.ObjectField	(currPos, content, myV, obj.PropertyTypes[i], true),
						_ => throw new System.NotImplementedException(),
					};
					currPos.y += YOffset;
				}
				if (EditorGUI.EndChangeCheck()/* && (newVal != currVal || debug != obj.PropertyValues[i])*/)
				{
					if (IsSuperDebug) Debug.Log($"Changed {obj.PropertyNames[i]} Property Value from {currVal} to {newVal}");
					Undo.RecordObject((Object)containerObj, $"Changed {obj.PropertyNames[i]} Property Value from {currVal} to {newVal}");
					if (debug != obj.PropertyValues[i])
					{
						if (IsSuperDebug) Debug.Log($"Changed {obj.PropertyNames[i]} Serialized Property Value from {obj.PropertyValues[i]} to {debug}");
						Undo.RecordObject((Object)containerObj, $"Changed {obj.PropertyNames[i]} Serialized Property Value from {obj.PropertyValues[i]} to {debug}");
						containerSerialized
							.FindProperty($"{property.name}.{Util.GetMemberName((PIO p) => p.newPropertyValues)}")
							.GetArrayElementAtIndex(i)
							.stringValue = debug;
					}
					else
					{
						if (IsSuperDebug) Debug.Log($"Changed {obj.PropertyNames[i]} Property Value from {currVal} to {newVal}");
						Undo.RecordObject((Object)containerObj, $"Changed {obj.PropertyNames[i]} Property Value from {currVal} to {newVal}");
						containerSerialized
							.FindProperty($"{property.name}.{Util.GetMemberName((PIO p) => p.newPropertyValues)}")
							.GetArrayElementAtIndex(i)
							.stringValue = PIO.Stringify(newVal, obj.PropertyTypes[i]);
					}
					property.serializedObject.ApplyModifiedProperties();
				}
			}
			
			isDebug = new_isDebug;
			isSuperDebug = new_isSuperDebug;

			#region Debug Toggle
			Event e = Event.current;
			if (e.type == EventType.MouseDown && e.button == 1 && position.Contains(e.mousePosition))
			{
				GenericMenu context = new GenericMenu();
				context.AddItem(new GUIContent("Toggle Debug"), !noDebug, () => noDebug = !noDebug);
				context.ShowAsContext();
			}
			#endregion

		}
	}
}
