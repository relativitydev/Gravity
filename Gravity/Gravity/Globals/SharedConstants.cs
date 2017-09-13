namespace Gravity.Globals
{
	public static class SharedConstants
	{
		public static readonly char ListIntSeparatorChar = ',';
		public const int FieldTypeCustomListInt = -2;
		public const int FieldTypeByteArray = -3;

		public static decimal KBFactorOfBytes = 1024m;
		public static decimal MBFactorOfBytes = 1048576m;
		public static decimal GBFactorOfBytes = 1073741824m;

		public static decimal MBFactorOfKilobytes = 1024m;
		public static decimal GBFactorOfKilobytes = 1048576m;

		internal static int retryAttempts = 5;
		internal static int sleepTimeInMiliseconds = 1000;
	}
}
