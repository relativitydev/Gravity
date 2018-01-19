using Gravity.Globals;
using Polly;
using System;
using System.Threading;

namespace Gravity.Utils
{
	public static class StandardPolicies
	{
		public static Policy InvokeWithRetry(int retryAttempts, int sleepTimeInMilliseconds) 
			=> Policy
				.Handle<Exception>()
				.WaitAndRetry(retryAttempts, x => TimeSpan.FromMilliseconds(sleepTimeInMilliseconds));

		public static Policy InvokeWithRetry() 
			=> InvokeWithRetry(SharedConstants.retryAttempts, SharedConstants.sleepTimeInMiliseconds);
	}
}
