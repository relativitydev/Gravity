using System;
using System.Threading;

namespace Gravity.Utils
{
	public class InvokeWithRetryService
	{
		private InvokeWithRetrySettings settings;

		public InvokeWithRetryService(InvokeWithRetrySettings settings)
		{
			this.settings = settings;
		}

		public T InvokeWithRetry<T>(Func<T> f)
		{
			int retryCount = 0;
			T result = default(T);

			while (retryCount++ < settings.RetryAttempts)
			{
				try
				{
					result = f();
					break;
				}
				catch (Exception ex)
				{
					if (retryCount == settings.RetryAttempts)
					{
						throw;
					}
				}

				Thread.Sleep(settings.SleepTimeInMiliseconds);
			}

			return result;
		}

		public void InvokeVoidMethodWithRetry(Action action)
		{
			int retryCount = 0;

			while (retryCount++ < settings.RetryAttempts)
			{
				try
				{
					action();
					break;
				}
				catch (Exception ex)
				{
					if (retryCount == settings.RetryAttempts)
					{
						throw;
					}
				}

				Thread.Sleep(settings.SleepTimeInMiliseconds);
			}
		}
	}
}
