using ModelGenerationTool.Attributes;
using ModelGenerationTool.Extensions;
using ModelGenerationTool.Models.NET;
using ModelGenerationTool.Models.NET.Base;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;

namespace ModelGenerationTool.Models.GravityRdo
{
	internal class GravityChoiceModel : INetParsableClass
	{
		internal GravityChoiceModel(XmlNode choiceFieldNode)
		{
			string choicesPath = typeof(GravityChoice).GetCustomAttribute<XmlNodePathAttribute>().XmlNodePath;
			string nameKey = typeof(GravityChoiceModel).GetPropertyAttribute<XmlNodeKeyAttribute>(nameof(Name)).XmlNodeKey;

			string choiceNameKey = typeof(GravityChoice).GetPropertyAttribute<XmlNodeKeyAttribute>(nameof(GravityChoice.Name)).XmlNodeKey;
			string choiceGuidKey = typeof(GravityChoice).GetPropertyAttribute<XmlNodeKeyAttribute>(nameof(GravityChoice.Guid)).XmlNodeKey;

			Name = choiceFieldNode[nameKey].InnerText.ToDotNetNameFormat();
			Choices = new List<GravityChoice>();

			XmlNodeList choicesList = choiceFieldNode.SelectNodes(choicesPath);
			int counter = 0;

			foreach (XmlNode choiceNode in choicesList)
			{
				string name = choiceNode[choiceNameKey].InnerText.ToDotNetNameFormat();
				string guid = choiceNode[choiceGuidKey].InnerText;

				Choices.Add(new GravityChoice(name, guid, ++counter));
			}
		}

		[XmlNodeKey("DisplayName")]
		internal string Name { get; set; }

		internal List<GravityChoice> Choices { get; set; }

		public NetModel ConvertToDotNet()
		{
			List<NetFlag> flags = new List<NetFlag>();
			flags.AddRange(Choices.Select(c => c.ConvertToDotNet()));

			return new NetModel("ModelGenerationTool.Test", null, Name, flags, "public");
		}
	}
}