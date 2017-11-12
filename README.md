# Gravity
CRUDQ Framework for Relativity Custom Development

This is also available as a [nuget package](https://www.nuget.org/packages/Gravity/).

## Target Framework
* .NET 4.5.2

## Dependencies
This project requires references to Relativity's Relativity® SDK dlls. These dlls are not part of the open source project and can be obtained 
by contacting support@relativity.com, getting it from your Relativity instance, or installing the SDK from the [Community Portal](https://community.relativity.com/s/files).

* kCura.Relativity.Client.dll 
* Relativity.API.dll 
* kCura.Data.RowDataGateway.dll 

## Samples
Before using the CRUD/Q methods in Gravity you will have to create a model and decorate it with the appropriate attributes.

* RelativityObject Attribute - Specifies the type Guid of the RDO you are targeting.
* RelativityObjectField Attribute - Specifies the type Guid and the "RdoFieldType" of the RDO field you are targeting.
* RelativityMultipleObject Attribute - Specifies the type Guid of a multiple object RDO field.
     * **Note:** This attribute is used if you want to return the field as a List of objects (not just ids).
* RelativitySingleObject Attribute - Specifies the type Guid of a single object RDO field.
    * **Note:** This attribute is used if you want to return the field as a objects (not just id).
* RelativityObjectChildrenList Attribute - Attribute is used to decorate a List of child RDOs as a object List.

The following example demonstrates a RDO represented as a Model:
```csharp
[Serializable]
[RelativityObject("0B5C62E0-2AFA-4408-B7FF-789351C9BEDC")]
public class DemoPurchaseOrder : BaseDto
{
	[RelativityObjectField("E1FA93B9-C2DB-442A-9978-84EEB6B61A3F", (int)RdoFieldType.FixedLengthText, 255)]
	public override string Name { get; set; }

	[RelativityObjectField("37159592-B5B6-4405-AF74-10B5728890B4", (int)RdoFieldType.WholeNumber)]
	public int OrderNumber { get; set; }

	[RelativityObjectField("37159592-B5B6-4405-AF74-10B5728890B4", (int)RdoFieldType.FixedLengthText, 100)]
	public string CustomerName { get; set; }

	[RelativityObjectField("3BDC0971-A87C-414E-9A37-FC477279BBAD", (int)RdoFieldType.FixedLengthText, 100)]
	public string CustomerEmail { get; set; }

	[RelativityObjectField("D0770889-8A4D-436A-9647-33419B96E37E", (int)RdoFieldType.MultipleObject, typeof(Items))]
	public IList<Items> ItemIds { get; set; }

	[RelativityMultipleObject("D0770889-8A4D-436A-9647-33419B96E37E", typeof(Items))]
	public List<Items> Items { get; set; }

	[RelativitySingleObject("D0770889-8A4D-436A-9647-33419B96E37E", typeof(Address))]
	public Address Address { get; set; }

	[RelativityObjectField("4501A308-5E68-4314-AEDC-4DEB527F12A8", (int)RdoFieldType.Decimal)]
	public decimal Total { get; set; }

	[RelativityObjectField("CEDB347B-679D-44ED-93D3-0B3027C7E6F5", (int)RdoFieldType.SingleChoice, typeof(OrderType))]
	public OrderType OrderType { get; set; }

	[RelativityObjectChildrenList(typeof(RelatedPurchase))]
	public IList<RelatedPurchase> RelatedPurchases { get; set; }
}
```

* **Note:** For property of type `User` use `kCura.Relativity.Client.DTOs.User` and for property of type `FileField` use `Gravity.Base.RelativityFile`

For Choice field you must create a enum and decorate it with the appropriate attributes.

* RelativityObject Attribute - Specifies the choice Guid.

The following example demonstrates a choice field represented as an Enum:
```csharp
public enum OrderType
{
	[RelativityObject("4F04381D-F3E3-4DEE-8EF9-11F27047D9B4")]
	TypeOne = 1,

	[RelativityObject("8453BF3E-D95B-4BC5-BD68-3CF4277DD731")]
	TypeTwo = 2
}
```

To use Gravity for RSAPI operations, you must instantiate an `RsapiDao` object using the `RsapiDao` constructor, with `IHelper` and `WorkspaceId` as parameters.

Supported RSAPIDao methods:
 - GetRelativityObject<T>(int artifactId, ObjectFieldsDepthLevel depthLevel) - Get DTO by Artifact ID and specific depth level of child objects and object     fields.
 - GetDTOs<T>(int[] artifactIDs, ObjectFieldsDepthLevel depthLevel) - Get DTOs by Artifact IDs and specific depth level of child objects and object
 fields.
 - List<T> GetAllChildDTOs<T>(Guid parentFieldGuid, int parentArtifactID, ObjectFieldsDepthLevel depthLevel) - Get all child DTOs of type for parent.
 - List<T> GetAllDTOs<T>(Condition queryCondition = null, ObjectFieldsDepthLevel depthLevel = ObjectFieldsDepthLevel.FirstLevelOnly)
 - List<T> GetAllDTOs<T>() - Get all DTOs of type.
 - DeleteRelativityObjectRecusively<T>(T theObjectToDelete) - Delete object recursively (includes child objects).
 - DeleteRelativityObjectRecusively<T>(int objectToDeleteId)- Delete object recursively (includes child objects) by Artifat ID.
 - InsertChildListObjects<T>(IList<T> objectsToInserted, int parentArtifactId) - Insert Child objects for parent.
 - InsertRelativityObject<T>(BaseDto theObjectToInsert) - Insert Relativity object from RDO.
 - UpdateRelativityObject<T>(BaseDto theObjectToUpdate) - Update Relativity object from RDO.
 - UpdateField<T>(int rdoID, Guid fieldGuid, object value) - Update field value by GUID and RDO Artifact ID


The following example demonstrates a object "Get" used in Event handler. First we instantiate `RsapiDao` and then we use the Gravity RSAPI Dao GetRelativityObject method to get the object (`ObjectFieldsDepthLevel.FirstLevelOnly` means that we want just the object - no child object fields, multiple object fields or single object fields are populated recursively):
```csharp
public override Response Execute()
{
	Response returnResponse = new Response() { Message = string.Empty, Success = true };

	RsapiDao gravityRsapiDao = new RsapiDao(this.Helper, this.Helper.GetActiveCaseID());

		DemoPurchaseOrder demoOrder =  gravityRsapiDao.GetRelativityObject<DemoPurchaseOrder>(1047088,
	  ObjectFieldsDepthLevel.FirstLevelOnly);

		return returnResponse;
}
```
