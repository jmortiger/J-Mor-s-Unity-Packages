using System;
using UnityEngine;

namespace JMor.Utility.Inspector.Alias
{
	[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = true)]
	public sealed class SpaceAttribute : UnityEngine.SpaceAttribute
	{
		#region Fields & Properties
		/// <summary>
        /// The number of pixels of vertical space to add above this member in the inspector.
        /// </summary>
        /// <remarks>I'm pretty confident 8 is the default value, and the EditorGUIUtility.standardVerticalSpacing is added to it to result in a total of 10 pixels of vertical space between elements. Checked the IMGUI debugger and all. Not documented anywhere, from what I can tell.</remarks>
		public float Height => height;
		#endregion
		
		#region Ctors
		/// <summary>
        /// Add 8 pixels of vertical space above this member in the inspector. This is the same as <see cref="UnityEngine.SpaceAttribute.SpaceAttribute()"/>
        /// </summary>
        /// <inheritdoc cref="Height"/>
		public SpaceAttribute() : base(/*8f*/) {}

		/// <summary>
		/// Add the specified number of pixels of vertical space above this member in the inspector. This is the same as <see cref="UnityEngine.SpaceAttribute.SpaceAttribute(float)"/>
		/// </summary>
		public SpaceAttribute(float height) : base(height) { }
		#endregion
	}
	namespace Replacements
	{
		[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = true)]
		public sealed class SpaceAttribute : PropertyAttribute
		{
			#region Fields & Properties
			/// <summary>
	        /// The number of pixels of vertical space to add above this member in the inspector.
	        /// </summary>
	        /// <remarks>I'm pretty confident 8 is the default value, and the EditorGUIUtility.standardVerticalSpacing is added to it to result in a total of 10 pixels of vertical space between elements. Checked the IMGUI debugger and all. Not documented anywhere, from what I can tell.</remarks>
			readonly float _height = 8f;
			
	        /// <inheritdoc cref="_height"/>
			public float Height => _height;
	        /// <inheritdoc cref="Height"/>
			public float height => _height;
			#endregion
			
			#region Ctors
			/// <summary>
	        /// Add 8 pixels of vertical space above this member in the inspector. This is the same as <see cref="UnityEngine.SpaceAttribute.SpaceAttribute()"/>
	        /// </summary>
	        /// <inheritdoc cref="_height"/>
			public SpaceAttribute() : this(8f) {}
	
			/// <summary>
			/// Add the specified number of pixels of vertical space above this member in the inspector. This is the same as <see cref="UnityEngine.SpaceAttribute.SpaceAttribute(float)"/>
			/// </summary>
			public SpaceAttribute(float height) => this._height = height;
			#endregion
		}
	}
}