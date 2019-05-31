using ModelGenerationTool.Models.File;
using ModelGenerationTool.Models.NET;
using System;
using System.Collections.Generic;
using System.Text;

namespace ModelGenerationTool.Factories.Internal
{
	internal class NetModelGenerationFactory
	{
		internal CSharpFile GenerateCSharpFileFromModel(NetModel netModel)
		{
			CSharpFile csFile;

			switch (netModel.Type)
			{
				case NetItemType.Class:
					csFile = CreateCSharpClassFile(netModel);
					break;

				case NetItemType.Enum:
					csFile = CreateCSharpEnumFile(netModel);
					break;

				case NetItemType.Interface:
					csFile = CreateCSharpInterfaceFile(netModel);
					break;

				case NetItemType.Struct:
					csFile = CreateCSharpStructFile(netModel);
					break;

				// TODO:
				default:
					throw new NotImplementedException();
			}

			return csFile;
		}

		private CSharpFile CreateCSharpClassFile(NetModel netModel)
		{
			StringBuilder classFileTemplate = new StringBuilder(CSharpTemplates.CSharpClass, short.MaxValue);

			classFileTemplate.Replace("%usings%", FormatUsings(netModel.Usings));
			classFileTemplate.Replace("%attributes%", FormatParamList(netModel.Attributes, "\t"));
			classFileTemplate.Replace("%inherits%", FormatAncestors(netModel.Inherits));
			classFileTemplate.Replace("%namespace%", netModel.Namespace);
			classFileTemplate.Replace("%access_modifier%", $"\t{netModel.AccessModifier}");
			classFileTemplate.Replace("%additional_keyword%", netModel.AdditionalKeyword);
			classFileTemplate.Replace("%name%", netModel.Name);
			classFileTemplate.Replace("%properties%", FormatProperties(netModel.Properties));

			return new CSharpFile(netModel.Name, classFileTemplate.ToString());
		}

		private CSharpFile CreateCSharpEnumFile(NetModel netModel)
		{
			StringBuilder enumFileTemplate = new StringBuilder(CSharpTemplates.CSharpEnum, short.MaxValue);

			enumFileTemplate.Replace("%usings%", FormatUsings(netModel.Usings));
			enumFileTemplate.Replace("%attributes%", FormatParamList(netModel.Attributes));
			enumFileTemplate.Replace("%namespace%", netModel.Namespace);
			enumFileTemplate.Replace("%access_modifier%", $"\t{netModel.AccessModifier}");
			enumFileTemplate.Replace("%name%", netModel.Name);
			enumFileTemplate.Replace("%flags%", FormatFlags(netModel.Flags));

			return new CSharpFile(netModel.Name, enumFileTemplate.ToString());
		}

		private CSharpFile CreateCSharpInterfaceFile(NetModel netModel)
		=> throw new NotImplementedException();

		private CSharpFile CreateCSharpStructFile(NetModel netModel)
		=> throw new NotImplementedException();


		#region Format Methods

		private string FormatUsings(List<string> usings)
		{
			StringBuilder paramBuilder;

			if (usings == null || usings.Count <= 0)
				return string.Empty;
			else
			{
				paramBuilder = new StringBuilder(short.MaxValue);

				for (int i = 0; i < usings.Count; i++)
				{
					if (i < usings.Count - 1)
						paramBuilder.AppendLine(FormatUsing(usings[i]));
					else
						paramBuilder.Append(FormatUsing(usings[i]));
				}

				return paramBuilder.ToString();
			}
		}

		private string FormatUsing(string _using)
		{
			if (string.IsNullOrEmpty(_using))
				return null;

			StringBuilder resultBuilder = new StringBuilder(_using);

			if (_using.LastIndexOf(';') != _using.Length - 1)
				resultBuilder.Append(";");

			if (_using.ToLower().IndexOf("using") == -1 && _using.IndexOf("=") == -1)
				resultBuilder.Insert(0, "using ");

			return resultBuilder.ToString();
		}

		private string FormatAncestors(List<string> ancestors)
		{
			StringBuilder paramBuilder;

			if (ancestors == null || ancestors.Count <= 0)
				return string.Empty;
			else
			{
				paramBuilder = new StringBuilder(short.MaxValue);

				for (int i = 0; i < ancestors.Count; i++)
				{
					if (i < ancestors.Count - 1)
						paramBuilder.AppendLine(FormatAncestor(ancestors[i], i));
					else
						paramBuilder.Append(FormatAncestor(ancestors[i], i));
				}

				return paramBuilder.ToString();
			}
		}

		private string FormatAncestor(string ancestor, int index)
		{
			string result = null;

			if (string.IsNullOrEmpty(ancestor))
				result = null;
			else if (index == 0)
				result = $" : {ancestor}";
			else
				result = $", {ancestor}";

			return result;
		}

		private string FormatProperties(List<NetProperty> properties)
		{
			StringBuilder propertiesBuilder;

			if (properties == null || properties.Count <= 0)
				return string.Empty;
			else
			{
				propertiesBuilder = new StringBuilder(short.MaxValue);

				foreach (var property in properties)
				{
					propertiesBuilder.AppendLine(FormatProperty(property));
				}

				return propertiesBuilder.ToString();
			}
		}

		private string FormatProperty(NetProperty netProperty)
		{
			StringBuilder propertyFileTemplate = new StringBuilder(CSharpTemplates.CSharpProperty, short.MaxValue);

			// Attributes
			propertyFileTemplate.Replace("%attributes%", FormatParamList(netProperty.Attributes, "\t\t"));

			// Access Modifier
			propertyFileTemplate.Replace("%access_modifier%", $"\t\t{netProperty.AccessModifier}");

			// Type
			propertyFileTemplate.Replace("%type%", netProperty.Type != null ? netProperty.Type.Name : netProperty.TypeName);

			// Name
			propertyFileTemplate.Replace("%name%", netProperty.Name);

			// Get & Set Modifiers
			if (string.IsNullOrEmpty(netProperty.GetAccessModifier) || string.IsNullOrEmpty(netProperty.SetAccessModifier))
			{
				propertyFileTemplate.Replace("%get_modifier%", "");
				propertyFileTemplate.Replace("%set_modifier%", "");
			}
			else
			{
				string getAccessModifier = netProperty.GetAccessModifier.ToLower().Trim() == "public" ? "" : netProperty.GetAccessModifier;
				string setAccessModifier = netProperty.SetAccessModifier.ToLower().Trim() == "public" ? "" : netProperty.SetAccessModifier;

				propertyFileTemplate.Replace("%get_modifier%", $"{'{'} {getAccessModifier} get; ");
				propertyFileTemplate.Replace("%set_modifier%", $"{setAccessModifier} set; {'}'}");
			}

			// Value
			propertyFileTemplate.Replace("%value%", string.IsNullOrEmpty(netProperty.Value) ? "" : $"= {netProperty.Value}");

			return propertyFileTemplate.ToString();
		}

		private string FormatFlags(List<NetFlag> flags)
		{
			StringBuilder flagsBuilder;

			if (flags == null || flags.Count <= 0)
				return string.Empty;
			else
			{
				flagsBuilder = new StringBuilder(short.MaxValue);

				foreach (var flag in flags)
				{
					flagsBuilder.AppendLine(FormatFlag(flag));
				}

				return flagsBuilder.ToString();
			}
		}

		private string FormatFlag(NetFlag flag)
		{
			StringBuilder flagFileTemplate = new StringBuilder(CSharpTemplates.CSharpEnumFlag, short.MaxValue);

			// Attributes
			flagFileTemplate.Replace("%attributes%", FormatParamList(flag.Attributes, "\t\t"));

			// Name
			flagFileTemplate.Replace("%name%", $"\t\t{flag.Name}");

			// Value
			flagFileTemplate.Replace("%value%", string.IsNullOrEmpty(flag.Value) ? "" : $"= {flag.Value},");

			return flagFileTemplate.ToString();
		}

		private string FormatParamList(List<string> paramList, string paramPrefix = "")
		{
			StringBuilder paramBuilder;

			if (paramList == null || paramList.Count <= 0)
				return string.Empty;
			else
			{
				paramBuilder = new StringBuilder(short.MaxValue);

				for (int i = 0; i < paramList.Count; i++)
				{
					if (i < paramList.Count - 1)
						paramBuilder.AppendLine($"{paramPrefix}{paramList[i]}");
					else
						paramBuilder.Append($"{paramPrefix}{paramList[i]}");
				}

				return paramBuilder.ToString();
			}
		}
		#endregion
	}
}