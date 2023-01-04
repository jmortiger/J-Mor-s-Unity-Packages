using System;
using UnityEngine;

namespace JMor.Utility.Inspector.Alias
{
	[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
	public sealed class SpaceAttribute : PropertyAttribute
	{
		/// <summary>
        /// 
        /// </summary>
        /// <remarks>I'm pretty confident 8 is the default value, and the EditorGUIUtility.standardVerticalSpacing is added to it to result in a total of 10 pixels of vertical space between elements. Checked the IMGUI debugger and all. Not documented anywhere, from what I can tell.</remarks>
		readonly float heightOfSpace = 8f;
		
		/// <summary>
        /// The number of pixels of vertical space to add above this member in the inspector.
        /// </summary>
        /// <inheritdoc cref="heightOfSpace"/>
		public float height => heightOfSpace;
		
		/// <summary>
        /// Add 8 pixels of vertical space above this member in the inspector. This is the same as <see cref="UnityEngine.SpaceAttribute.SpaceAttribute()"/>
        /// </summary>
        /// <inheritdoc cref="heightOfSpace"/>
		public SpaceAttribute() : this(8f) {}
		/// <summary>
        /// Add the specified number of pixels of vertical space above this member in the inspector. This is the same as <see cref="UnityEngine.SpaceAttribute.SpaceAttribute(float)"/>
        /// </summary>
		public SpaceAttribute(float height)
		{
			this.heightOfSpace = height;
		}
	}
}