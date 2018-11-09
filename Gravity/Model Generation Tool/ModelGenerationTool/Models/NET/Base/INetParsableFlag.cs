namespace ModelGenerationTool.Models.NET.Base
{
	internal interface INetParsableFlag
	{
		NetFlag ConvertToDotNet();
	}
}