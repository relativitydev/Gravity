using kCura.Relativity.Client.DTOs;
using kCura.Relativity.Client.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gravity.Extensions
{
    public static class ResultSetExtensions
    {
		public static List<T> GetResultData<T>(this ResultSet<T> results) where T: Artifact
		{
			if (!results.Success)
				throw new InvalidOperationException("Query failure: " + results.Message);

			return results.Results.Select(x =>
				x.Success ? x.Artifact : throw new InvalidOperationException("Query item failure: " + x.Message)
			).ToList();
		}
	}
}
