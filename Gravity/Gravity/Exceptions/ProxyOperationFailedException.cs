using System;

namespace Gravity.Exceptions
{
	public class ProxyOperationFailedException : Exception
	{
		public ProxyOperationFailedException() : base() { }

		public ProxyOperationFailedException(string message) : base(message) { }

		public ProxyOperationFailedException(string message, Exception innerException) : base(message, innerException) { }
	}
}
