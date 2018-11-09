namespace ModelGenerationTool.Models.NET.Base
{
	internal interface INetParsableProperty
	{
		NetProperty ConvertToDotNet();
	}
}