using Polly;
using System;
using System.Threading;

namespace Gravity.Utils
{
	public class InvokeWithRetryService
	{
		private Policy policy;

		public InvokeWithRetryService(InvokeWithRetrySettings settings)
		{
			policy = Policy
				.Handle<Exception>()
				.WaitAndRetry(settings.RetryAttempts, x => TimeSpan.FromMilliseconds(settings.SleepTimeInMiliseconds));
		}

		public T InvokeWithRetry<T>(Func<T> func)
		{
			return policy.Execute(func);
		}

		public void InvokeVoidMethodWithRetry(Action action)
		{
			policy.Execute(action);
		}
	}
}
