using System;
using System.Collections.Generic;
using System.Linq;
using kCura.Relativity.Client;
using NUnit.Framework;
using Relativity.API;
using Relativity.Test.Helpers;
using Relativity.Test.Helpers.ServiceFactory.Extentions;
using Relativity.Test.Helpers.SharedTestHelpers;
using Relativity.Test.Helpers.WorkspaceHelpers;
using System.Configuration;
using Gravity.Base;
using Gravity.Test.TestClasses;
using kCura.Relativity.Client.DTOs;
using System.Reflection;
using Gravity.Extensions;
using Gravity.Test.Helpers;
using Choice = kCura.Relativity.Client.DTOs.Choice;
using Artifact = kCura.Relativity.Client.DTOs.Artifact;

namespace Gravity.Test.Integration
{
	[TestFixture]
	public partial class RSAPI_IntegrationTest : Base
	{


		#region Setup
		[OneTimeSetUp]
		public void Execute_TestFixtureSetup()
		{
			//Start of test and setup
			Console.WriteLine("RSAPI Integration Test START.....");
		}
		#endregion

		#region "Teardown"
		[OneTimeTearDown]
		public void Execute_TestFixtureTeardown()
		{
			Console.WriteLine("RSAPI Integration Test Teardown START.....");
		}
		#endregion



		#region "Tests"

		[Test, Description("Verify Test Object is Created")]
		public void Valid_Gravity_Object_Created()
		{
			void Inner()
			{
				//Arrange
				LogStart("Arrangement");

				GravityLevelOne testObject = new GravityLevelOne() { Name = $"TestObject_{Guid.NewGuid()}" };


				LogEnd("Arrangement");

				//Act
				LogStart("Act");

				var newRdoArtifactId = _testObjectHelper.GetDao().Insert(testObject, ObjectFieldsDepthLevel.FirstLevelOnly);

				LogEnd("Act");

				//Assert
				LogStart("Assertion");

				//Assert object returned valid Artifact ID
				LogStart("Artifact ID > 0 Assertion");
				Assert.Greater(newRdoArtifactId, 0);
				LogEnd($"Artifact ID > 0 Assertion (was {newRdoArtifactId})");

				LogEnd("Assertion");
			}
			TestWrapper(Inner);
		}


		[Test, Description("RSAPIDao: Verify RelativityObject field created correctly using Gravity"),
		 TestCaseSource(typeof(TestCaseDefinition), nameof(TestCaseDefinition.SimpleFieldReadWriteTestCases))]
		//need object fields, could get a little more difficult
		public void Valid_Gravity_RelativityObject_Create_Field_Type<T>(string objectPropertyName, T sampleData)
		{
			void Inner()
			{
				//Arrange
				LogStart($"for property{objectPropertyName}");

				GravityLevelOne testObject = new GravityLevelOne() { Name = $"TestObjectCreate_{objectPropertyName}{Guid.NewGuid()}" };

				var testFieldAttribute = testObject.GetCustomAttribute<RelativityObjectFieldAttribute>(objectPropertyName);
				Guid testFieldGuid = testFieldAttribute.FieldGuid;
				RdoFieldType fieldType = testFieldAttribute.FieldType;

				_client.APIOptions.WorkspaceID = _workspaceId;
				RSAPI_IntegrationTestHelper.CreateRDOFromGravityOneObject(testObject, _client, _testObjectHelper, objectPropertyName, sampleData, fieldType);

				LogEnd("Arrangement");

				//Act
				LogStart("Act");

				var newRdoArtifactId = _testObjectHelper.GetDao().Insert(testObject, ObjectFieldsDepthLevel.FirstLevelOnly);

				//read artifactID from RSAPI
				RDO newObject = _client.Repositories.RDO.ReadSingle(newRdoArtifactId);

				FieldValue field = newObject.Fields.Get(testFieldGuid);

				object newObjectValue = null;
				object expectedData = sampleData;

				switch (fieldType)
				{
					case RdoFieldType.LongText:
						newObjectValue = field.ValueAsLongText;
						break;
					case RdoFieldType.FixedLengthText:
						newObjectValue = field.ValueAsFixedLengthText;
						break;
					case RdoFieldType.WholeNumber:
						newObjectValue = field.ValueAsWholeNumber;
						break;
					case RdoFieldType.YesNo:
						newObjectValue = field.ValueAsYesNo;
						break;
					case RdoFieldType.Currency:
						newObjectValue = field.ValueAsCurrency;
						break;
					case RdoFieldType.Decimal:
						newObjectValue = field.ValueAsDecimal;
						break;
					case RdoFieldType.SingleChoice:
						int choiceArtifactId = field.ValueAsSingleChoice.ArtifactID;
						if (choiceArtifactId > 0)
						{
							Choice choice = _client.Repositories.Choice.ReadSingle(choiceArtifactId);
							Enum singleChoice = (Enum)Enum.ToObject(sampleData.GetType(), sampleData);
							Guid singleChoiceGuid = singleChoice.GetRelativityObjectAttributeGuidValue();
							newObjectValue = choice.Guids.SingleOrDefault(x => x == singleChoiceGuid);
							expectedData = singleChoiceGuid;
						}
						break;
					case RdoFieldType.SingleObject:
						newObjectValue = field.ValueAsSingleObject.ArtifactID;
						expectedData = testObject.GravityLevel2Obj.ArtifactId > 0
							? (object)testObject.GravityLevel2Obj.ArtifactId
							: null;
						break;
					case RdoFieldType.MultipleObject:
						var rawNewObjectValue = field.GetValueAsMultipleObject<Artifact>();
						var resultData = new List<GravityLevel2>();
						Guid childFieldNameGuid = new GravityLevel2().GetCustomAttribute<RelativityObjectFieldAttribute>("Name").FieldGuid;

						foreach (Artifact child in rawNewObjectValue)
						{
							//'Read' - need to get name.
							RDO childRdo = new RDO() {
								Fields = new List<FieldValue>() { new FieldValue(childFieldNameGuid) }
							};
							childRdo = _client.Repositories.RDO.ReadSingle(child.ArtifactID);
							string childNameValue = childRdo.Fields.Where(x => x.Guids.Contains(childFieldNameGuid)).FirstOrDefault().ToString();

							resultData.Add(new GravityLevel2() { ArtifactId = child.ArtifactID, Name = childNameValue });
						}
						newObjectValue = resultData.ToDictionary(x => x.ArtifactId, x => x.Name);
						expectedData = ((IEnumerable<GravityLevel2>)expectedData).ToDictionary(x => x.ArtifactId, x => x.Name);

						break;
				}

				LogEnd("Act");

				//Assert
				LogStart("Assertion");

				//Assert
				Assert.AreEqual(expectedData, newObjectValue);

				LogEnd("Assertion");
			}
			TestWrapper(Inner);
		}

		[Test, Description("RSAPIDao: Verify RelativityObject field read correctly using Gravity"),
		 TestCaseSource(typeof(TestCaseDefinition), nameof(TestCaseDefinition.SimpleFieldReadWriteTestCases))]
		public void Valid_Gravity_RelativityObject_Read_Field_Type<T>(string objectPropertyName, T sampleData)
		{
			void Inner()
			{
				//Arrange
				LogStart($"Arrangement for property {objectPropertyName}");


				GravityLevelOne testObject = new GravityLevelOne() { Name = $"TestObjectRead_{objectPropertyName}{Guid.NewGuid()}" };

				var testFieldAttribute = testObject.GetCustomAttribute<RelativityObjectFieldAttribute>(objectPropertyName);
				Guid testFieldGuid = testFieldAttribute.FieldGuid;
				RdoFieldType fieldType = testFieldAttribute.FieldType;
				Guid nameFieldGuid = testObject.GetCustomAttribute<RelativityObjectFieldAttribute>("Name").FieldGuid;
				Guid testObjectTypeGuid = testObject.GetObjectLevelCustomAttribute<RelativityObjectAttribute>().ObjectTypeGuid;

				_client.APIOptions.WorkspaceID = _workspaceId;
				object expectedData = sampleData;
				int newArtifactId = RSAPI_IntegrationTestHelper.CreateRDOFromValue(testObject, _client, _testObjectHelper,
						testObjectTypeGuid, nameFieldGuid, testFieldGuid, sampleData, fieldType, ref expectedData);


				LogEnd("Arrangement");

				//Act
				LogStart("Act");

				object gravityFieldValue = null;

				if (newArtifactId > 0)
				{
					GravityLevelOne testGravityObject = _testObjectHelper.GetDao().Get<GravityLevelOne>(newArtifactId, ObjectFieldsDepthLevel.FirstLevelOnly);
					gravityFieldValue = testGravityObject.GetPropertyValue(objectPropertyName);
					if (gravityFieldValue != null)
					{
						switch (fieldType)
						{
							case RdoFieldType.SingleObject:
								gravityFieldValue = ((GravityLevel2)gravityFieldValue).Name;
								break;
							case RdoFieldType.MultipleObject:
								gravityFieldValue = ((List<GravityLevel2>)gravityFieldValue).ToDictionary(x => x.ArtifactId, x => x.Name);
								break;
						}
					}
				}

				LogEnd("Act");

				//Assert
				LogStart("Assertion");

				if (newArtifactId > 0)
				{
					Assert.AreEqual(expectedData, gravityFieldValue);
				}
				else
				{
					Assert.Fail("Could not create object to test with through RSAPI. This is not a Gravity failure.");
				}

				LogEnd("Assertion");
			}
			TestWrapper(Inner);
		}
		#endregion

	}
}
