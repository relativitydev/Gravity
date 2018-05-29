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
			return new ResultSet<RDO>
			{
				Success = true,
				Results = rdos.Select(x => new Result<RDO> { Success = true, Artifact = x }).ToList()
			};
		}
	}
}
