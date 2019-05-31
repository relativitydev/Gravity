using Gravity.Base;
using ModelGenerationTool.Attributes;
using ModelGenerationTool.Factories.Base;
using ModelGenerationTool.Models.GravityRdo;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;

namespace ModelGenerationTool.Factories.Internal
{
	internal class GravityRdoFactory : XmlFactoryBase
	{
		internal GravityRdoFactory(XmlDocument xmlDocument)
			: base(xmlDocument)
		{ }

		internal GravityRdoFactory(string xmlDocumentLocation)
			: base(new XmlDocument())
		{
			xmlDocument.Load(xmlDocumentLocation);
		}

		internal List<GravityModel> GenerateRdoModelsForXml()
		{
			List<GravityModel> resultList = new List<GravityModel>();

			string objectsXpath = typeof(GravityModel).GetCustomAttribute<XmlNodePathAttribute>().XmlNodePath;

			XmlNodeList rdoObjects = xmlDocument.SelectNodes(objectsXpath);

			foreach (XmlNode rdoObject in rdoObjects)
			{
				resultList.Add(new GravityModel(rdoObject));
			}

			PopulateObjectFields(resultList);
			PopulateChildrenFields(resultList);

			return resultList;
		}

		private void PopulateObjectFields(List<GravityModel> models)
		{
			foreach (var model in models)
			{
				var objectFields = model.GravityFields
					.Where(f => f.RdoFieldType == RdoFieldType.SingleObject || f.RdoFieldType == RdoFieldType.MultipleObject);

				foreach (var objField in objectFields)
				{
					string descriptorObjName = models.FirstOrDefault(m => m.DescriptorArtifactTypeId == objField.AssociativeArtifactTypeId.Value)?.Name;
					var associatedObjs = model.GravityFields.Where(f => f.AssociativeArtifactTypeId == objField.AssociativeArtifactTypeId.Value).ToList();

					foreach (var associatedObj in associatedObjs)
					{
						associatedObj.Name = descriptorObjName ?? associatedObj.Name;
					}
				}
			}
		}

		private void PopulateChildrenFields(List<GravityModel> models)
		{
			foreach (var child in models)
			{
				var parent = models.FirstOrDefault(m => m.DescriptorArtifactTypeId == child.ParentArtifactTypeId);

				if (child.ParentArtifactTypeId.HasValue && parent != null)
				{
					parent.GravityFields.Add(new GravityFieldModel(child.Name, null, null));
				}
			}
		}
	}
}