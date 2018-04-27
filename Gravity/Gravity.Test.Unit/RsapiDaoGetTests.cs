using NUnit.Framework;
using System;
using System.Linq;

namespace Gravity.Test.Unit
{
	public class RsapiDaoGetTests
	{
		[Test]
		public void ToHydratedRDO_SimpleTypesFields()
		{
			//ensures that simple type fields can be set
			throw new NotImplementedException();
		}

		[Test]
		public void ToHydratedRDO_TypeMismatch()
		{
			//ensures that if wrong type, throws an exception
			throw new NotImplementedException();
		}

		[Test]
		public void ToHydratedRDO_MultipleChoice_AllInEnum()
		{
			//ensures can set multiple choice
			//ensures mixed case works
			//ensures can trim to match by name (but see below)
			throw new NotImplementedException();
		}

		[Test]
		public void ToHydratedRDO_MultipleChoice_MultipleEnumSameValue()
		{
			//Note that we are stuck matching by name because of RSAPI limitations. If required, we need to make
			//another query.

			//So for example, matching two different Choices with names "(Other)" and "Other" will both try to map to the same client-side type.
			//This needs to throw an error until it is fixed.
			throw new NotImplementedException();
		}

		[Test]
		public void ToHydratedRDO_MultipleChoice_ChoiceNotInAvailableChoices()
		{
			//What do we want to do when some choices are not enumerated in the set of valid choices? Throw an exception?
			throw new NotImplementedException();
		}

		[Test]
		public void ToHydratedRDO_MultipleObject_PopulatesIntegerListField()
		{
			throw new NotImplementedException();
		}

		[Test]
		public void ToHydratedRDO_SingleObject_PopulatesIntegerField()
		{
			throw new NotImplementedException();
		}

		[Test]
		public void ToHydratedRDO_User()
		{
			//this is going to be a problem in the long run as we will not always be using RSAPI types
			throw new NotImplementedException();
		}

		[Test]
		public void ToHydratedRDO_File()
		{
			//generates file
			throw new NotImplementedException();
		}

		[Test]
		public void GetHydratedDTO_MultiObject_Recursive()
		{
			//test MultiObject fields with varying degrees of recursion
			throw new NotImplementedException();
		}

		[Test]
		public void GetHydratedDTO_ChildObjectList_Recursive()
		{
			//test ChildObject fields with varying degrees of recursion
			throw new NotImplementedException();
		}

		[Test]
		public void GetHydratedDTO_SingleObject_Recursive()
		{
			//test single object fields with varying degrees of recursion
			throw new NotImplementedException();
		}

		[Test]
		public void GetHydratedDTO_DownloadsFileContents()
		{
			//if possible, test whether Hydrated DTO can download properly
			throw new NotImplementedException();
		}


	}
}
