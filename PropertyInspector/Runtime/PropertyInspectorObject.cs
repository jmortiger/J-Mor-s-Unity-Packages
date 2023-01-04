using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using PIO = JMor.Utility.Inspector.Alias;

namespace JMor.Utility.Inspector
{
	// TODO: Tooltip/HideInInspector/SerializeField Attribute Support
	// TODO: Rewrite members to match conventions (field => lowerCamelCase, Properties => UpperCamelCase)
	// TODO: Collections shouldn't change size outside of this class, consider changing lists to arrays.
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
		protected List<PropertyInfo> PropertyReflectionData { get; private set; }
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
		public List<string> newPropertyValues;
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
		public void OnAfterDeserialize() // TODO: Fix Drawer only updating to match changed property positions/values after interaction
		{
			PropertyReflectionData = PropertyReflectionData ?? new List<PropertyInfo>();
			PropertyNames = PropertyNames ?? new List<string>();
			for (int i = 0; i < PropertyReflectionData.Count; i++)
				if (PropertyReflectionData?[0]?.Name != PropertyNames?[0] || 
					PropertyReflectionData?[0]?.PropertyType != PropertyTypes?[0])
				{
					RebuildLists();
					break;
				}
			PropertyTypes = PropertyTypes ?? new List<Type>();
			PropertyValues = PropertyValues ?? new List<string>();
			newPropertyValues = newPropertyValues ?? new List<string>(PropertyValues.Count);
			while (newPropertyValues.Count < PropertyValues.Count)
				newPropertyValues.Add("");
			for (int i = 0; i < PropertyReflectionData.Count; i++)
				if (!string.IsNullOrEmpty(newPropertyValues?[i]) && (TryParse(newPropertyValues[i], PropertyTypes[i], out object output) || !IsLoneValueType(PropertyReflectionData[i].PropertyType)))
				{
					PropertyValues[i] = newPropertyValues[i];
					newPropertyValues[i] = "";
					PropertyReflectionData[i].SetValue(
						Convert.ChangeType(container, containerType),
						IsLoneValueType(PropertyReflectionData[i].PropertyType) ?
							output :
							JsonUtility.FromJson(PropertyValues[i], PropertyTypes[i]));
				}
		}

		public void RebuildLists() {
			if (containerType == null || container == null)
			{
				Debug.LogWarning($"Not initialized ({(containerType == null ? "type" : "")}{(containerType == null && container == null ? " and " : "")}{(container == null ? "container" : "")} = null), canceling serialization (OnBeforeSerialize)");
				return;
			}
			var props = containerType.GetProperties(
				BindingFlags.Public |
				BindingFlags.NonPublic |
				BindingFlags.Instance);
			PropertyReflectionData = /*PropertyReflectionData ?? */new List<PropertyInfo>();
			PropertyNames = /*PropertyNames ?? */new List<string>();
			PropertyTypes = /*PropertyTypes ?? */new List<Type>();
			PropertyValues = /*PropertyValues ?? */new List<string>();
			newPropertyValues = new List<string>(PropertyValues.Count);
			PropertyValues.ForEach(e => newPropertyValues.Add(""));
			for (int i = 0; i < props.Length; i++)
			{
				void UpdateArrays(int ind)
				{
					if (ind < 0)
					{
						PropertyNames.Add(props[i].Name);
						PropertyTypesStored.Add(props[i].PropertyType.AssemblyQualifiedName);
						PropertyValues.Add(
							(IsLoneValueType(props[i].PropertyType)) ?
								props[i].GetValue(Convert.ChangeType(container, containerType)).ToString() :
								JsonUtility.ToJson(props[i].GetValue(Convert.ChangeType(container, containerType))));
						newPropertyValues.Add("");
					}
					else
					{
						PropertyNames[ind] = props[i].Name;
						PropertyTypesStored[ind] = props[i].PropertyType.AssemblyQualifiedName;
						PropertyValues[ind] = (
							(IsLoneValueType(props[i].PropertyType)) ?
								props[i].GetValue(Convert.ChangeType(container, containerType)).ToString() :
								JsonUtility.ToJson(props[i].GetValue(Convert.ChangeType(container, containerType))));
					}
				}
				bool isInspected = props[i].GetCustomAttribute<PropertyInspectorAttribute>() != null;

				if (isInspected)
				{
					if (!PropertyReflectionData.Contains(props[i]))
						PropertyReflectionData.Add(props[i]);
					UpdateArrays(PropertyNames.IndexOf(props[i].Name));
				}
				else if (!isInspected && PropertyReflectionData.Contains(props[i]))
				{
					PropertyReflectionData.Remove(props[i]);
					var ind = PropertyNames.IndexOf(props[i].Name);
					if (ind >= 0)
					{
						PropertyNames.RemoveAt(ind);
						PropertyTypesStored.RemoveAt(ind);
						PropertyValues.RemoveAt(ind);
						newPropertyValues.RemoveAt(ind);
					}
				}
			}
		}

		public void OnBeforeSerialize()
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
			newPropertyValues = new List<string>(PropertyValues.Count);
			PropertyValues.ForEach(e => newPropertyValues.Add(""));
			for (int i = 0; i < props.Length; i++)
			{
				void UpdateArrays(int ind)
				{
					if (ind < 0)
					{
						PropertyNames.Add(props[i].Name);
						PropertyTypesStored.Add(props[i].PropertyType.AssemblyQualifiedName);
						PropertyValues.Add(
							(IsLoneValueType(props[i].PropertyType)) ?
								props[i].GetValue(Convert.ChangeType(container, containerType)).ToString() :
								JsonUtility.ToJson(props[i].GetValue(Convert.ChangeType(container, containerType))));
						newPropertyValues.Add("");
					}
					else
					{
						PropertyNames[ind] = props[i].Name;
						PropertyTypesStored[ind] = props[i].PropertyType.AssemblyQualifiedName;
						PropertyValues[ind] = (
							(IsLoneValueType(props[i].PropertyType)) ?
								props[i].GetValue(Convert.ChangeType(container, containerType)).ToString() :
								JsonUtility.ToJson(props[i].GetValue(Convert.ChangeType(container, containerType))));
					}
				}
				bool isInspected = props[i].GetCustomAttribute<PropertyInspectorAttribute>() != null;

				if (isInspected)
				{
					if (!PropertyReflectionData.Contains(props[i]))
						PropertyReflectionData.Add(props[i]);
					UpdateArrays(PropertyNames.IndexOf(props[i].Name));
				}
				else if (!isInspected && PropertyReflectionData.Contains(props[i]))
				{
					PropertyReflectionData.Remove(props[i]);
					var ind = PropertyNames.IndexOf(props[i].Name);
					if (ind >= 0)
					{
						PropertyNames.RemoveAt(ind);
						PropertyTypesStored.RemoveAt(ind);
						PropertyValues.RemoveAt(ind);
						newPropertyValues.RemoveAt(ind);
					}
				}
			}
		}
		#region Get Attribute Info
#nullable enable
		public static bool IsOrderImportant<T>() where T : PropertyAttribute =>
			typeof(T) == typeof(PIO.SpaceAttribute) ||
			typeof(T) == typeof(SpaceAttribute) ||
			typeof(T) == typeof(PIO.HeaderAttribute) ||
			typeof(T) == typeof(HeaderAttribute);
		public static bool IsOrderImportant(Type t) => 
			t == typeof(PIO.SpaceAttribute) ||
			t == typeof(SpaceAttribute) ||
			t == typeof(PIO.HeaderAttribute) ||
			t == typeof(HeaderAttribute);
		public static PropertyAttribute[] GetOrderImportantAttributes(PropertyAttribute[]? input) => input
			?.Where<PropertyAttribute>(v => IsOrderImportant(v.GetType()))
			?.ToArray<PropertyAttribute>() ?? new PropertyAttribute[0];
		public static PropertyAttribute[] GetOrderedAttributes(PropertyAttribute[]? input)
		{
			var output = GetOrderImportantAttributes(input);
			int[] orders = new int[output.Length];
			for (int i = 0; i < output.Length; i++)
				orders[i] = output[i].order;
			Array.Sort(orders, output);
			return output;
		}
		public PropertyAttribute[] GetOrderedAttributes(int index) => GetOrderedAttributes(PropertyNames[index]);
		public PropertyAttribute[] GetOrderedAttributes(string propertyName) => GetOrderedAttributes(GetUnityEngineAttributes(propertyName));
		#region Shorthand
		public string? GetTooltip(int index) => GetTooltip(PropertyNames[index]);
		public string? GetTooltip(string propertyName)
		{
			PropertyInfo? p = containerType?.GetProperty(propertyName,
				BindingFlags.Public |
				BindingFlags.NonPublic |
				BindingFlags.Instance);
			var tooltip = p?.GetCustomAttribute<TooltipAttribute>();
			return tooltip?.tooltip;
		}
		public string GetInspectorName(int index) => GetInspectorName(PropertyNames[index]);
		public string GetInspectorName(string propertyName)
		{
			PropertyInfo? p = containerType?.GetProperty(propertyName,
				BindingFlags.Public |
				BindingFlags.NonPublic |
				BindingFlags.Instance);
			return p?.GetCustomAttribute<PIO.InspectorNameAttribute>()?.displayName ?? propertyName;
		}
		public string GetHeaderText(int index) => GetHeaderText(PropertyNames[index]);
		public string GetHeaderText(string propertyName)
		{
			PropertyInfo? p = containerType?.GetProperty(propertyName,
				BindingFlags.Public |
				BindingFlags.NonPublic |
				BindingFlags.Instance);
			return p?.GetCustomAttribute<PIO.HeaderAttribute>()?.header ?? "";
		}
		public HeaderAttribute[] GetAllHeaders()
		{
			PropertyInfo[]? p = containerType?.GetProperties(
				BindingFlags.Public |
				BindingFlags.NonPublic |
				BindingFlags.Instance);
			var output = new List<HeaderAttribute>();
			for (int i = 0; p != null && i < p?.Length; i++)
				foreach (var a in 
					p?[i]?.GetCustomAttributes<HeaderAttribute>()?.ToArray<HeaderAttribute>() ?? 
					new HeaderAttribute[0])
					output.Add(a);
			return output.ToArray<HeaderAttribute>();
		}
		public string[] GetAllHeaderTexts()
		{
			var output = new List<string>();
			foreach (var h in GetAllHeaders())
				output.Add(h.header);
			return output.ToArray<string>();
		}
		public HeaderAttribute[] GetHeaders(int index) => GetHeaders(PropertyNames[index]);
		public HeaderAttribute[] GetHeaders(string propertyName)
		{
			PropertyInfo? p = containerType?.GetProperty(propertyName,
				BindingFlags.Public |
				BindingFlags.NonPublic |
				BindingFlags.Instance);
			return 
				p?.GetCustomAttributes<HeaderAttribute>()?.ToArray<HeaderAttribute>() ?? 
				new HeaderAttribute[0];
		}
		public string[] GetHeaderTexts(int index) => GetHeaderTexts(PropertyNames[index]);
		public string[] GetHeaderTexts(string propertyName)
		{
			var output = new List<string>();
			foreach (var h in GetHeaders(propertyName))
				output.Add(h.header);
			return output.ToArray<string>();
		}
		public float GetSpace(int index) => GetSpace(PropertyNames[index]);
		public float GetSpace(string propertyName)
		{
			PropertyInfo? p = containerType?.GetProperty(propertyName,
				BindingFlags.Public |
				BindingFlags.NonPublic |
				BindingFlags.Instance);
			return p?.GetCustomAttribute<PIO.SpaceAttribute>()?.height ?? 0;
		}
		public int GetNumSpaces()
		{
			PropertyInfo[]? p = containerType?.GetProperties(
				BindingFlags.Public |
				BindingFlags.NonPublic |
				BindingFlags.Instance);
			int num = 0;
			for (int i = 0; p != null && i < p?.Length; i++)
				if (p?[i]?.GetCustomAttribute<PIO.SpaceAttribute>() != null)
					++num;
			return num;
		}
		public float GetTotalAddedSpaceFromSpaceAttributes()
		{
			PropertyInfo[]? p = containerType?.GetProperties(
				BindingFlags.Public |
				BindingFlags.NonPublic |
				BindingFlags.Instance);
			float num = 0;
			for (int i = 0; p != null && i < p?.Length; i++)
				//num += p?[i]?.GetCustomAttribute<PIO.SpaceAttribute>()?.height ?? 0;
				foreach (var a in p?[i]?.GetCustomAttributes<PIO.SpaceAttribute>() ?? new PIO.SpaceAttribute[0])
					num += a?.height ?? 0;
			return num;
		}
		#endregion
		#region Concrete
		// For find and replace, replace "UnityEngine"
		/*public UnityEngine? GetUnityEngineAttribute(int index) => GetUnityEngineAttribute(PropertyNames[index]);
		public UnityEngine? GetUnityEngineAttribute(string propertyName)
		{
			PropertyInfo? p = containerType?.GetProperty(propertyName,
				BindingFlags.Public |
				BindingFlags.NonPublic |
				BindingFlags.Instance);
			return p?.GetCustomAttribute<UnityEngine>();
		}
		public UnityEngine[]? GetUnityEngineAttributes(int index) => GetUnityEngineAttributes(PropertyNames[index]);
		public UnityEngine[]? GetUnityEngineAttributes(string propertyName)
		{
			PropertyInfo? p = containerType?.GetProperty(propertyName,
				BindingFlags.Public |
				BindingFlags.NonPublic |
				BindingFlags.Instance);
			return (UnityEngine[])(p?.GetCustomAttributes<UnityEngine>() ?? new UnityEngine[0]);
		}*/
		public PIO.SpaceAttribute? GetSpaceAttribute(int index) => GetSpaceAttribute(PropertyNames[index]);
		public PIO.SpaceAttribute? GetSpaceAttribute(string propertyName)
		{
			PropertyInfo? p = containerType?.GetProperty(propertyName,
				BindingFlags.Public |
				BindingFlags.NonPublic |
				BindingFlags.Instance);
			return p?.GetCustomAttribute<PIO.SpaceAttribute>();
		}
		public PIO.SpaceAttribute[]? GetSpaceAttributes(int index) => GetSpaceAttributes(PropertyNames[index]);
		public PIO.SpaceAttribute[]? GetSpaceAttributes(string propertyName)
		{
			PropertyInfo? p = containerType?.GetProperty(propertyName,
				BindingFlags.Public |
				BindingFlags.NonPublic |
				BindingFlags.Instance);
			return (PIO.SpaceAttribute[])(p?.GetCustomAttributes<PIO.SpaceAttribute>() ?? new PIO.SpaceAttribute[0]);
		}
		public TooltipAttribute? GetTooltipAttribute(int index) => GetTooltipAttribute(PropertyNames[index]);
		public TooltipAttribute? GetTooltipAttribute(string propertyName)
		{
			PropertyInfo? p = containerType?.GetProperty(propertyName,
				BindingFlags.Public |
				BindingFlags.NonPublic |
				BindingFlags.Instance);
			return p?.GetCustomAttribute<TooltipAttribute>();
		}
		public HideInInspector? GetHideInInspectorAttribute(int index) => GetHideInInspectorAttribute(PropertyNames[index]);
		public HideInInspector? GetHideInInspectorAttribute(string propertyName)
		{
			PropertyInfo? p = containerType?.GetProperty(propertyName,
				BindingFlags.Public |
				BindingFlags.NonPublic |
				BindingFlags.Instance);
			return p?.GetCustomAttribute<HideInInspector>();
		}
		public ImageEffectAfterScale? GetImageEffectAfterScaleAttribute(int index) => GetImageEffectAfterScaleAttribute(PropertyNames[index]);
		public ImageEffectAfterScale? GetImageEffectAfterScaleAttribute(string propertyName)
		{
			PropertyInfo? p = containerType?.GetProperty(propertyName,
				BindingFlags.Public |
				BindingFlags.NonPublic |
				BindingFlags.Instance);
			return p?.GetCustomAttribute<ImageEffectAfterScale>();
		}
		public ImageEffectAllowedInSceneView? GetImageEffectAllowedInSceneViewAttribute(int index) => GetImageEffectAllowedInSceneViewAttribute(PropertyNames[index]);
		public ImageEffectAllowedInSceneView? GetImageEffectAllowedInSceneViewAttribute(string propertyName)
		{
			PropertyInfo? p = containerType?.GetProperty(propertyName,
				BindingFlags.Public |
				BindingFlags.NonPublic |
				BindingFlags.Instance);
			return p?.GetCustomAttribute<ImageEffectAllowedInSceneView>();
		}
		#endregion
		public T? GetUnityEngineAttribute<T>(int index) where T : Attribute => GetUnityEngineAttribute<T>(PropertyNames[index]);
		public T? GetUnityEngineAttribute<T>(string propertyName) where T : Attribute
		{
			PropertyInfo? p = containerType?.GetProperty(propertyName,
				BindingFlags.Public |
				BindingFlags.NonPublic |
				BindingFlags.Instance);
			return p?.GetCustomAttribute<T>();
		}
		public T[]? GetUnityEngineAttributes<T>(int index) where T : Attribute => GetUnityEngineAttributes<T>(PropertyNames[index]);
		public T[]? GetUnityEngineAttributes<T>(string propertyName) where T : Attribute
		{
			PropertyInfo? p = containerType?.GetProperty(propertyName,
				BindingFlags.Public |
				BindingFlags.NonPublic |
				BindingFlags.Instance);
			return (T[])(p?.GetCustomAttributes<T>() ?? new T[0]);
		}
		public PropertyAttribute[] GetUnityEngineAttributes(int index) => GetUnityEngineAttributes<PropertyAttribute>(PropertyNames[index]) ?? new PropertyAttribute[0];
		public PropertyAttribute[] GetUnityEngineAttributes(string propertyName)
		{
			PropertyInfo? p = containerType?.GetProperty(propertyName,
				BindingFlags.Public |
				BindingFlags.NonPublic |
				BindingFlags.Instance);
			return (PropertyAttribute[])(p?.GetCustomAttributes<PropertyAttribute>() ?? new PropertyAttribute[0]);
		}
#nullable restore
		#endregion

		#region Helpers
		#region IsLoneValueType
		public static bool IsLoneValueType<T>(T val) => val switch
		{
			byte	=> true,
			sbyte	=> true,
			short	=> true,
			ushort	=> true,
			int		=> true,
			uint	=> true,
			long	=> true,
			ulong	=> true,
			float	=> true,
			double	=> true,
			bool	=> true,
			string	=> true,
			_		=> false,
		};
		public static bool IsLoneValueType(Type val) => val switch
		{
			Type _byte	 when _byte		== typeof(byte	) => true,
			Type _sbyte	 when _sbyte	== typeof(sbyte	) => true,
			Type _short	 when _short	== typeof(short	) => true,
			Type _ushort when _ushort	== typeof(ushort) => true,
			Type _int	 when _int		== typeof(int	) => true,
			Type _uint	 when _uint		== typeof(uint	) => true,
			Type _long	 when _long		== typeof(long	) => true,
			Type _ulong	 when _ulong	== typeof(ulong	) => true,
			Type _float	 when _float	== typeof(float	) => true,
			Type _double when _double	== typeof(double) => true,
			Type _bool	 when _bool		== typeof(bool	) => true,
			Type _string when _string	== typeof(string) => true,
			_											  => false,
		};
		#endregion
		#region Stringify
		public static string Stringify<T>(T value) where T : 
			/*struct, */IComparable, IComparable<T>, IConvertible, IEquatable<T>/*, IFormattable*/ => value.ToString();
		public static string Stringify(object value, Type t) => t switch
		{
			Type _byte	 when _byte		== typeof(byte	) => Stringify<byte>((byte)value),
			Type _sbyte	 when _sbyte	== typeof(sbyte	) => Stringify<sbyte>((sbyte)value),
			Type _short	 when _short	== typeof(short	) => Stringify<short>((short)value),
			Type _ushort when _ushort	== typeof(ushort) => Stringify<ushort>((ushort)value),
			Type _int	 when _int		== typeof(int	) => Stringify<int>((int)value),
			Type _uint	 when _uint		== typeof(uint	) => Stringify<uint>((uint)value),
			Type _long	 when _long		== typeof(long	) => Stringify<long>((long)value),
			Type _ulong	 when _ulong	== typeof(ulong	) => Stringify<ulong>((ulong)value),
			Type _float	 when _float	== typeof(float	) => Stringify<float>((float)value),
			Type _double when _double	== typeof(double) => Stringify<double>((double)value),
			Type _bool	 when _bool		== typeof(bool	) => Stringify<bool>((bool)value),
			Type _string when _string	== typeof(string) => Stringify<string>((string)value),
			_ => JsonUtility.ToJson(Convert.ChangeType(value, t)),
		};
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
		public static string Stringify(string value) => value/*.ToString()*/;
		#endregion
		#region Parsers
		#region TryParse
#nullable enable
		public static object? TryParse(string value, Type t)
		{
			switch (t)
			{
				case Type t_byte 	when t_byte		== typeof(byte):	return TryParse(value, out byte o_byte)		? o_byte	: null;
				case Type t_sbyte 	when t_sbyte	== typeof(sbyte):	return TryParse(value, out sbyte o_sbyte)	? o_sbyte	: null;
				case Type t_short 	when t_short	== typeof(short):	return TryParse(value, out short o_short)	? o_short	: null;
				case Type t_ushort 	when t_ushort	== typeof(ushort):	return TryParse(value, out ushort o_ushort)	? o_ushort	: null;
				case Type t_int 	when t_int 		== typeof(int):		return TryParse(value, out int o_int)		? o_int		: null;
				case Type t_uint 	when t_uint		== typeof(uint):	return TryParse(value, out uint o_uint)		? o_uint	: null;
				case Type t_long 	when t_long		== typeof(long):	return TryParse(value, out long o_long)		? o_long	: null;
				case Type t_ulong 	when t_ulong	== typeof(ulong):	return TryParse(value, out ulong o_ulong)	? o_ulong	: null;
				case Type t_float 	when t_float	== typeof(float):	return TryParse(value, out float o_float)	? o_float	: null;
				case Type t_double 	when t_double	== typeof(double):	return TryParse(value, out double o_double)	? o_double	: null;
				case Type t_bool 	when t_bool		== typeof(bool):	return TryParse(value, out bool o_bool)		? o_bool	: null;
				case Type t_string 	when t_string	== typeof(string):	return TryParse(value, out string o_string)	? o_string	: null;
				default: return JsonUtility.FromJson(value, t);
			}
		}
		public static bool TryParse(string value, Type t, out object? output)
		{
			bool successful;
			switch (t)
			{
				case Type t_byte	when t_byte		== typeof(byte	): successful = TryParse(value, out byte o_byte		); output = successful ? o_byte		: null; break;
				case Type t_sbyte	when t_sbyte	== typeof(sbyte	): successful = TryParse(value, out sbyte o_sbyte	); output = successful ? o_sbyte	: null; break;
				case Type t_short	when t_short	== typeof(short	): successful = TryParse(value, out short o_short	); output = successful ? o_short	: null; break;
				case Type t_ushort	when t_ushort	== typeof(ushort): successful = TryParse(value, out ushort o_ushort	); output = successful ? o_ushort	: null; break;
				case Type t_int		when t_int 		== typeof(int	): successful = TryParse(value, out int o_int		); output = successful ? o_int		: null; break;
				case Type t_uint	when t_uint		== typeof(uint	): successful = TryParse(value, out uint o_uint		); output = successful ? o_uint		: null; break;
				case Type t_long	when t_long		== typeof(long	): successful = TryParse(value, out long o_long		); output = successful ? o_long		: null; break;
				case Type t_ulong	when t_ulong	== typeof(ulong	): successful = TryParse(value, out ulong o_ulong	); output = successful ? o_ulong	: null; break;
				case Type t_float	when t_float	== typeof(float	): successful = TryParse(value, out float o_float	); output = successful ? o_float	: null; break;
				case Type t_double	when t_double	== typeof(double): successful = TryParse(value, out double o_double	); output = successful ? o_double	: null; break;
				case Type t_bool	when t_bool		== typeof(bool	): successful = TryParse(value, out bool o_bool		); output = successful ? o_bool		: null; break;
				case Type t_string	when t_string	== typeof(string): successful = TryParse(value, out string o_string	); output = successful ? o_string	: null; break;
				default: output = JsonUtility.FromJson(value, t); return output == null;
			}
			return successful;
		}
#nullable restore
		public static bool TryParse(string value, out byte output) => byte.TryParse(value, out output);
		public static bool TryParse(string value, out sbyte output) => sbyte.TryParse(value, out output);
		public static bool TryParse(string value, out short output) => short.TryParse(value, out output);
		public static bool TryParse(string value, out ushort output) => ushort.TryParse(value, out output);
		public static bool TryParse(string value, out int output) => int.TryParse(value, out output);
		public static bool TryParse(string value, out uint output) => uint.TryParse(value, out output);
		public static bool TryParse(string value, out long output) => long.TryParse(value, out output);
		public static bool TryParse(string value, out ulong output) => ulong.TryParse(value, out output);
		public static bool TryParse(string value, out float output) => float.TryParse(value, out output);
		public static bool TryParse(string value, out double output) => double.TryParse(value, out output);
		public static bool TryParse(string value, out bool output) => bool.TryParse(value, out output);
		public static bool TryParse(string value, out string output) { output = value; return string.IsNullOrEmpty(value); }
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
		#region Old - Couldn't handle null
		/*public static void Parse(string value, out byte output) => output = byte.Parse(value);
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
		public static void Parse(string value, out string output) => output = value;*/
		#endregion
		public static void Parse(string value, out byte output) => byte.TryParse(value, out output);
		public static void Parse(string value, out sbyte output) => sbyte.TryParse(value, out output);
		public static void Parse(string value, out short output) => short.TryParse(value, out output);
		public static void Parse(string value, out ushort output) => ushort.TryParse(value, out output);
		public static void Parse(string value, out int output) => int.TryParse(value, out output);
		public static void Parse(string value, out uint output) => uint.TryParse(value, out output);
		public static void Parse(string value, out long output) => long.TryParse(value, out output);
		public static void Parse(string value, out ulong output) => ulong.TryParse(value, out output);
		public static void Parse(string value, out float output) => float.TryParse(value, out output);
		public static void Parse(string value, out double output) => double.TryParse(value, out output);
		public static void Parse(string value, out bool output) => bool.TryParse(value, out output);
		public static void Parse(string value, out string output) => output = value;
		#endregion
		#endregion
		#endregion

	}
}
