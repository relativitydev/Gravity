using System;

namespace ModelGenerationTool.Attributes
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
	internal class XmlNodeKeyAttribute : Attribute
	{
		internal XmlNodeKeyAttribute(string xmlNodeKey)
		{
			XmlNodeKey = xmlNodeKey;
		}

		internal XmlNodeKeyAttribute(string xmlNodeKey, string customData)
		{
			XmlNodeKey = xmlNodeKey;
			CustomData = customData;
		}

		internal string XmlNodeKey { get; set; }

		internal string CustomData { get; set; }
	}
}