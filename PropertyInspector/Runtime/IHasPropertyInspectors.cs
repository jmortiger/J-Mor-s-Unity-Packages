using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace JMor.Utility.Inspector
{
	public interface IHasPropertyInspectors<T> : ISerializationCallbackReceiver
	{
		protected List<PropertyInfo> PropertyReflectionData { get; set; }
		public List<string> PropertyNames { get; protected set; }
		public List<Type> PropertyTypes { get; protected set; }
		public List<string> PropertyValues { get; set; }
		new void OnAfterDeserialize() => ((ISerializationCallbackReceiver)this).OnAfterDeserialize();
		void ISerializationCallbackReceiver.OnAfterDeserialize()
		{
			var props = typeof(T).GetProperties(
				BindingFlags.Public |
				BindingFlags.NonPublic);
			PropertyReflectionData = PropertyReflectionData ?? new List<PropertyInfo>();
			PropertyNames = PropertyNames ?? new List<string>();
			PropertyTypes = PropertyTypes ?? new List<Type>();
			PropertyValues = PropertyValues ?? new List<string>();
			for (int i = 0; i < props.Length; i++)
			{
				foreach (var a in props[i].CustomAttributes)
				{
					if (a.AttributeType == typeof(PropertyInspectorAttribute))
					{
						PropertyReflectionData.Add(props[i]);
						break;
					}
				}
			}
			for (int i = 0; i < PropertyReflectionData.Count; i++)
			{
				PropertyReflectionData[i].SetValue((T)this, JsonUtility.FromJson(PropertyValues[i], PropertyTypes[i]));
			}
		}

		new void OnBeforeSerialize() => ((ISerializationCallbackReceiver)this).OnBeforeSerialize();
		void ISerializationCallbackReceiver.OnBeforeSerialize()
		{
			var props = typeof(T).GetProperties(
				BindingFlags.Public |
				BindingFlags.NonPublic);
			PropertyReflectionData = PropertyReflectionData ?? new List<PropertyInfo>();
			PropertyNames = PropertyNames ?? new List<string>();
			PropertyTypes = PropertyTypes ?? new List<Type>();
			PropertyValues = PropertyValues ?? new List<string>();
			for (int i = 0; i < props.Length; i++)
			{
				bool isInspected = false;
				foreach (var a in props[i].CustomAttributes)
				{
					if (a.AttributeType == typeof(PropertyInspectorAttribute))
					{
						isInspected = true;
						break;
					}
				}
				if (isInspected && !PropertyReflectionData.Contains(props[i]))
				{
					PropertyReflectionData.Add(props[i]);
					var ind = PropertyNames.IndexOf(props[i].Name);
					if (ind < 0)
					{
						PropertyNames.Add(props[i].Name);
						PropertyTypes.Add(props[i].PropertyType);
						PropertyValues.Add(JsonUtility.ToJson(props[i].GetValue((T)this)));
					}
					else
					{
						PropertyNames[ind] = props[i].Name;
						PropertyTypes[ind] = props[i].PropertyType;
						PropertyValues[ind] = JsonUtility.ToJson(props[i].GetValue((T)this));
					}
				}
				if (isInspected && PropertyReflectionData.Contains(props[i]))
				{
					// PropertyReflectionData.Add(props[i]);
					var ind = PropertyNames.IndexOf(props[i].Name);
					if (ind < 0)
					{
						PropertyNames.Add(props[i].Name);
						PropertyTypes.Add(props[i].PropertyType);
						PropertyValues.Add(JsonUtility.ToJson(props[i].GetValue((T)this)));
					}
					else
					{
						PropertyNames[ind] = props[i].Name;
						PropertyTypes[ind] = props[i].PropertyType;
						PropertyValues[ind] = JsonUtility.ToJson(props[i].GetValue((T)this));
					}
				}
				else if (!isInspected && PropertyReflectionData.Contains(props[i]))
				{
					PropertyReflectionData.Remove(props[i]);
					var ind = PropertyNames.IndexOf(props[i].Name);
					if (ind < 0) { }
					else
					{
						PropertyNames.RemoveAt(ind);
						PropertyTypes.RemoveAt(ind);
						PropertyValues.RemoveAt(ind);
					}
					--i;
				}
			}
		}
	}
	public interface IHasPropertyInspectors : ISerializationCallbackReceiver
	{
		protected List<PropertyInfo> PropertyReflectionData { get; set; }
		public List<string> PropertyNames { get; protected set; }
		public List<Type> PropertyTypes { get; protected set; }
		public List<string> PropertyValues { get; set; }
		public abstract Type MyDerivedType { get; }
		new void OnAfterDeserialize() => ((ISerializationCallbackReceiver)this).OnAfterDeserialize();
		void ISerializationCallbackReceiver.OnAfterDeserialize()
		{
			var props = MyDerivedType.GetProperties(
				BindingFlags.Public |
				BindingFlags.NonPublic);
			PropertyReflectionData = PropertyReflectionData ?? new List<PropertyInfo>();
			PropertyNames = PropertyNames ?? new List<string>();
			PropertyTypes = PropertyTypes ?? new List<Type>();
			PropertyValues = PropertyValues ?? new List<string>();
			for (int i = 0; i < props.Length; i++)
			{
				foreach (var a in props[i].CustomAttributes)
				{
					if (a.AttributeType == typeof(PropertyInspectorAttribute))
					{
						PropertyReflectionData.Add(props[i]);
						break;
					}
				}
			}
			for (int i = 0; i < PropertyReflectionData.Count; i++)
			{
				PropertyReflectionData[i].SetValue(Convert.ChangeType(this, this.MyDerivedType), JsonUtility.FromJson(PropertyValues[i], PropertyTypes[i]));
			}
		}

		new void OnBeforeSerialize() => ((ISerializationCallbackReceiver)this).OnBeforeSerialize();
		void ISerializationCallbackReceiver.OnBeforeSerialize()
		{
			var props = MyDerivedType.GetProperties(
				BindingFlags.Public |
				BindingFlags.NonPublic);
			PropertyReflectionData = PropertyReflectionData ?? new List<PropertyInfo>();
			PropertyNames = PropertyNames ?? new List<string>();
			PropertyTypes = PropertyTypes ?? new List<Type>();
			PropertyValues = PropertyValues ?? new List<string>();
			for (int i = 0; i < props.Length; i++)
			{
				bool isInspected = false;
				foreach (var a in props[i].CustomAttributes)
				{
					if (a.AttributeType == typeof(PropertyInspectorAttribute))
					{
						isInspected = true;
						break;
					}
				}
				if (isInspected && !PropertyReflectionData.Contains(props[i]))
				{
					PropertyReflectionData.Add(props[i]);
					var ind = PropertyNames.IndexOf(props[i].Name);
					if (ind < 0)
					{
						PropertyNames.Add(props[i].Name);
						PropertyTypes.Add(props[i].PropertyType);
						PropertyValues.Add(JsonUtility.ToJson(props[i].GetValue(Convert.ChangeType(this, this.MyDerivedType))));
					}
					else
					{
						PropertyNames[ind] = props[i].Name;
						PropertyTypes[ind] = props[i].PropertyType;
						PropertyValues[ind] = JsonUtility.ToJson(props[i].GetValue(Convert.ChangeType(this, this.MyDerivedType)));
					}
				}
				if (isInspected && PropertyReflectionData.Contains(props[i]))
				{
					// PropertyReflectionData.Add(props[i]);
					var ind = PropertyNames.IndexOf(props[i].Name);
					if (ind < 0)
					{
						PropertyNames.Add(props[i].Name);
						PropertyTypes.Add(props[i].PropertyType);
						PropertyValues.Add(JsonUtility.ToJson(props[i].GetValue(Convert.ChangeType(this, this.MyDerivedType))));
					}
					else
					{
						PropertyNames[ind] = props[i].Name;
						PropertyTypes[ind] = props[i].PropertyType;
						PropertyValues[ind] = JsonUtility.ToJson(props[i].GetValue(Convert.ChangeType(this, this.MyDerivedType)));
					}
				}
				else if (!isInspected && PropertyReflectionData.Contains(props[i]))
				{
					PropertyReflectionData.Remove(props[i]);
					var ind = PropertyNames.IndexOf(props[i].Name);
					if (ind < 0) { }
					else
					{
						PropertyNames.RemoveAt(ind);
						PropertyTypes.RemoveAt(ind);
						PropertyValues.RemoveAt(ind);
					}
					--i;
				}
			}
		}
	}
}
