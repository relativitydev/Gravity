using System.Collections.Generic;

namespace ModelGenerationTool.Models.NET
{
	internal class NetFlag
	{
		/// <summary>
		/// Constructor for .NET enumeration flags.
		/// </summary>
		/// <param name="attributes">.NET enum flag attributes.</param>
		/// <param name="name">.NET enum flag name.</param>
		/// <param name="value">.NET enum flag value.</param>
		internal NetFlag(List<string> attributes, string name, string value)
		{
			Attributes = attributes;
			Name = name;
			Value = value;
		}

		internal List<string> Attributes { get; private set; }

		internal string Name { get; private set; }

		internal string Value { get; private set; }
	}
}