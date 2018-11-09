namespace ModelGenerationTool.Models.NET.Base
{
	internal interface INetParsableClass
	{
		NetModel ConvertToDotNet();
	}
}