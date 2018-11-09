using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;

namespace Gravity.DAL.RSAPI.Tests
{
	public class MockedRsapiProviderTestBase
	{
		protected Mock<IRsapiProvider> rsapiProvider;

		[SetUp]
		public void Init()
		{
			rsapiProvider = new Mock<IRsapiProvider>(MockBehavior.Strict);
		}

		[TearDown]
		public void End()
		{
			var skipVerify = (bool?)TestContext.CurrentContext.Test.Properties["SkipVerifyAll"].FirstOrDefault();
			if (skipVerify != true)
			{
				rsapiProvider.VerifyAll();
			}
		}

		[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
		[Obsolete("Fix the test to handle " + nameof(Mock.VerifyAll))]
		public class SkipVerifyAllAttribute : PropertyAttribute
		{
			public SkipVerifyAllAttribute() : base(true) { }
		}
	}

	
}
