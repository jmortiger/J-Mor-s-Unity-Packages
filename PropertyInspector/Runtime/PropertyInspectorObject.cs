using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace JMor.Utility.Inspector
{
	// TODO: Tooltip/HideInInspector/SerializeField Attribute Support
	// TODO: Stop overwriting changes to the field that come from other sources.
	// TODO: Rewrite members to match conventions (field => lowerCamelCase, Properties => UpperCamelCase)
	[Serializable] 
	public class PropertyInspectorObject : ISerializationCallbackReceiver
	{
		#region Fields & Properties
		public Tuple<string, Type, string> this[int index]
		{
			get => new(PropertyNames[index], PropertyTypes[index], PropertyValues[index]);
			set
			{
				(PropertyNames[index], _, PropertyValues[index]) = value;
				PropertyTypesStored[index] = value.Item2.AssemblyQualifiedName;
			}
		}
		public List<PropertyInfo> PropertyReflectionData { get; private set; }// protected List<PropertyInfo> PropertyReflectionData { get; set; }
		public List<string> PropertyNames;
		public List<Type> PropertyTypes
		{
			get => PropertyTypesStored == null ? null : PropertyTypesStored.ConvertAll<Type>(s => Type.GetType(s));
			set => PropertyTypesStored = value == null ? null : value.ConvertAll<string>(t => t.AssemblyQualifiedName);
		}
		/// <summary>
		/// <see cref="Type"/> can't be serialized, so we use this for serialization of <see cref="PropertyTypes"/>.
		/// </summary>
		public List<string> PropertyTypesStored = new List<string>();
		public List<string> PropertyValues;
		public object container;
		public Type containerType
		{
			get => Type.GetType(containerTypeStored);
			set => containerTypeStored = value.AssemblyQualifiedName;
		}
		/// <summary>
		/// <see cref="Type"/> can't be serialized, so we use this for serialization of <see cref="containerType"/>.
		/// </summary>
		public string containerTypeStored;
		#endregion
		public PropertyInspectorObject(object container, Type containerType)
		{
			this.container = container;
			this.containerType = containerType;
		}
		public void OnAfterDeserialize()/* => ((ISerializationCallbackReceiver)this).OnAfterDeserialize();
		void ISerializationCallbackReceiver.OnAfterDeserialize()*/
		{
			PropertyReflectionData = PropertyReflectionData ?? new List<PropertyInfo>();
			PropertyNames = PropertyNames ?? new List<string>();
			PropertyTypes = PropertyTypes ?? new List<Type>();
			PropertyValues = PropertyValues ?? new List<string>();
			for (int i = 0; i < PropertyReflectionData.Count; i++)
				PropertyReflectionData[i].SetValue(
					Convert.ChangeType(container, containerType),
					IsLoneValueType(PropertyReflectionData[i].PropertyType) ? 
						Parse(PropertyValues[i], PropertyTypes[i]) : 
						JsonUtility.FromJson(PropertyValues[i], PropertyTypes[i]));
		}

		public void OnBeforeSerialize()/* => ((ISerializationCallbackReceiver)this).OnBeforeSerialize();
		void ISerializationCallbackReceiver.OnBeforeSerialize()*/
		{
			if (containerType == null || container == null)
			{
				Debug.LogWarning($"Not initialized ({(containerType == null ? "type" : "")}{(containerType == null && container == null ? " and " : "")}{(container == null ? "container" : "")} = null), canceling serialization (OnBeforeSerialize)");
				return;
			}
			var props = containerType.GetProperties(
				BindingFlags.Public |
				BindingFlags.NonPublic |
				BindingFlags.Instance);
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
						PropertyTypesStored.Add(props[i].PropertyType.AssemblyQualifiedName);
						PropertyValues.Add((IsLoneValueType(props[i].PropertyType)) ? props[i].GetValue(Convert.ChangeType(container, containerType)).ToString() : JsonUtility.ToJson(props[i].GetValue(Convert.ChangeType(container, containerType))));
					}
					else
					{
						PropertyNames[ind] = props[i].Name;
						PropertyTypesStored[ind] = props[i].PropertyType.AssemblyQualifiedName;
						PropertyValues[ind] = ((IsLoneValueType(props[i].PropertyType)) ? props[i].GetValue(Convert.ChangeType(container, containerType)).ToString() : JsonUtility.ToJson(props[i].GetValue(Convert.ChangeType(container, containerType))));
					}
				}
				if (isInspected && PropertyReflectionData.Contains(props[i]))
				{
					var ind = PropertyNames.IndexOf(props[i].Name);
					if (ind < 0)
					{
						PropertyNames.Add(props[i].Name);
						PropertyTypesStored.Add(props[i].PropertyType.AssemblyQualifiedName);
						PropertyValues.Add((IsLoneValueType(props[i].PropertyType)) ? props[i].GetValue(Convert.ChangeType(container, containerType)).ToString() : JsonUtility.ToJson(props[i].GetValue(Convert.ChangeType(container, containerType))));
					}
					else
					{
						PropertyNames[ind] = props[i].Name;
						PropertyTypesStored[ind] = props[i].PropertyType.AssemblyQualifiedName;
						PropertyValues[ind] = ((IsLoneValueType(props[i].PropertyType)) ? props[i].GetValue(Convert.ChangeType(container, containerType)).ToString() : JsonUtility.ToJson(props[i].GetValue(Convert.ChangeType(container, containerType))));
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
						PropertyTypesStored.RemoveAt(ind);
						PropertyValues.RemoveAt(ind);
					}
					--i;
				}
			}
		}

		#region Helpers
		#region IsLoneValueType
		public static bool IsLoneValueType<T>(T val)
		{
			switch (val)
			{
				case byte:
				case sbyte:
				case short:
				case ushort:
				case int:
				case uint:
				case long:
				case ulong:
				case float:
				case double:
				case bool:
				case string:
					return true;
				default:
					return false;
			}
		}
		public static bool IsLoneValueType(Type val)
		{
			switch (val)
			{
				case Type t_byte when t_byte == typeof(byte):
				case Type t_sbyte when t_sbyte == typeof(sbyte):
				case Type t_short when t_short == typeof(short):
				case Type t_ushort when t_ushort == typeof(ushort):
				case Type t_int when t_int == typeof(int):
				case Type t_uint when t_uint == typeof(uint):
				case Type t_long when t_long == typeof(long):
				case Type t_ulong when t_ulong == typeof(ulong):
				case Type t_float when t_float == typeof(float):
				case Type t_double when t_double == typeof(double):
				case Type t_bool when t_bool == typeof(bool):
				case Type t_string when t_string == typeof(string):
					return true;
				default:
					return false;
			}
		}
		#endregion
		#region Stringify
		public static string Stringify<T>(T value) where T : /*struct, */IComparable, IComparable<T>, IConvertible, IEquatable<T>//, IFormattable
		{
			return value.ToString();
		}
		public static string Stringify(object value, Type t)
		{
			switch (t)
			{
				case Type t_byte when t_byte == typeof(byte):
					return PropertyInspectorObject.Stringify<byte>((byte)value);
				case Type t_sbyte when t_sbyte == typeof(sbyte):
					return PropertyInspectorObject.Stringify<sbyte>((sbyte)value);
				case Type t_short when t_short == typeof(short):
					return PropertyInspectorObject.Stringify<short>((short)value);
				case Type t_ushort when t_ushort == typeof(ushort):
					return PropertyInspectorObject.Stringify<ushort>((ushort)value);
				case Type t_int when t_int == typeof(int):
					return PropertyInspectorObject.Stringify<int>((int)value);
				case Type t_uint when t_uint == typeof(uint):
					return PropertyInspectorObject.Stringify<uint>((uint)value);
				case Type t_long when t_long == typeof(long):
					return PropertyInspectorObject.Stringify<long>((long)value);
				case Type t_ulong when t_ulong == typeof(ulong):
					return PropertyInspectorObject.Stringify<ulong>((ulong)value);
				case Type t_float when t_float == typeof(float):
					return PropertyInspectorObject.Stringify<float>((float)value);
				case Type t_double when t_double == typeof(double):
					return PropertyInspectorObject.Stringify<double>((double)value);
				case Type t_bool when t_bool == typeof(bool):
					return PropertyInspectorObject.Stringify<bool>((bool)value);
				case Type t_string when t_string == typeof(string):
					return PropertyInspectorObject.Stringify<string>((string)value);
				default:
					return JsonUtility.ToJson(Convert.ChangeType(value, t));
			}
		}
		public static string Stringify(byte value) => value.ToString();
		public static string Stringify(sbyte value) => value.ToString();
		public static string Stringify(short value) => value.ToString();
		public static string Stringify(ushort value) => value.ToString();
		public static string Stringify(int value) => value.ToString();
		public static string Stringify(uint value) => value.ToString();
		public static string Stringify(long value) => value.ToString();
		public static string Stringify(ulong value) => value.ToString();
		public static string Stringify(float value) => value.ToString();
		public static string Stringify(double value) => value.ToString();
		public static string Stringify(bool value) => value.ToString();
		public static string Stringify(string value) => value.ToString();
		#endregion
		#region Parse
		public static object Parse(string value, Type t)
		{
			switch (t)
			{
				case Type t_byte when t_byte == typeof(byte):
					PropertyInspectorObject.Parse(value, out byte o_byte);
					return o_byte;
				case Type t_sbyte when t_sbyte == typeof(sbyte):
					PropertyInspectorObject.Parse(value, out sbyte o_sbyte);
					return o_sbyte;
				case Type t_short when t_short == typeof(short):
					PropertyInspectorObject.Parse(value, out short o_short);
					return o_short;
				case Type t_ushort when t_ushort == typeof(ushort):
					PropertyInspectorObject.Parse(value, out ushort o_ushort);
					return o_ushort;
				case Type t_int when t_int == typeof(int):
					PropertyInspectorObject.Parse(value, out int o_int);
					return o_int;
				case Type t_uint when t_uint == typeof(uint):
					PropertyInspectorObject.Parse(value, out uint o_uint);
					return o_uint;
				case Type t_long when t_long == typeof(long):
					PropertyInspectorObject.Parse(value, out long o_long);
					return o_long;
				case Type t_ulong when t_ulong == typeof(ulong):
					PropertyInspectorObject.Parse(value, out ulong o_ulong);
					return o_ulong;
				case Type t_float when t_float == typeof(float):
					PropertyInspectorObject.Parse(value, out float o_float);
					return o_float;
				case Type t_double when t_double == typeof(double):
					PropertyInspectorObject.Parse(value, out double o_double);
					return o_double;
				case Type t_bool when t_bool == typeof(bool):
					PropertyInspectorObject.Parse(value, out bool o_bool);
					return o_bool;
				case Type t_string when t_string == typeof(string):
					PropertyInspectorObject.Parse(value, out string o_string);
					return o_string;
				default:
					return JsonUtility.FromJson(value, t);
			}
		}
		public static void Parse(string value, out byte output) => output = byte.Parse(value);
		public static void Parse(string value, out sbyte output) => output = sbyte.Parse(value);
		public static void Parse(string value, out short output) => output = short.Parse(value);
		public static void Parse(string value, out ushort output) => output = ushort.Parse(value);
		public static void Parse(string value, out int output) => output = int.Parse(value);
		public static void Parse(string value, out uint output) => output = uint.Parse(value);
		public static void Parse(string value, out long output) => output = long.Parse(value);
		public static void Parse(string value, out ulong output) => output = ulong.Parse(value);
		public static void Parse(string value, out float output) => output = float.Parse(value);
		public static void Parse(string value, out double output) => output = double.Parse(value);
		public static void Parse(string value, out bool output) => output = bool.Parse(value);
		public static void Parse(string value, out string output) => output = value;
		#endregion
		#endregion

	}
}
