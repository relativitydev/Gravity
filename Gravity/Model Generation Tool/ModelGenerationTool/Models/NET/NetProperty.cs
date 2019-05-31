using System;
using System.Collections.Generic;

namespace ModelGenerationTool.Models.NET
{
	internal class NetProperty
	{
		/// <summary>
		/// Constructor for .NET class properties.
		/// </summary>
		/// <param name="attributes">.NET class property attributes.</param>
		/// <param name="name">.NET class property name.</param>
		/// <param name="type">.NET class property type.</param>
		/// <param name="additionalKeyword">.NET class property additional keyword. For example - "abstract", "static", etc.</param>
		/// <param name="accessModifier">.NET class property access modifier. For example - "public".</param>
		/// <param name="getModifier">.NET class property get access modifier. For example - "private".</param>
		/// <param name="setModifier">.NET class property set access modifier. For example - "private".</param>
		/// <param name="value">.NET class property value. For example - "new List<string>()".</param>
		internal NetProperty(List<string> attributes, string name, Type type,
			string additionalKeyword = null,
			string accessModifier = "public",
			string getModifier = "public",
			string setModifier = "public",
			string value = null)
			: this(attributes, name, additionalKeyword, accessModifier, getModifier, setModifier, value)
		{
			Type = type;
		}

		/// <summary>
		/// Constructor for .NET class properties.
		/// </summary>
		/// <param name="attributes">.NET class property attributes.</param>
		/// <param name="name">.NET class property name.</param>
		/// <param name="typeName">.NET class property type name. For example "MyCustomClass".</param>
		/// <param name="additionalKeyword">.NET class property additional keyword. For example - "abstract", "static", etc.</param>
		/// <param name="accessModifier">.NET class property access modifier. For example - "public".</param>
		/// <param name="getModifier">.NET class property get access modifier. For example - "private".</param>
		/// <param name="setModifier">.NET class property set access modifier. For example - "private".</param>
		/// <param name="value">.NET class property value. For example - "new List<string>()".</param>
		internal NetProperty(List<string> attributes, string name, string typeName,
			string additionalKeyword = null,
			string accessModifier = "public",
			string getModifier = "public",
			string setModifier = "public",
			string value = null)
			: this(attributes, name, additionalKeyword, accessModifier, getModifier, setModifier, value)
		{
			TypeName = typeName;
		}

		private NetProperty(List<string> attributes, string name,
			string additionalKeyword = null,
			string accessModifier = "public",
			string getModifier = "public",
			string setModifier = "public",
			string value = null)
		{
			Attributes = attributes;
			Name = name;

			AdditionalKeyword = additionalKeyword;
			AccessModifier = accessModifier;
			GetAccessModifier = getModifier;
			SetAccessModifier = setModifier;
			Value = value;
		}

		internal List<string> Attributes { get; private set; }

		internal string Name { get; private set; }

		internal Type Type { get; private set; }

		internal string TypeName { get; private set; }

		internal string AdditionalKeyword { get; private set; }

		internal string AccessModifier { get; private set; }

		internal string GetAccessModifier { get; private set; }

		internal string SetAccessModifier { get; private set; }

		internal string Value { get; private set; }
	}
}