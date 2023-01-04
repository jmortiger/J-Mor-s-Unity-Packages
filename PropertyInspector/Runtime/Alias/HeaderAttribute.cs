using System;

namespace JMor.Utility.Inspector.Alias
{
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = true)]
	public sealed class HeaderAttribute : UnityEngine.HeaderAttribute
	{
		public HeaderAttribute(string header) : base(header) { }
		public string Header => header;
		public string Tooltip { get; set; }
		public string tooltip { get => Tooltip; set => Tooltip = value; }
	}
    namespace Replacements
	{
		[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = true)]
			public sealed class HeaderAttribute : UnityEngine.PropertyAttribute
			{
				readonly string _header;
				public HeaderAttribute(string _header) => this._header = _header;
				public string Header => _header;
				public string header => _header;
				public string Tooltip { get; set; }
				public string tooltip { get => Tooltip; set => Tooltip = value; }
			}
	}
}
