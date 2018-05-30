using kCura.Relativity.Client.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gravity.Test.Helpers
{
	public static class ResultSetHelper
	{
		public static ResultSet<RDO> ToSuccessResultSet(this IEnumerable<RDO> rdos)
		{
			return ToSuccessResultSet<ResultSet<RDO>>(rdos);
		}

		public static T ToSuccessResultSet<T>(this IEnumerable<RDO> rdos) where T: ResultSet<RDO>, new()
		{
			return new T
			{
				Success = true,
				Results = rdos.Select(x => new Result<RDO> { Success = true, Artifact = x }).ToList()
			};
		}
	}
}
