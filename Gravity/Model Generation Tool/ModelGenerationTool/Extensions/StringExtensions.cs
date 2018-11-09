using System.Text;

namespace ModelGenerationTool.Extensions
{
	internal static class StringExtensions
	{
		internal static string ToDotNetNameFormat(this string input)
		{
			StringBuilder resultBuilder = new StringBuilder(input.Length);
			char[] inputAsChars = input.ToCharArray();

			for (int i = 0; i < input.Length; i++)
			{
				if ((char.IsDigit(inputAsChars[i]) && resultBuilder.Length > 0)
					|| char.IsLetter(inputAsChars[i]))
				{
					resultBuilder.Append(inputAsChars[i]);
				}
			}

			return resultBuilder.ToString();
		}
	}
}
