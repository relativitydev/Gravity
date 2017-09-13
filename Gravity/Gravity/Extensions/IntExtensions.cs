using System.Collections.Generic;
using System.Text;

namespace Gravity.Extensions
{
	public static class IntExtensions
	{
		public static string ToSeparatedString(this IEnumerable<int> listOfInts, char separator)
		{
			StringBuilder sb = new StringBuilder();
			if (listOfInts == null) return sb.ToString();

			foreach (int theInt in listOfInts)
			{
				sb.Append(theInt.ToString());
				sb.Append(separator);
			}

			return sb.ToString();
		}
	}
}