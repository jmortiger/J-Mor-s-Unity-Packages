using System;

namespace JMor.Utility.Inspector
{
	[AttributeUsage(AttributeTargets.Property)]
	public class PropertyInspectorAttribute : Attribute
	{
		public PropertyInspectorAttribute() {}
	}
}