using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gravity.DAL.RSAPI;
using Gravity.Test.Helpers;
using kCura.Relativity.Client.DTOs;
using Moq.Language;
using Moq.Language.Flow;

namespace Gravity.DAL.RSAPI.Tests
{
	public static class MockHelpers
	{
		#region ResultSet stuff

		private static T ToSuccessResultSet<T>(this IEnumerable<RDO> rdos) where T : ResultSet<RDO>, new()
		{
			return new T {
				Success = true,
				Results = rdos.Select(x => new Result<RDO> { Success = true, Artifact = x }).ToList()
			};
		}

		public static IReturnsResult<IRsapiProvider> ReturnsResultSet<T>
			(this ISetup<IRsapiProvider, T> setup, IEnumerable<RDO> rdos)
			where T : ResultSet<RDO>, new()
		{
			return setup.Returns(rdos.ToSuccessResultSet<T>());
		}

		public static IReturnsResult<IRsapiProvider> ReturnsResultSet<T>
			(this ISetup<IRsapiProvider, T> setup, params RDO[] rdos)
			where T : ResultSet<RDO>, new()
		{
			return ReturnsResultSet(setup, (IEnumerable<RDO>)rdos);
		}

		public static IReturnsResult<IRsapiProvider> ReturnsResultSet
			(this ISetup<IRsapiProvider, IEnumerable<QueryResultSet<RDO>>> setup, IEnumerable<RDO> rdos)
		{
			return setup.Returns(new[] { rdos.ToSuccessResultSet<QueryResultSet<RDO>>() } );
		}

		public static IReturnsResult<IRsapiProvider> ReturnsResultSet
			(this ISetup<IRsapiProvider, IEnumerable<QueryResultSet<RDO>>> setup, params RDO[] rdos)
		{
			return ReturnsResultSet(setup, (IEnumerable<RDO>)rdos);
		}

		public static ISetupSequentialResult<T> ReturnsResultSet<T>
			(this ISetupSequentialResult<T> setup, IEnumerable<RDO> rdos)
			where T : ResultSet<RDO>, new()
		{
			return setup.Returns(rdos.ToSuccessResultSet<T>());
		}

		public static ISetupSequentialResult<IEnumerable<QueryResultSet<RDO>>> ReturnsResultSet
			(this ISetupSequentialResult<IEnumerable<QueryResultSet<RDO>>> setup, IEnumerable<RDO> rdos)
		{
			return setup.Returns(new[] { rdos.ToSuccessResultSet<QueryResultSet<RDO>>() });
		}

		#endregion

		public static bool IsEquivalent<T>(this IEnumerable<T> source, IEnumerable<T> other)
		{
			return new HashSet<T>(source).SetEquals(other);
		}
	}
}
