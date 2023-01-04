using System;
using UnityEditor;
using UnityEngine;
using E_GUI = UnityEditor.EditorGUI;
using EGU = UnityEditor.EditorGUIUtility;
using Object = UnityEngine.Object;
using PIO = JMor.Utility.Inspector.PropertyInspectorObject;

namespace JMor.Utility.Inspector.EditorScripts
{
	[CustomPropertyDrawer(typeof(PIO))]
	public class PropertyInspectorObjectDrawer : PropertyDrawer
	{
		static readonly string 
			pion = typeof(PIO).Name, 
			pc = Util.GetMemberName((PIO p) => p.container), 
			npv = Util.GetMemberName((PIO p) => p.newPropertyValues);
		private SerializedObject Get_MyContainerSerialized(SerializedProperty property) => property.serializedObject;
		private object Get_MyContainer(SerializedProperty property) => property.serializedObject.targetObject;
		private PIO Get_PIO(SerializedProperty property) => property.managedReferenceValue as PIO;
		public bool noDebug = true;
		public bool isDebug = true;
		public bool IsDebug => !noDebug && isDebug;
		public bool isSuperDebug = false;
		public bool IsSuperDebug => IsDebug && isSuperDebug;
		public float GetNumberOfLines(SerializedProperty property, GUIContent label) =>
			Get_PIO(property).PropertyNames.Count * (IsDebug ? 2f : 1f) +
			Get_PIO(property).GetAllHeaders().Length +
			(!noDebug ? (isDebug ? 2f : 1f) : 0f);
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => (
			(EGU.singleLineHeight + EGU.standardVerticalSpacing) *
			GetNumberOfLines(property, label) *
			(EGU.wideMode ? 1f : 2f)
		) + Get_PIO(property).GetTotalAddedSpaceFromSpaceAttributes();
		/// <summary>
        /// Distance from the top of 1 element to the top of the next.
        /// </summary>
		public float YOffset => EGU.singleLineHeight + EGU.standardVerticalSpacing;
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
			if (obj.container == null || obj.containerType == null)
				Debug.LogWarning($"{pion} not properly initialized, hooking up through SerializedProperty.");
			else if (obj.container != containerObj)
				Debug.LogError($"{pion}.{pc} != SerializedProperty.{pc}; Desynced. Major problem. Syncing to SerializedProperty.{pc}. Requires investigation.");
			obj.container = (obj.container != containerObj) ? containerObj : obj.container;
			obj.containerType = containerObj.GetType();
			var containerSerialized = Get_MyContainerSerialized(property);
			#endregion

			var currPos = position;
			currPos.height = EGU.singleLineHeight;

			#region Debug buttons
			var new_isDebug = isDebug;
			var new_isSuperDebug = isSuperDebug;
			if (!noDebug)
			{
				new_isDebug = E_GUI.Toggle(currPos, "Debug?", isDebug);
				currPos.y += YOffset;
				if (isDebug)
				{
					new_isSuperDebug = E_GUI.Toggle(currPos, "Super Debug?", isSuperDebug);
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
				var (pName, pType, pValue) = obj[i];
				var orderedAttribs = obj.GetOrderedAttributes(i);
				if (IsSuperDebug) Debug.Log($"obj.GetOrderedAttributes({i}).Length: {orderedAttribs.Length}");
				for (int j = 0; j < orderedAttribs?.Length; j++)
				{
					if (typeof(SpaceAttribute).IsInstanceOfType(orderedAttribs[j]))//(orderedAttribs[j].GetType() == typeof(SpaceAttribute))
					{
						var h = (orderedAttribs[j] as SpaceAttribute).height;
						currPos.y += h;
						if (IsSuperDebug) Debug.Log($"CurrSpace.height: {h}");
					}
					else if (typeof(HeaderAttribute).IsInstanceOfType(orderedAttribs[j]))//(orderedAttribs[j].GetType() == typeof(HeaderAttribute))
					{
						var temp = new GUIContent(
							(orderedAttribs[j] as HeaderAttribute).header, 
							(orderedAttribs[j].GetType() == typeof(Alias.HeaderAttribute)) ? 
								(orderedAttribs[j] as Alias.HeaderAttribute).Tooltip : 
								"");
						E_GUI.LabelField(currPos, temp);
						currPos.y += YOffset;
						if (IsSuperDebug) Debug.Log($"CurrHeader.header: text:{temp.text} tooltip:{temp.tooltip}");
					}
				}
				var currVal = PIO.Parse(pValue, pType);
				E_GUI.BeginChangeCheck();
				string debug = pValue;
				if (IsDebug)
				{
					debug = E_GUI.TextField(
						currPos,
						new GUIContent($"{obj.GetInspectorName(i)} Serialized", obj.GetTooltip(i) ?? ""),
						pValue);
					currPos.y += YOffset;
				}
				object newVal = currVal;
				if (obj.GetUnityEngineAttribute<HideInInspector>(i) == null)
				{
					var content = new GUIContent(obj.GetInspectorName(i), obj.GetTooltip(i) ?? "");
					newVal = currVal switch
					{
						int				myV => E_GUI.IntField		(currPos, content, myV),
						float			myV => E_GUI.FloatField		(currPos, content, myV),
						bool			myV => E_GUI.Toggle			(currPos, content, myV),
						long			myV => E_GUI.LongField		(currPos, content, myV),
						Vector2			myV => E_GUI.Vector2Field	(currPos, content, myV),
						Vector2Int		myV => E_GUI.Vector2IntField(currPos, content, myV),
						Vector3			myV => E_GUI.Vector3Field	(currPos, content, myV),
						Vector3Int		myV => E_GUI.Vector3IntField(currPos, content, myV),
						Bounds			myV => E_GUI.BoundsField	(currPos, content, myV),
						Color			myV => E_GUI.ColorField		(currPos, content, myV),
						Rect			myV => E_GUI.RectField		(currPos, content, myV),
						RectInt			myV => E_GUI.RectIntField	(currPos, content, myV),
						Vector4			myV => E_GUI.Vector4Field	(currPos, content, myV),
						AnimationCurve	myV => E_GUI.CurveField		(currPos, content, myV),
						Gradient		myV => E_GUI.GradientField	(currPos, content, myV),
						Object			myV => E_GUI.ObjectField	(currPos, content, myV, pType, true),
						_ => throw new NotImplementedException(),
					};
					currPos.y += YOffset;
				}
				if (E_GUI.EndChangeCheck()/* && (newVal != currVal || debug != pValue)*/)
				{
					if (IsSuperDebug) Debug.Log($"Changed {pName} Property Value from {currVal} to {newVal}");
					Undo.RecordObject((Object)containerObj, $"Changed {pName} Property Value from {currVal} to {newVal}");
					if (debug != pValue)
					{
						if (IsSuperDebug) Debug.Log($"Changed {pName} Serialized Property Value from {pValue} to {debug}");
						Undo.RecordObject((Object)containerObj, $"Changed {pName} Serialized Property Value from {pValue} to {debug}");
						containerSerialized
							.FindProperty($"{property.name}.{npv}")
							.GetArrayElementAtIndex(i)
							.stringValue = debug;
					}
					else
					{
						if (IsSuperDebug) Debug.Log($"Changed {pName} Property Value from {currVal} to {newVal}");
						Undo.RecordObject((Object)containerObj, $"Changed {pName} Property Value from {currVal} to {newVal}");
						containerSerialized
							.FindProperty($"{property.name}.{npv}")
							.GetArrayElementAtIndex(i)
							.stringValue = PIO.Stringify(newVal, pType);
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
