using System.Collections.Generic;

namespace ModelGenerationTool.Models.NET
{
	internal class NetModel
	{
		/// <summary>
		/// Constructor for .NET class models.
		/// </summary>
		/// <param name="_namespace">.NET class namespace.</param>
		/// <param name="attributes">.NET class attributes.</param>
		/// <param name="name">.NET class name.</param>
		/// <param name="properties">.NET class properties.</param>
		/// <param name="additionalKeyword">.NET class additional keyword. For example - "abstract", "static", etc.</param>
		/// <param name="accessModifier">.NET class access modifier. For example "public".</param>
		/// <param name="usings">.NET class usings. For example {"System", "System.Collections.Generic"}</param>
		/// <param name="inherits">.NET class parents. For example {"BaseClass", "IMyInterface"}</param>
		internal NetModel(string _namespace, List<string> attributes, string name, List<NetProperty> properties,
			string additionalKeyword = null,
			string accessModifier = null,
			List<string> usings = null,
			List<string> inherits = null)
			: this(_namespace, attributes, name, additionalKeyword, accessModifier, usings, inherits)
		{
			Type = NetItemType.Class;
			Properties = properties;
		}

		/// <summary>
		/// Constructor for .NET enumeration models.
		/// </summary>
		/// <param name="_namespace">.NET enumeration namespace.</param>
		/// <param name="attributes">.NET enumeration attributes.</param>
		/// <param name="name">.NET enumeration name.</param>
		/// <param name="flags">.NET enumeration flags.</param>
		/// <param name="accessModifier">.NET enumeration access modifier. For example "public".</param>
		/// <param name="usings">.NET enumeration usings. For example {"System", "System.Collections.Generic"}</param>
		internal NetModel(string _namespace, List<string> attributes, string name, List<NetFlag> flags,
			string accessModifier = null,
			List<string> usings = null)
			: this(_namespace, attributes, name, null, accessModifier, usings, null)
		{
			Type = NetItemType.Enum;
			Flags = flags;
		}

		private NetModel(string _namespace, List<string> attributes, string name,
			string additionalKeyword = null,
			string accessModifier = null,
			List<string> usings = null,
			List<string> inherits = null)
		{
			Namespace = _namespace;
			Attributes = attributes;
			Name = name;

			AdditionalKeyword = additionalKeyword;
			AccessModifier = accessModifier;
			Usings = usings;
			Inherits = inherits;
		}

		internal string Namespace { get; private set; }

		internal List<string> Attributes { get; private set; }

		internal NetItemType Type { get; private set; }

		internal string Name { get; private set; }

		internal List<NetProperty> Properties { get; private set; }

		internal List<NetFlag> Flags { get; private set; }

		internal string AdditionalKeyword { get; private set; }

		internal List<string> Usings { get; private set; }

		internal List<string> Inherits { get; private set; }

		internal string AccessModifier { get; private set; }
	}
}