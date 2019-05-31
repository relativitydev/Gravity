using System;

namespace ModelGenerationTool.Attributes
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	internal class XmlNodePathAttribute : Attribute
	{
		internal XmlNodePathAttribute(string xmlNodePath)
		{
			XmlNodePath = xmlNodePath;
		}

		internal XmlNodePathAttribute(string xmlNodePath, string customData)
		{
			XmlNodePath = xmlNodePath;
			CustomData = customData;
		}

		internal string XmlNodePath { get; set; }

		internal string CustomData { get; set; }
	}
}
