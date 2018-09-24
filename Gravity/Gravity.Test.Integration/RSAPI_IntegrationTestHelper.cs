using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gravity.Base;
using Gravity.Extensions;
using Gravity.Test.Helpers;
using Gravity.Test.TestClasses;
using kCura.Relativity.Client;
using kCura.Relativity.Client.DTOs;
using NUnit.Framework;

namespace Gravity.Test.Integration
{
	public static class RSAPI_IntegrationTestHelper
	{
		public static int CreateRDOFromGravityOneObject<T>(GravityLevelOne testObject, IRSAPIClient client, TestObjectHelper testObjectHelper, string objectPropertyName, T sampleData, RdoFieldType fieldType)
		{
			switch (fieldType)
			{
				case RdoFieldType.Currency:
				case RdoFieldType.Decimal:
					testObject.SetValueByPropertyName(objectPropertyName, Convert.ToDecimal(sampleData));
					break;
				default:
					testObject.SetValueByPropertyName(objectPropertyName, sampleData);
					break;
			}

			var newRdoArtifactId = testObjectHelper.GetDao().Insert(testObject, ObjectFieldsDepthLevel.FirstLevelOnly);
			return newRdoArtifactId;
		}

		public static int CreateRDOFromValue<T>(GravityLevelOne testObject, IRSAPIClient client, TestObjectHelper testObjectHelper, Guid objectType, Guid nameField, Guid testField, T sampleData, RdoFieldType fieldType, ref object expectedData)
		{
			var dto = new RDO() { ArtifactTypeGuids = new List<Guid> { objectType } };
			int newArtifactId = -1;
			dto.Fields.Add(new FieldValue(nameField, testObject.Name));

			int objectToAttachID;

			//need this mess because when passing in tests for decimal and currency System wants to use double and causes problems
			switch (fieldType)
			{
				case RdoFieldType.SingleChoice:
					Enum singleChoice = (Enum)Enum.ToObject(sampleData.GetType(), sampleData);
					Guid singleChoiceGuid = singleChoice.GetRelativityObjectAttributeGuidValue();
					kCura.Relativity.Client.DTOs.Choice singleChoiceToAdd = new kCura.Relativity.Client.DTOs.Choice(singleChoiceGuid);
					dto.Fields.Add(new FieldValue(testField, singleChoiceToAdd));
					break;
				case RdoFieldType.SingleObject:
					int objectToAttach =
							testObjectHelper.GetDao().Insert(sampleData as GravityLevel2, ObjectFieldsDepthLevel.FirstLevelOnly);
					dto.Fields.Add(new FieldValue(testField, objectToAttach));
					expectedData = (sampleData as GravityLevel2).Name;
					break;
				case RdoFieldType.MultipleObject:
					IList<GravityLevel2> gravityLevel2s = (IList<GravityLevel2>)sampleData;
					FieldValueList<kCura.Relativity.Client.DTOs.Artifact> objects = new FieldValueList<kCura.Relativity.Client.DTOs.Artifact>();
					expectedData = new Dictionary<int, string>();
					foreach (GravityLevel2 child in gravityLevel2s)
					{
						objectToAttachID =
								testObjectHelper.GetDao().Insert(child, ObjectFieldsDepthLevel.FirstLevelOnly);
						objects.Add(new kCura.Relativity.Client.DTOs.Artifact(objectToAttachID));
						(expectedData as Dictionary<int, string>).Add(objectToAttachID, child.Name);
					}
					dto.Fields.Add(new FieldValue(testField, objects));
					break;
				default:
					dto.Fields.Add(new FieldValue(testField, sampleData));
					break;
			}

			WriteResultSet<RDO> writeResults = client.Repositories.RDO.Create(dto);

			if (writeResults.Success)
			{
				newArtifactId = writeResults.Results[0].Artifact.ArtifactID;
				Console.WriteLine($"Object was created with Artifact ID {newArtifactId}.");
			}
			else
			{
				Console.WriteLine($"An error occurred creating object: {writeResults.Message}");
				foreach (var result in
						writeResults.Results
						.Select((item, index) => new { rdoResult = item, itemNumber = index })
						.Where(x => x.rdoResult.Success == false))
				{
					Console.WriteLine($"An error occurred in create request {result.itemNumber}: {result.rdoResult.Message}");
				}
			}
			return newArtifactId;
		}
	}
}
