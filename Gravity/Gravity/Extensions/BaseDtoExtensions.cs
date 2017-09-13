using System.Collections.Generic;
using System.Text;
using Gravity.Base;

namespace Gravity.Extensions
{
	public static class BaseDtoExtensions
	{
		public static string ToSeparatedString(this IEnumerable<BaseDto> listOfDtos, char separator)
		{
			StringBuilder sb = new StringBuilder();

			if (listOfDtos != null)
			{
				foreach (BaseDto theDto in listOfDtos)
				{
					sb.Append(theDto.Name);
					sb.Append(separator);
				}
			}

			return sb.ToString();
		}
	}
}
