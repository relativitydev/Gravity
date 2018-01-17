using Gravity.Globals;

namespace Gravity.Utils
{
	public class InvokeWithRetrySettings
	{
		public static InvokeWithRetrySettings Default 
			=> new InvokeWithRetrySettings(SharedConstants.retryAttempts, SharedConstants.sleepTimeInMiliseconds);

		public InvokeWithRetrySettings(int retryAttempts,int  sleepTimeInMilliseconds)
		{
			this.RetryAttempts = retryAttempts;
			this.SleepTimeInMiliseconds = sleepTimeInMilliseconds;
		}

		public int RetryAttempts { get; set; }

		public int SleepTimeInMiliseconds { get; set; }
	}
}
