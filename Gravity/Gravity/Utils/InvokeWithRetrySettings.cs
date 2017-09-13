namespace Gravity.Utils
{
	public class InvokeWithRetrySettings
	{
		public InvokeWithRetrySettings(int retryAttempts,int  sleepTimeInMilliseconds)
		{
			this.RetryAttempts = retryAttempts;
			this.SleepTimeInMiliseconds = sleepTimeInMilliseconds;
		}

		public int RetryAttempts { get; set; }

		public int SleepTimeInMiliseconds { get; set; }
	}
}
