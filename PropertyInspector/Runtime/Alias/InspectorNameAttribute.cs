using System;
using UnityEngine;

namespace JMor.Utility.Inspector.Alias
{
	[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
	public sealed class InspectorNameAttribute : UnityEngine.InspectorNameAttribute
	{
		/// <inheritdoc cref="UnityEngine.InspectorNameAttribute.displayName"/>
		public string DisplayName => displayName;

		public InspectorNameAttribute(string displayName) : base(displayName) { }
	}
	namespace Replacements
	{
		[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
		public sealed class InspectorNameAttribute : PropertyAttribute
		{
			/// <summary>Name to display in the Inspector.</summary>
			readonly string _displayName;
		
			/// <inheritdoc cref="_displayName"/>
			public string DisplayName => _displayName;
			/// <inheritdoc cref="DisplayName"/>
			public string displayName => _displayName;
		
			public InspectorNameAttribute(string displayName) => this._displayName = displayName;
		}
	}
}