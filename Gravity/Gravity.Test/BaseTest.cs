using Microsoft.VisualStudio.TestTools.UnitTesting;
using Relativity.API;
using Gravity.Base;

namespace Gravity.Test
{
	[TestClass]
	public abstract class BaseTest
	{
		// Implementation of IHelper here to simulate the different contexts in Relativity where you can place code, such as:
		//		event handlers, agents, custom pages
		// Implementing tests will have the "same" Helper available to them.
		protected IHelper Helper;

		public virtual void Initialize()
		{
			Helper = new AppConfigConnectionHelper();
		}
	}
}
