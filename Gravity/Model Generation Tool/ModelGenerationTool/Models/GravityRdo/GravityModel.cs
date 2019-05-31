using Gravity.Base;
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
	[XmlNodePath("/Application/Objects/Object")]
	internal class GravityModel : INetParsableClass
	{
		internal GravityModel(XmlNode rdoObjectNode)
		{
			string nameKey = typeof(GravityModel).GetPropertyAttribute<XmlNodeKeyAttribute>(nameof(Name)).XmlNodeKey;
			string guidKey = typeof(GravityModel).GetPropertyAttribute<XmlNodeKeyAttribute>(nameof(Guid)).XmlNodeKey;
			string descriptorObjIdKey = typeof(GravityModel).GetPropertyAttribute<XmlNodeKeyAttribute>(nameof(DescriptorArtifactTypeId)).XmlNodeKey;
			string parentObjIdKey = typeof(GravityModel).GetPropertyAttribute<XmlNodeKeyAttribute>(nameof(ParentArtifactTypeId)).XmlNodeKey;

			string fieldModelPath = typeof(GravityFieldModel).GetCustomAttributes<XmlNodePathAttribute>().FirstOrDefault(a => a.CustomData.Equals("Default"))?.XmlNodePath;
			string sysFieldModelPath = typeof(GravityFieldModel).GetCustomAttributes<XmlNodePathAttribute>().FirstOrDefault(a => a.CustomData.Equals("System"))?.XmlNodePath;

			Name = rdoObjectNode[nameKey].InnerText.ToDotNetNameFormat();
			Guid = rdoObjectNode[guidKey].InnerText;
			GravityFields = new List<GravityFieldModel>();
			GravityChoices = new List<GravityChoiceModel>();

			int.TryParse(rdoObjectNode[descriptorObjIdKey].InnerText, out int descriptorObjId);
			DescriptorArtifactTypeId = descriptorObjId > 0 ? descriptorObjId : (int?)null;

			int.TryParse(rdoObjectNode[parentObjIdKey].InnerText, out int parentObjId);
			ParentArtifactTypeId = parentObjId > 0 ? parentObjId : (int?)null;

			XmlNodeList fieldsForObject = rdoObjectNode.SelectNodes(fieldModelPath);
			XmlNodeList sysFieldsForObject = rdoObjectNode.SelectNodes(sysFieldModelPath);

			GravityFieldModel fieldModel;

			foreach (XmlNode field in fieldsForObject)
			{
				fieldModel = new GravityFieldModel(field);
				GravityFields.Add(fieldModel);

				if (fieldModel.RdoFieldType == RdoFieldType.SingleChoice
					|| fieldModel.RdoFieldType == RdoFieldType.MultipleChoice)
				{
					GravityChoices.Add(new GravityChoiceModel(field));
				}
			}

			foreach (XmlNode sysField in sysFieldsForObject)
			{
				fieldModel = new GravityFieldModel(sysField, true);

				if (fieldModel.AssociativeArtifactTypeId.HasValue
					|| Constants.AllowedRelativitySystemFields.Contains(fieldModel.Name))
				{
					GravityFields.Add(fieldModel);
				}
			}
		}

		[XmlNodeKey("Name")]
		internal string Name { get; private set; }

		[XmlNodeKey("Guid")]
		internal string Guid { get; private set; }

		[XmlNodeKey("ParentArtifactTypeId")]
		internal int? ParentArtifactTypeId { get; set; }

		[XmlNodeKey("DescriptorArtifactTypeId")]
		internal int? DescriptorArtifactTypeId { get; set; }

		internal List<GravityFieldModel> GravityFields { get; set; }

		internal List<GravityChoiceModel> GravityChoices { get; set; }

		public NetModel ConvertToDotNet()
		{
			List<string> attributes = new List<string>()
			{
				// Set Gravity Attributes..
				$"[Serializable()]",
				$"[RelativityObject(\"{Guid}\")]"
			};

			List<NetProperty> properties = new List<NetProperty>();
			properties.AddRange(GravityFields.OrderByDescending(f => f.IsSystemField).Select(f => f.ConvertToDotNet()));

			return new NetModel("ModelGenerationTool.Test", attributes, Name, properties, "public", null, new List<string>() { "System", "Gravity.Base", "System.Collections.Generic" }, new List<string>() { "BaseDto" });
		}
	}
}