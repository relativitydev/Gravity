using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModelGenerationTool.Factories.External;

namespace ModelGeneration.Tests
{
	[TestClass]
	public class GravityModelGenerationTests
	{
		GravityModelGenerationFactory _gravityModelGenFactory;

		string _rapXmlSchemaLocation = "";
		string _modelsOutputPath = "";

		[TestInitialize]
		public void Initialize()
		{
			_gravityModelGenFactory = new GravityModelGenerationFactory(_rapXmlSchemaLocation);
		}

		[TestMethod]
		public void GenerateGravityModels()
		{
			_gravityModelGenFactory.GenerateGravityModels(_modelsOutputPath);
		}
	}
}
