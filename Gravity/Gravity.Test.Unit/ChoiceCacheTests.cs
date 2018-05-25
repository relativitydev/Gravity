using NUnit.Framework;
using Gravity.DAL.RSAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gravity.DAL.RSAPI.Tests
{
	[TestFixture]
	public class ChoiceCacheTests
	{
		[Test] public void GetEnum_TypeIsNotEnum() => throw new NotImplementedException();
		[Test] public void GetEnum_NoValueFound() => throw new NotImplementedException();
		[Test] public void GetEnum_OnlyFetchesFromServerOnce() => throw new NotImplementedException();
		[Test] public void GetEnum_RefreshesForEachCacheInstance() => throw new NotImplementedException();
		[Test] public void GetEnum_RefreshesForDifferentEnumerationTypes() => throw new NotImplementedException();
	}
}