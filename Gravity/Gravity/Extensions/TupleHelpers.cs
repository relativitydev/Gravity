using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gravity.Extensions
{
    internal static class TupleHelpers
    {
		// Lets us use C#7 - style tuple deconstruction on regular Tuples in .NET 4.5.1
		public static void Deconstruct<T1, T2>(this Tuple<T1, T2> tuple, out T1 item1, out T2 item2)
		{
			item1 = tuple.Item1;
			item2 = tuple.Item2;
		}

	}
}
