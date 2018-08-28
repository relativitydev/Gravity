using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gravity.Test.Unit
{
	public class RsapiDaoUpdateTests
	{
		[Test]
		public void Update_SimpleFields()
		{
		}

		[Test]
		public void Update_SimpleFields_Remove()
		{
		}

		[Test]
		public void Update_SingleChoice()
		{
		}

		[Test]
		public void Update_MultipleChoice_AddAndRemove()
		{
		}

		[Test, Ignore("File behavior not defined yet")]
		public void Update_FileField_Add()
		{
		}

		[Test, Ignore("File behavior not defined yet")]
		public void Update_FileField_Remove()
		{
		}

		[Test, Ignore("File behavior not defined yet")]
		public void Update_FileField_Modify()
		{
		}
		
		[Test]
		public void Update_ChildObject_Update()
		{
		}

		[Test]
		public void Update_ChildObject_UpdateWithRecursion()
		{
		}

		[Test]
		public void Update_ChildObject_InsertNew()
		{
			//without recursion, should throw an error
		}

		[Test]
		public void Update_ChildObject_InsertNewWithRecursion()
		{
		}

		[Test]
		public void Update_ChildObject_Remove()
		{
			//delete if not in the collection. Annoying that have to query, but <shrug>
		}

		[Test]
		public void Update_SingleObject_Update()
		{
		}

		[Test]
		public void Update_SingleObject_UpdateWithRecursion()
		{
		}

		[Test]
		public void Update_SingleObject_InsertExisting()
		{
		}

		[Test]
		public void Update_SingleObject_InsertExistingWithRecursion()
		{
		}

		[Test]
		public void Update_SingleObject_InsertNew()
		{
			//without recursion, should throw an error
		}

		[Test]
		public void Update_SingleObject_InsertNewWithRecursion()
		{
		}

		[Test, Ignore("No third-level objects")]
		public void Update_SingleObject_InsertNewWithUpdatePropertyRecursion()
		{
		}

		[Test]
		public void Update_SingleObject_Remove()
		{
		}

		[Test]
		public void Update_MultipleObject_Update()
		{
		}

		[Test]
		public void Update_MultipleObject_UpdateWithRecursion()
		{
		}

		[Test]
		public void Update_MultipleObject_InsertExisting()
		{
		}

		[Test]
		public void Update_MultipleObject_InsertExistingWithRecursion()
		{
		}

		[Test]
		public void Update_MultipleObject_InsertNew()
		{
			//without recursion, should throw an error
		}

		[Test]
		public void Update_MultipleObject_InsertNewWithRecursion()
		{
		}


		[Test, Ignore("No third-level objects")]
		public void Update_MultipleObject_InsertNewWithUpdatePropertyRecursion()
		{
		}

		[Test]
		public void Update_MultipleObject_Remove()
		{
		}

	}
}
