using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gravity.Test.Unit
{
    public class RsapiDaoDeleteTests
    {
		[Test]
		public void DeleteRelativityObjectRecusively()
		{
			//DeleteRelativityObjectRecusively<T>(T theObjectToDelete)

			//write a class with nested child objects
			// DeleteRDO should be called on the root only
			// Each other level should have DeleteRDOs called on the whole child collection (recursively)
			throw new NotImplementedException();
		}
	}
}
