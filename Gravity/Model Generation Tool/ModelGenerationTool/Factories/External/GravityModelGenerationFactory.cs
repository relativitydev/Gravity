using ModelGenerationTool.Factories.Base;
using ModelGenerationTool.Factories.Internal;
using ModelGenerationTool.Models.File;
using ModelGenerationTool.Models.GravityRdo;
using ModelGenerationTool.Models.NET;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace ModelGenerationTool.Factories.External
{
	public class GravityModelGenerationFactory : XmlFactoryBase
	{
		public GravityModelGenerationFactory(XmlDocument xmlDocument)
			: base(xmlDocument) { }

		public GravityModelGenerationFactory(string xmlDocumentLocation)
			: base(new XmlDocument()) => xmlDocument.Load(xmlDocumentLocation);

		public void GenerateGravityModels(string outputPath)
		{
			// 1. Load xmlDocument by Path
			GravityRdoFactory rdoFactory = new GravityRdoFactory(xmlDocument);

			// 2. Read in Platform specific Models
			var rdoModels = rdoFactory.GenerateRdoModelsForXml();

			// 3. ConvertToDotNet with NetProperties
			var rdoModelsAsDotNet = ConvertRdoModelsToDotNet(rdoModels);

			NetModelGenerationFactory netFactory = new NetModelGenerationFactory();

			foreach (var netModel in rdoModelsAsDotNet)
			{
				// 4. Convert NetModel with NetProperties to CSharpFile
				var csFile = netFactory.GenerateCSharpFileFromModel(netModel);

				// 5. Store file in specified output path
				csFile.Save(outputPath);
			}
		}

		public List<CSharpFile> GenerateGravityModelFiles()
		{
			List<CSharpFile> resultList = new List<CSharpFile>();

			// 1. Load xmlDocument by Path
			GravityRdoFactory rdoFactory = new GravityRdoFactory(xmlDocument);

			// 2. Read in Platform specific Models
			var rdoModels = rdoFactory.GenerateRdoModelsForXml();

			// 3. ConvertToDotNet with NetProperties
			var rdoModelsAsDotNet = ConvertRdoModelsToDotNet(rdoModels);

			NetModelGenerationFactory netFactory = new NetModelGenerationFactory();

			foreach (var netModel in rdoModelsAsDotNet)
			{
				// 4. Convert NetModel with NetProperties to CSharpFile
				resultList.Add(netFactory.GenerateCSharpFileFromModel(netModel));
			}

			return resultList;
		}

		// TODO: This should be moved out from factory..
		private static List<NetModel> ConvertRdoModelsToDotNet(List<GravityModel> rdoModels)
		{
			List<NetModel> returnList = new List<NetModel>();

			foreach (var rdoModel in rdoModels)
			{
				foreach (var choice in rdoModel.GravityChoices)
				{
					if (returnList.Select(x => x.Name).Contains(choice.Name) == false)
						returnList.Add(choice.ConvertToDotNet());
				}

				returnList.Add(rdoModel.ConvertToDotNet());
			}

			return returnList;
		}
	}
}