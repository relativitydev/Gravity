namespace ModelGenerationTool.Models.File
{
	public class CSharpFile
	{
		private string _name;
		private string _content;

		internal CSharpFile(string name, string content)
		{
			_name = name;
			_content = content;
		}

		public void Save(string location)
			=> System.IO.File.WriteAllText($"{location}\\{_name}.cs", _content);
	}
}