using ModelGenerationTool.Attributes;
using ModelGenerationTool.Models.NET;
using ModelGenerationTool.Models.NET.Base;
using System.Collections.Generic;

namespace ModelGenerationTool.Models.GravityRdo
{
	[XmlNodePath("Codes/Code")]
	internal class GravityChoice : INetParsableFlag
	{
		int _value;

		internal GravityChoice(string name, string guid, int value)
		{
			Name = name;
			Guid = guid;
			_value = value;
		}

		[XmlNodeKey("Name")]
		internal string Name { get; set; }

		[XmlNodeKey("Guid")]
		internal string Guid { get; set; }

		public NetFlag ConvertToDotNet()
		{
			List<string> attributes = new List<string>()
			{
				$"[RelativityObject(\"{Guid}\")]"
			};

			return new NetFlag(attributes, Name, _value.ToString());
		}
	}
}